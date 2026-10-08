# TodoList

Small ASP.NET minimal API (`src/TodoList.Api`) serving a static page (`wwwroot/index.html`) and an in-memory todo API.
Tests live in `tests/TodoList.Tests` (xUnit + WebApplicationFactory).

- Run tests: `dotnet test`
- Run app: `dotnet run --project src/TodoList.Api --urls http://localhost:5080`
- UI is plain HTML/JS in `wwwroot/index.html`. No frameworks, no build step.
- Any new API field must be covered by a test and shown in the UI.
