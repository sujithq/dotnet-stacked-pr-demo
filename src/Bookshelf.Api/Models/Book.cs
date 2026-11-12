namespace Bookshelf.Api.Models;

public sealed record Book(Guid Id, string Title, string Author, string Isbn, int PublishedYear);
