using TodoList.Web.Models;
using TodoList.Web.Services;

namespace TodoList.Tests;

/// <summary>In-memory stand-in for the browser localStorage.</summary>
public sealed class FakeStore : ITodoStore
{
    public List<TodoItem> Saved { get; set; } = [];
    public int SaveCount { get; private set; }
    public StorageUnavailableException? LoadFailure { get; set; }
    public StorageUnavailableException? SaveFailure { get; set; }

    public Task<IReadOnlyList<TodoItem>> LoadAsync() =>
        LoadFailure is { } ex
            ? Task.FromException<IReadOnlyList<TodoItem>>(ex)
            : Task.FromResult<IReadOnlyList<TodoItem>>(Saved.ToList());

    public Task SaveAsync(IReadOnlyList<TodoItem> items)
    {
        if (SaveFailure is { } ex) return Task.FromException(ex);
        Saved = items.ToList();
        SaveCount++;
        return Task.CompletedTask;
    }
}

public class TodoServiceTests
{
    private readonly FakeStore _store = new();
    private readonly TodoService _service;

    public TodoServiceTests() => _service = new TodoService(_store);

    // ---- add ------------------------------------------------------------------

    [Fact]
    public async Task Add_ValidTask_IsStoredWithTrimmedTitle()
    {
        await _service.InitializeAsync();

        var result = await _service.AddAsync("  Comprar leite  ", Priority.Alta, new DateOnly(2030, 1, 15));

        Assert.True(result.Ok);
        var item = Assert.Single(_service.Items);
        Assert.Equal("Comprar leite", item.Title);
        Assert.Equal(Priority.Alta, item.Priority);
        Assert.Equal(new DateOnly(2030, 1, 15), item.DueDate);
        Assert.False(item.Done);
        Assert.Equal(item, Assert.Single(_store.Saved));
    }

    [Fact]
    public async Task Add_WithoutDueDate_KeepsItNull()
    {
        await _service.InitializeAsync();

        await _service.AddAsync("Sem prazo", Priority.Baixa, null);

        Assert.Null(Assert.Single(_service.Items).DueDate);
    }

