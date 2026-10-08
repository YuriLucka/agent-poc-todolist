using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var items = new ConcurrentDictionary<int, TodoItem>();
var nextId = 0;
string[] priorities = ["baixa", "media", "alta"];

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/todos", () => items.Values.OrderBy(t => t.Id));

app.MapPost("/api/todos", (CreateTodo input) =>
{
    if (string.IsNullOrWhiteSpace(input.Title))
        return Results.BadRequest(new { error = "Title is required." });

    var priority = string.IsNullOrWhiteSpace(input.Priority) ? "media" : input.Priority.Trim().ToLowerInvariant();
    if (!priorities.Contains(priority))
        return Results.BadRequest(new { error = "Priority must be baixa, media or alta." });

    var id = Interlocked.Increment(ref nextId);
    var item = new TodoItem(id, input.Title.Trim(), false, priority);
    items[id] = item;
    return Results.Created($"/api/todos/{id}", item);
});

app.MapPut("/api/todos/{id:int}/toggle", (int id) =>
{
    if (!items.TryGetValue(id, out var item))
        return Results.NotFound();

    var updated = item with { Done = !item.Done };
    items[id] = updated;
    return Results.Ok(updated);
});

app.MapDelete("/api/todos/{id:int}", (int id) =>
    items.TryRemove(id, out _) ? Results.NoContent() : Results.NotFound());

app.Run();

public record TodoItem(int Id, string Title, bool Done, string Priority);
public record CreateTodo(string Title, string? Priority = null);

public partial class Program { }
