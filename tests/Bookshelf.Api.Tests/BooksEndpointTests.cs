using System.Net;
using System.Net.Http.Json;
using System.Text;
using Bookshelf.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bookshelf.Api.Tests;

public sealed class BooksEndpointTests : IDisposable
{
    private static readonly CreateBookRequest ValidBook = new("The Hobbit", "J. R. R. Tolkien", "978-0-261 10221-7", 1937);
    private readonly WebApplicationFactory<Program> factory = new();

    [Fact]
    public async Task Create_ListAndGet_ReturnSameBookWithWorkingLocation()
    {
        using var client = factory.CreateClient();
        using var emptyResponse = await client.GetAsync("/api/books");
        Assert.Equal(HttpStatusCode.OK, emptyResponse.StatusCode);
        Assert.Empty(Assert.IsType<Book[]>(await emptyResponse.Content.ReadFromJsonAsync<Book[]>()));

        using var created = await client.PostAsJsonAsync("/api/books", ValidBook);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var book = Assert.IsType<Book>(await created.Content.ReadFromJsonAsync<Book>());
        Assert.NotEqual(Guid.Empty, book.Id);
        Assert.Equal(ValidBook.Title, book.Title);
        Assert.Equal(ValidBook.Author, book.Author);
        Assert.Equal("9780261102217", book.Isbn);
        Assert.Equal(ValidBook.PublishedYear, book.PublishedYear);
        var location = Assert.IsType<Uri>(created.Headers.Location);
        Assert.Equal($"/api/books/{book.Id}", location.AbsolutePath);

        using var fetched = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        Assert.Equal(book, await fetched.Content.ReadFromJsonAsync<Book>());

        using var listed = await client.GetAsync("/api/books");
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        Assert.Equal(book, Assert.Single(Assert.IsType<Book[]>(await listed.Content.ReadFromJsonAsync<Book[]>())));
    }

    [Fact]
    public async Task Create_InvalidBook_ReturnsFieldErrorsWithoutStoringBook()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/books", new CreateBookRequest(" ", null, "invalid", 1449));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(
            await response.Content.ReadFromJsonAsync<ValidationProblemDetails>());
        Assert.Equal(400, problem.Status);
        foreach (var field in new[] { "Title", "Author", "Isbn", "PublishedYear" })
        {
            Assert.Contains(field, problem.Errors.Keys);
            Assert.All(problem.Errors[field], error => Assert.False(string.IsNullOrWhiteSpace(error)));
            Assert.NotEmpty(problem.Errors[field]);
        }

        Assert.Empty(Assert.IsType<Book[]>(await client.GetFromJsonAsync<Book[]>("/api/books")));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{")]
    public async Task Create_InvalidBody_ReturnsBadRequest(string json)
    {
        using var client = factory.CreateClient();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/books", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(
            await response.Content.ReadFromJsonAsync<ValidationProblemDetails>());
        Assert.NotEmpty(problem.Errors);
    }

    [Fact]
    public async Task Get_UnknownId_ReturnsNotFound()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/books/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateNormalizedIsbn_ReturnsConflictWithoutAddingBook()
    {
        using var client = factory.CreateClient();
        using var created = await client.PostAsJsonAsync("/api/books", ValidBook);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var duplicate = await client.PostAsJsonAsync("/api/books", ValidBook with { Isbn = "9780261102217" });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(await duplicate.Content.ReadFromJsonAsync<ProblemDetails>());
        Assert.Equal(409, problem.Status);
        Assert.Equal("Duplicate ISBN", problem.Title);
        Assert.Equal("A book with this ISBN already exists.", problem.Detail);
        Assert.Single(Assert.IsType<Book[]>(await client.GetFromJsonAsync<Book[]>("/api/books")));
    }

    [Theory]
    [InlineData("  hObBiT  ", "The Hobbit")]
    [InlineData("  mARTIN  ", "Clean Code")]
    [InlineData("unknown", null)]
    [InlineData("", "*")]
    [InlineData("  ", "*")]
    public async Task List_Query_FiltersByTitleOrAuthor(string query, string? expectedTitle)
    {
        using var client = factory.CreateClient();
        using var first = await client.PostAsJsonAsync("/api/books", ValidBook);
        using var second = await client.PostAsJsonAsync("/api/books",
            new CreateBookRequest("Clean Code", "Robert Martin", "9780132350884", 2008));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        using var response = await client.GetAsync($"/api/books?query={Uri.EscapeDataString(query)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var books = Assert.IsType<Book[]>(await response.Content.ReadFromJsonAsync<Book[]>());
        if (expectedTitle == "*")
        {
            Assert.Equal(2, books.Length);
            Assert.Contains(books, book => book.Title == "The Hobbit");
            Assert.Contains(books, book => book.Title == "Clean Code");
        }
        else if (expectedTitle is null)
        {
            Assert.Empty(books);
        }
        else
        {
            Assert.Equal(expectedTitle, Assert.Single(books).Title);
        }
    }

    public void Dispose() => factory.Dispose();
}
