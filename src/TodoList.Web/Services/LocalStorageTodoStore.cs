using System.Text.Json;
using Microsoft.JSInterop;
using TodoList.Web.Models;

namespace TodoList.Web.Services;

public sealed class LocalStorageTodoStore(IJSRuntime js) : ITodoStore
{
    public const string Key = "todolist.items.v1";

    public async Task<IReadOnlyList<TodoItem>> LoadAsync()
    {
        string? json;
        try
        {
            json = await js.InvokeAsync<string?>("localStorage.getItem", Key);
        }
        catch (JSException ex)
        {
            throw new StorageUnavailableException("Não consegui ler as tarefas salvas neste navegador.", ex);
        }

        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            return TodoSerializer.Deserialize(json);
        }
        catch (JsonException ex)
        {
            throw new StorageUnavailableException("As tarefas salvas estavam corrompidas e foram ignoradas.", ex);
        }
    }

    public async Task SaveAsync(IReadOnlyList<TodoItem> items)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", Key, TodoSerializer.Serialize(items));
        }
        catch (JSException ex)
        {
            throw new StorageUnavailableException("Não consegui salvar neste navegador. As tarefas valem só até fechar a página.", ex);
        }
    }
}
