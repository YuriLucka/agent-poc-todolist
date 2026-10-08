using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TodoList.Tests;

public class TodoApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_ReturnsCreatedItem()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { title = "Comprar leite" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<TodoItem>();
        Assert.Equal("Comprar leite", item!.Title);
        Assert.False(item.Done);
    }

    [Fact]
    public async Task Create_WithEmptyTitle_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { title = "  " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Toggle_FlipsDone()
    {
        var created = await (await _client.PostAsJsonAsync("/api/todos", new { title = "Estudar" }))
            .Content.ReadFromJsonAsync<TodoItem>();

        var response = await _client.PutAsync($"/api/todos/{created!.Id}/toggle", null);
        var toggled = await response.Content.ReadFromJsonAsync<TodoItem>();

        Assert.True(toggled!.Done);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/todos/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
