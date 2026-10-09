# TodoList

Static Blazor WebAssembly app: there is no server. It runs entirely in the browser, keeps the tasks in
`localStorage`, and is published to GitHub Pages on every push to `main`.

## Layout
- `src/TodoList.Web/Models` - `TodoItem`, `Priority`.
- `src/TodoList.Web/Services` - the rules and the storage. `TodoService` holds all the logic;
  `ITodoStore` is where tasks are saved (`LocalStorageTodoStore` in the browser).
- `src/TodoList.Web/Pages/Home.razor` - the only screen. Keep rules out of it; put them in `TodoService`.
- `src/TodoList.Web/wwwroot/css/app.css` - all styling. Plain CSS, no framework.
- `tests/TodoList.Tests` - xUnit tests for the services, using an in-memory `FakeStore`.

## Commands
- Tests: `dotnet test`
- Run: `dotnet run --project src/TodoList.Web --urls http://localhost:5080`
  The first load takes a few seconds because the browser downloads the .NET runtime.
- To look at the screen with data in it, write a small Playwright script (Node, `playwright` is installed
  globally) that fills the form before taking the screenshot. The list starts empty.

## Rules
- Any new field goes through all four places: `TodoItem`, `TodoService` (validation), a test, and the screen.
- The text shown to the user is in Brazilian Portuguese.
- Do not edit anything under `.github/workflows`: publishing is configured by hand.
