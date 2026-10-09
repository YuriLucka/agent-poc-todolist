using TodoList.Web.Models;

namespace TodoList.Web.Services;

public sealed record AddResult(bool Ok, string? Error, TodoItem? Item)
{
    public static AddResult Success(TodoItem item) => new(true, null, item);
    public static AddResult Fail(string error) => new(false, error, null);
}

/// <summary>The task list and its rules. The page only calls this; it never touches storage directly.</summary>
public sealed class TodoService(ITodoStore store)
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 500;

    private List<TodoItem> _items = [];
    private bool _initialized;

    public IReadOnlyList<TodoItem> Items => _items;
    public int PendingCount => _items.Count(t => !t.Done);
    public int DoneCount => _items.Count(t => t.Done);

    /// <summary>Set when the browser could not read or write the saved tasks; null when everything is fine.</summary>
    public string? StorageError { get; private set; }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            _items = (await store.LoadAsync()).OrderBy(t => t.Id).ToList();
        }
        catch (StorageUnavailableException ex)
        {
            _items = [];
            StorageError = ex.Message;
        }
    }

    public async Task<AddResult> AddAsync(string? title, Priority? priority, DateOnly? dueDate, string? description = null)
    {
        var clean = title?.Trim() ?? "";
        if (clean.Length == 0) return AddResult.Fail("Escreva o título da tarefa.");
        if (clean.Length > MaxTitleLength) return AddResult.Fail($"O título pode ter no máximo {MaxTitleLength} caracteres.");
        if (priority is null) return AddResult.Fail("Escolha a prioridade.");
        var note = description?.Trim();
        if (note?.Length > MaxDescriptionLength) return AddResult.Fail($"A descrição pode ter no máximo {MaxDescriptionLength} caracteres.");
        if (string.IsNullOrEmpty(note)) note = null;

        var id = _items.Count == 0 ? 1 : _items.Max(t => t.Id) + 1;
        var item = new TodoItem(id, clean, false, priority.Value, dueDate, note);
        _items.Add(item);
        await PersistAsync();
        return AddResult.Success(item);
    }

    public async Task ToggleAsync(int id)
    {
        var index = _items.FindIndex(t => t.Id == id);
        if (index < 0) return;

        _items[index] = _items[index] with { Done = !_items[index].Done };
        await PersistAsync();
    }

    public async Task RemoveAsync(int id)
    {
        if (_items.RemoveAll(t => t.Id == id) > 0)
            await PersistAsync();
    }

    private async Task PersistAsync()
    {
        try
        {
            await store.SaveAsync(_items);
            StorageError = null;
        }
        catch (StorageUnavailableException ex)
        {
            // Keep working in memory: the task is on screen, only the saving failed.
            StorageError = ex.Message;
        }
    }
}
