using Bookshelf.Api.Models;

namespace Bookshelf.Api.Services;

public interface IBookCatalogService
{
    IReadOnlyList<Book> GetAll();
    Book? GetById(Guid id);
    AddBookResult Add(CreateBookRequest request);
}
