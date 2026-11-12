using Bookshelf.Api.Models;

namespace Bookshelf.Api.Repositories;

public sealed class InMemoryBookRepository : IBookRepository
{
    private readonly Lock gate = new();
    private readonly Dictionary<Guid, Book> books = new();
    private readonly HashSet<string> isbns = new(StringComparer.Ordinal);

    public IReadOnlyList<Book> GetAll()
    {
        lock (gate)
        {
            return books.Values.ToArray();
        }
    }

    public Book? GetById(Guid id)
    {
        lock (gate)
        {
            return books.GetValueOrDefault(id);
        }
    }

    public bool TryAdd(Book book)
    {
        lock (gate)
        {
            if (isbns.Contains(book.Isbn))
            {
                return false;
            }

            books.Add(book.Id, book);
            isbns.Add(book.Isbn);
            return true;
        }
    }
}
