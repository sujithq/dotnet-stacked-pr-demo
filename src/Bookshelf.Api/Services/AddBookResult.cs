using Bookshelf.Api.Models;

namespace Bookshelf.Api.Services;

public enum AddBookStatus
{
    Success,
    ValidationErrors,
    DuplicateIsbn
}

public sealed record AddBookResult(
    AddBookStatus Status,
    Book? Book,
    IReadOnlyDictionary<string, string[]> Errors);
