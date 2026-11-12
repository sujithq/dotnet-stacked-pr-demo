using Bookshelf.Api.Models;
using Bookshelf.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bookshelf.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController(IBookCatalogService catalog) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<Book>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<Book>> GetAll([FromQuery] string? query = null) => Ok(catalog.GetAll(query));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<Book>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Book> GetById(Guid id)
    {
        var book = catalog.GetById(id);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpPost]
    [ProducesResponseType<Book>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<Book> Create(CreateBookRequest request)
    {
        var result = catalog.Add(request);
        switch (result.Status)
        {
            case AddBookStatus.Success:
                var book = result.Book ?? throw new InvalidOperationException("A successful add must return a book.");
                return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
            case AddBookStatus.ValidationErrors:
                foreach (var (field, errors) in result.Errors)
                {
                    foreach (var error in errors)
                    {
                        ModelState.AddModelError(field, error);
                    }
                }

                return ValidationProblem(ModelState);
            case AddBookStatus.DuplicateIsbn:
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Duplicate ISBN",
                    detail: "A book with this ISBN already exists.");
            default:
                throw new InvalidOperationException($"Unexpected add status: {result.Status}.");
        }
    }
}
