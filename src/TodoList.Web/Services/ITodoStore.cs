using TodoList.Web.Models;

namespace TodoList.Web.Services;

/// <summary>Where the tasks are kept. In the browser it is localStorage; tests use an in-memory fake.</summary>
public interface ITodoStore
{
    /// <exception cref="StorageUnavailableException">The stored data could not be read.</exception>
    Task<IReadOnlyList<TodoItem>> LoadAsync();

    /// <exception cref="StorageUnavailableException">The data could not be written.</exception>
    Task SaveAsync(IReadOnlyList<TodoItem> items);
}

/// <summary>Storage failed in a way the user should be told about (blocked storage, quota, corrupted data).</summary>
public sealed class StorageUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