    [Fact]
    public async Task Add_AssignsNextId_AsHighestPlusOne()
    {
        await _service.InitializeAsync();
        await _service.AddAsync("A", Priority.Baixa, null);
        await _service.AddAsync("B", Priority.Baixa, null);
        await _service.RemoveAsync(2);

        var result = await _service.AddAsync("C", Priority.Baixa, null);

        Assert.Equal(2, result.Item!.Id);
        Assert.Equal([1, 2], _service.Items.Select(t => t.Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Add_WithEmptyTitle_IsRejected_AndNothingIsSaved(string? title)
    {
        await _service.InitializeAsync();

        var result = await _service.AddAsync(title, Priority.Media, null);

        Assert.False(result.Ok);
        Assert.Contains("título", result.Error);
        Assert.Empty(_service.Items);
        Assert.Equal(0, _store.SaveCount);
    }

    [Fact]
    public async Task Add_WithTooLongTitle_IsRejected()
    {
        await _service.InitializeAsync();

        var result = await _service.AddAsync(new string('x', TodoService.MaxTitleLength + 1), Priority.Media, null);

        Assert.False(result.Ok);
        Assert.Contains("200", result.Error);
        Assert.Empty(_service.Items);
    }

    [Fact]
    public async Task Add_WithoutPriority_IsRejected()
    {
        await _service.InitializeAsync();

        var result = await _service.AddAsync("Tarefa", null, null);

        Assert.False(result.Ok);
        Assert.Contains("prioridade", result.Error);
        Assert.Empty(_service.Items);
    }

    [Fact]
    public async Task Add_WithDescription_TrimsAndStoresIt()
    {
        await _service.InitializeAsync();

        await _service.AddAsync("A", Priority.Baixa, null, "  detalhes  ");

        Assert.Equal("detalhes", Assert.Single(_store.Saved).Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Add_WithBlankDescription_StoresNull(string? description)
    {
        await _service.InitializeAsync();

        await _service.AddAsync("A", Priority.Baixa, null, description);

        Assert.Null(Assert.Single(_store.Saved).Description);
    }

    [Fact]
    public async Task Add_WithTooLongDescription_IsRejected()
    {
        await _service.InitializeAsync();

        var result = await _service.AddAsync("A", Priority.Baixa, null, new string('x', TodoService.MaxDescriptionLength + 1));

        Assert.False(result.Ok);
        Assert.Empty(_store.Saved);
    }

    [Fact]
    public async Task Counters_SeparatePendingFromDone()
    {
        await _service.InitializeAsync();
        await _service.AddAsync("A", Priority.Baixa, null);
        await _service.AddAsync("B", Priority.Baixa, null);
        await _service.ToggleAsync(1);

        Assert.Equal(1, _service.PendingCount);
        Assert.Equal(1, _service.DoneCount);
    }

    [Fact]
    public void Deserialize_OfOldDataWithoutDescription_Works()
    {
        var items = TodoSerializer.Deserialize("[{\"id\":1,\"title\":\"A\",\"done\":false,\"priority\":\"Alta\"}]");

        Assert.Null(Assert.Single(items).Description);
    }

    // ---- toggle and remove --------------------------------------------------------

    [Fact]
    public async Task Toggle_FlipsDone_AndKeepsTheOtherFields()
    {
        await _service.InitializeAsync();
        await _service.AddAsync("Estudar", Priority.Alta, new DateOnly(2030, 3, 10));

        await _service.ToggleAsync(1);
        var done = Assert.Single(_service.Items);
        await _service.ToggleAsync(1);
        var undone = Assert.Single(_service.Items);

        Assert.True(done.Done);
        Assert.Equal(Priority.Alta, done.Priority);
        Assert.Equal(new DateOnly(2030, 3, 10), done.DueDate);
        Assert.False(undone.Done);
        Assert.Equal(undone, Assert.Single(_store.Saved));
    }

    [Fact]
    public async Task Toggle_UnknownId_DoesNothing()
    {
        await _service.InitializeAsync();
        await _service.AddAsync("A", Priority.Baixa, null);
        var savesBefore = _store.SaveCount;

        await _service.ToggleAsync(999);

        Assert.Equal(savesBefore, _store.SaveCount);
    }

    [Fact]
    public async Task Remove_DeletesOnlyThatTask()
    {
        await _service.InitializeAsync();
        await _service.AddAsync("A", Priority.Baixa, null);
        await _service.AddAsync("B", Priority.Baixa, null);

        await _service.RemoveAsync(1);

        Assert.Equal("B", Assert.Single(_service.Items).Title);
        Assert.Equal("B", Assert.Single(_store.Saved).Title);
    }

    [Fact]
    public async Task Remove_UnknownId_DoesNotSave()
    {
        await _service.InitializeAsync();
        await _service.AddAsync("A", Priority.Baixa, null);
        var savesBefore = _store.SaveCount;

        await _service.RemoveAsync(999);

        Assert.Equal(savesBefore, _store.SaveCount);
        Assert.Single(_service.Items);
    }

    // ---- loading ----------------------------------------------------------------

    [Fact]
    public async Task Initialize_LoadsSavedTasks_InIdOrder()
    {
        _store.Saved = [new(3, "C", false, Priority.Baixa), new(1, "A", true, Priority.Alta)];

        await _service.InitializeAsync();

        Assert.Equal([1, 3], _service.Items.Select(t => t.Id));
        Assert.Null(_service.StorageError);
    }

    [Fact]
    public async Task Add_AfterLoading_ContinuesFromTheHighestId()
    {
        _store.Saved = [new(7, "Antiga", false, Priority.Media)];
        await _service.InitializeAsync();

        var result = await _service.AddAsync("Nova", Priority.Media, null);

        Assert.Equal(8, result.Item!.Id);
    }

    [Fact]
    public async Task Initialize_RunsOnlyOnce()
    {
        _store.Saved = [new(1, "A", false, Priority.Media)];
        await _service.InitializeAsync();
        await _service.AddAsync("B", Priority.Media, null);

        _store.Saved = []; // a second load would wipe the list
        await _service.InitializeAsync();

        Assert.Equal(2, _service.Items.Count);
    }

    // ---- storage failures ---------------------------------------------------------

    [Fact]
    public async Task LoadFailure_StartsEmpty_AndReportsTheProblem()
    {
        _store.LoadFailure = new StorageUnavailableException("dados corrompidos");

        await _service.InitializeAsync();

        Assert.Empty(_service.Items);
        Assert.Equal("dados corrompidos", _service.StorageError);
    }

    [Fact]
    public async Task SaveFailure_KeepsTheTaskOnScreen_AndReportsTheProblem()
    {
        await _service.InitializeAsync();
        _store.SaveFailure = new StorageUnavailableException("sem espaço");

        var result = await _service.AddAsync("Tarefa", Priority.Media, null);

        Assert.True(result.Ok);
        Assert.Single(_service.Items);
        Assert.Equal("sem espaço", _service.StorageError);
    }

    [Fact]
    public async Task SaveFailure_IsClearedByTheNextSuccessfulSave()
    {
        await _service.InitializeAsync();
        _store.SaveFailure = new StorageUnavailableException("sem espaço");
        await _service.AddAsync("A", Priority.Media, null);

        _store.SaveFailure = null;
        await _service.AddAsync("B", Priority.Media, null);

        Assert.Null(_service.StorageError);
        Assert.Equal(2, _store.Saved.Count);
    }
}

public class TodoSerializerTests
{
    [Fact]
    public void RoundTrip_PreservesEveryField()
    {
        IReadOnlyList<TodoItem> items =
        [
            new(1, "Com prazo", true, Priority.Alta, new DateOnly(2030, 1, 15)),
            new(2, "Sem prazo", false, Priority.Baixa)
        ];

        var back = TodoSerializer.Deserialize(TodoSerializer.Serialize(items));

        Assert.Equal(items, back);
    }

    [Fact]
    public void Serialize_WritesReadablePriorityAndIsoDate()
    {
        var json = TodoSerializer.Serialize([new TodoItem(1, "A", false, Priority.Media, new DateOnly(2030, 5, 20))]);

        Assert.Contains("\"priority\":\"Media\"", json);
        Assert.Contains("\"dueDate\":\"2030-05-20\"", json);
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("{\"id\":1}")]
    [InlineData("[{\"id\":1,\"title\":\"A\",\"done\":false,\"priority\":\"Urgente\"}]")]
    public void Deserialize_OfCorruptedData_Throws(string json)
    {
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => TodoSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_OfNullJson_ReturnsEmpty()
    {
        Assert.Empty(TodoSerializer.Deserialize("null"));
    }
}

public class PriorityTests
{
    [Theory]
    [InlineData(Priority.Baixa, "Baixa")]
    [InlineData(Priority.Media, "Média")]
    [InlineData(Priority.Alta, "Alta")]
    public void Label_UsesPortugueseWithAccents(Priority priority, string expected) =>
        Assert.Equal(expected, priority.Label());
}
