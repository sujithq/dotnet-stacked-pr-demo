using Bookshelf.Api.Models;
using Bookshelf.Api.Repositories;

namespace Bookshelf.Api.Services;

public sealed class BookCatalogService(IBookRepository repository, TimeProvider timeProvider) : IBookCatalogService
{
    public IReadOnlyList<Book> GetAll(string? query = null)
    {
        var books = repository.GetAll();
        if (string.IsNullOrWhiteSpace(query))
        {
            return books;
        }

        var search = query.Trim();
        return books.Where(book =>
            book.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            book.Author.Contains(search, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public Book? GetById(Guid id) => repository.GetById(id);

    public AddBookResult Add(CreateBookRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors[nameof(request.Title)] = ["Title is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Author))
        {
            errors[nameof(request.Author)] = ["Author is required."];
        }

        var isbn = NormalizeIsbn(request.Isbn);
        if (isbn is null)
        {
            errors[nameof(request.Isbn)] = ["A valid ISBN-13 is required."];
        }

        if (request.PublishedYear < 1450 || request.PublishedYear > timeProvider.GetUtcNow().Year)
        {
            errors[nameof(request.PublishedYear)] = ["Published year must be between 1450 and the current year."];
        }

        if (errors.Count > 0)
        {
            return new(AddBookStatus.ValidationErrors, null, errors);
        }

        var book = new Book(Guid.NewGuid(), request.Title!.Trim(), request.Author!.Trim(), isbn!, request.PublishedYear);
        return repository.TryAdd(book)
            ? new(AddBookStatus.Success, book, errors)
            : new(AddBookStatus.DuplicateIsbn, null, errors);
    }

    private static string? NormalizeIsbn(string? value)
    {
        if (value is null || value.Any(c => c is not (>= '0' and <= '9') and not '-' and not ' '))
        {
            return null;
        }

        var digits = new string(value.Where(c => c is not ('-' or ' ')).ToArray());
        if (digits.Length != 13 || !(digits.StartsWith("978", StringComparison.Ordinal) || digits.StartsWith("979", StringComparison.Ordinal)))
        {
            return null;
        }

        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            sum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        return sum % 10 == 0 ? digits : null;
    }
}
