using Bookshelf.Api.Models;

namespace Bookshelf.Api.Repositories;

public interface IBookRepository
{
    IReadOnlyList<Book> GetAll();
    Book? GetById(Guid id);
    bool TryAdd(Book book);
}
