using System.Text.Json;
using System.Text.Json.Serialization;
using TodoList.Web.Models;

namespace TodoList.Web.Services;

public static class TodoSerializer
{
    // Enums as names ("Alta") so the saved data stays readable and survives reordering the enum.
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(IReadOnlyList<TodoItem> items) => JsonSerializer.Serialize(items, Options);

    /// <exception cref="JsonException">The text is not a valid list of tasks.</exception>
    public static IReadOnlyList<TodoItem> Deserialize(string json) =>
        JsonSerializer.Deserialize<List<TodoItem>>(json, Options) ?? [];
}
