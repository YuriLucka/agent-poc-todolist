namespace TodoList.Web.Models;

public sealed record TodoItem(int Id, string Title, bool Done, Priority Priority, DateOnly? DueDate = null);
