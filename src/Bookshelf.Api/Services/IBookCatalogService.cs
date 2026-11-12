using Bookshelf.Api.Models;

namespace Bookshelf.Api.Services;

public interface IBookCatalogService
{
    IReadOnlyList<Book> GetAll(string? query = null);
    Book? GetById(Guid id);
    AddBookResult Add(CreateBookRequest request);
}
