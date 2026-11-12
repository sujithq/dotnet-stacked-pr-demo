namespace Bookshelf.Api.Models;

public sealed record CreateBookRequest(string? Title, string? Author, string? Isbn, int PublishedYear);
