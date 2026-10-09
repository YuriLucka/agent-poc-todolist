namespace TodoList.Web.Models;

public enum Priority
{
    Baixa,
    Media,
    Alta
}

public static class PriorityExtensions
{
    /// <summary>Text shown to the user (the enum names have no accents).</summary>
    public static string Label(this Priority priority) => priority switch
    {
        Priority.Baixa => "Baixa",
        Priority.Media => "Média",
        Priority.Alta => "Alta",
        _ => priority.ToString()
    };
}
