# Bookshelf: a .NET 10 stacked pull requests demo

A deliberately small ASP.NET Core Web API (.NET 10, controller-based) used to showcase
[GitHub stacked pull requests](https://docs.github.com/en/pull-requests/get-started/about-stacked-prs).

`main` holds only the app skeleton: a health endpoint, a test project and CI. The book catalog
feature is built on top of it as a stack of small, dependent pull requests. Each pull request
targets the branch below it, so reviewers only see that layer's diff.

```text
   ┌── 3. Book search      (base: 2. Books API)        ← top
  ┌── 2. Books API         (base: 1. Catalog domain)
 ┌── 1. Catalog domain     (base: main)                ← bottom
main
```

| Layer | What it adds | Depends on |
| --- | --- | --- |
| 1. Catalog domain | `Book` model, in-memory repository and `IBookCatalogService` with validation rules, plus unit tests | `main` |
| 2. Books API | `BooksController` (`GET /api/books`, `GET /api/books/{id}`, `POST /api/books`) and DI wiring, plus integration tests | Layer 1 |
| 3. Book search | `GET /api/books?query=` filtering by title or author, plus tests | Layer 2 |

## Try the stack

- Review bottom to top: open the bottom pull request and use the stack map in the merge box to
  move between layers.
- Merge the top pull request to land the whole stack, or merge a mid-stack pull request to land it
  together with the layers below it. The remaining pull requests are rebased and retargeted to `main`.
- To work on the stack locally, install the
  [`gh stack`](https://docs.github.com/en/pull-requests/reference/stacked-prs-cli-commands) extension
  and run `gh stack checkout <pr-number>`.

## Run locally

Requires the .NET 10 SDK (pinned in `global.json`). NuGet packages are restored from the feed in
`nuget.config`.

```bash
dotnet restore Bookshelf.slnx
dotnet build Bookshelf.slnx --no-restore
dotnet test Bookshelf.slnx --no-build
dotnet run --project src/Bookshelf.Api
```

Then call `GET http://localhost:5268/api/health`, or use `src/Bookshelf.Api/Bookshelf.Api.http`.

## CI

[`ci.yml`](.github/workflows/ci.yml) restores, builds and tests the solution on every push to `main`
and on every pull request, so each layer of the stack gets its own checks.

## Learn more

- [About stacked pull requests](https://docs.github.com/en/pull-requests/get-started/about-stacked-prs)
- [Stacked pull requests quickstart](https://docs.github.com/en/pull-requests/get-started/stacked-prs-quickstart)
- [Stacked pull requests CLI commands](https://docs.github.com/en/pull-requests/reference/stacked-prs-cli-commands)
- [Stacked pull requests APIs and webhooks](https://docs.github.com/en/pull-requests/reference/stacked-pull-requests-apis-and-webhooks)
