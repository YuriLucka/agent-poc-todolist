# agent-poc-todolist

A to-do list with priority and due date, built as a **static Blazor WebAssembly** app. There is no server:
the page runs in the browser and saves the tasks in that browser's `localStorage`.

Live site (after the first publish): https://yurilucka.github.io/agent-poc-todolist/

## Run locally

```powershell
dotnet run --project src/TodoList.Web --urls http://localhost:5080
dotnet test
```

## How it is published

`.github/workflows/pages.yml` runs on every push to `main`: it runs the tests, publishes the app,
rewrites the base path for `/<repository>/` and deploys to GitHub Pages. `.github/workflows/ci.yml` runs the
tests and a publish on every pull request, without deploying.

One-time setup in the repository: **Settings > Pages > Build and deployment > Source: GitHub Actions**.

## Things to know

- The tasks belong to the browser: another browser or device shows its own list, and clearing the site data
  erases it.
- GitHub Pages serves the files with a cache of about 10 minutes, so a new version can take a moment to
  appear. A hard refresh (Ctrl+F5) forces it.
- The first visit downloads the .NET runtime (a few MB), so it opens slower than a plain HTML page.
- On a private repository, GitHub Pages needs a paid plan, and the published site is public.
