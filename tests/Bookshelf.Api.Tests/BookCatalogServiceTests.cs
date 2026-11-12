using Bookshelf.Api.Models;
using Bookshelf.Api.Repositories;
using Bookshelf.Api.Services;

namespace Bookshelf.Api.Tests;

public sealed class BookCatalogServiceTests
{
    private static readonly CreateBookRequest ValidBook = new("The Hobbit", "J. R. R. Tolkien", "978-0-261 10221-7", 1937);
    private readonly BookCatalogService service = new(new InMemoryBookRepository(), new FixedTimeProvider());

    [Fact]
    public void Add_ValidBook_NormalizesAndStoresBook()
    {
        var result = service.Add(ValidBook);

        Assert.Equal(AddBookStatus.Success, result.Status);
        var book = Assert.IsType<Book>(result.Book);
        Assert.NotEqual(Guid.Empty, book.Id);
        Assert.Equal("9780261102217", book.Isbn);
        Assert.Equal(ValidBook.Title, book.Title);
        Assert.Equal(ValidBook.Author, book.Author);
        Assert.Equal(ValidBook.PublishedYear, book.PublishedYear);
        Assert.Empty(result.Errors);
        Assert.Equal(book, service.GetById(book.Id));
        Assert.Equal(book, Assert.Single(service.GetAll()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Add_MissingTitle_ReturnsValidationError(string? title)
    {
        var result = service.Add(ValidBook with { Title = title });
        Assert.Equal(AddBookStatus.ValidationErrors, result.Status);
        Assert.Contains("Title", result.Errors.Keys);
        Assert.Empty(service.GetAll());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Add_MissingAuthor_ReturnsValidationError(string? author)
    {
        var result = service.Add(ValidBook with { Author = author });
        Assert.Equal(AddBookStatus.ValidationErrors, result.Status);
        Assert.Contains("Author", result.Errors.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("9780261102218")]
    [InlineData("978026110221")]
    [InlineData("978026110221x")]
    [InlineData("9780261102217!")]
    [InlineData("1234567890128")]
    public void Add_InvalidIsbn_ReturnsValidationError(string? isbn)
    {
        var result = service.Add(ValidBook with { Isbn = isbn });
        Assert.Equal(AddBookStatus.ValidationErrors, result.Status);
        Assert.Contains("Isbn", result.Errors.Keys);
    }

    [Theory]
    [InlineData(1449)]
    [InlineData(2027)]
    public void Add_YearOutsideRange_ReturnsValidationError(int year)
    {
        var result = service.Add(ValidBook with { PublishedYear = year });
        Assert.Equal(AddBookStatus.ValidationErrors, result.Status);
        Assert.Contains("PublishedYear", result.Errors.Keys);
    }

    [Theory]
    [InlineData(1450)]
    [InlineData(2026)]
    public void Add_YearAtBoundary_Succeeds(int year)
    {
        Assert.Equal(AddBookStatus.Success, service.Add(ValidBook with { PublishedYear = year }).Status);
    }

    [Fact]
    public void Add_DuplicateNormalizedIsbn_IsRejected()
    {
        service.Add(ValidBook);
        var result = service.Add(ValidBook with { Isbn = "9780261102217" });
        Assert.Equal(AddBookStatus.DuplicateIsbn, result.Status);
        Assert.Null(result.Book);
        Assert.Single(service.GetAll());
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        Assert.Null(service.GetById(Guid.NewGuid()));
    }

    [Theory]
    [InlineData("Hobbit")]
    [InlineData("Tolkien")]
    [InlineData("  hObBiT  ")]
    [InlineData("  tOLKIEN  ")]
    public void GetAll_MatchingQuery_ReturnsOnlyMatchingBook(string query)
    {
        var expected = Assert.IsType<Book>(service.Add(ValidBook).Book);
        Assert.IsType<Book>(service.Add(new CreateBookRequest("Clean Code", "Robert Martin", "9780132350884", 2008)).Book);

        Assert.Equal(expected, Assert.Single(service.GetAll(query)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void GetAll_BlankQuery_ReturnsAllBooks(string? query)
    {
        var first = Assert.IsType<Book>(service.Add(ValidBook).Book);
        var second = Assert.IsType<Book>(service.Add(new CreateBookRequest("Clean Code", "Robert Martin", "9780132350884", 2008)).Book);

        Assert.Equal(new[] { first, second }, service.GetAll(query));
    }

    [Fact]
    public void GetAll_UnmatchedQuery_ReturnsEmpty()
    {
        service.Add(ValidBook);

        Assert.Empty(service.GetAll("not present"));
    }

    [Fact]
    public void Add_ConcurrentDuplicateIsbns_OnlyOneSucceeds()
    {
        var results = new AddBookResult[100];
        Parallel.For(0, results.Length, i => results[i] = service.Add(ValidBook));
        Assert.Single(results, result => result.Status == AddBookStatus.Success);
        Assert.Equal(99, results.Count(result => result.Status == AddBookStatus.DuplicateIsbn));
        Assert.Single(service.GetAll());
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
