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
    public async Task Create_WithoutPriority_DefaultsToMedia()
    {
        var item = await (await _client.PostAsJsonAsync("/api/todos", new { title = "Ler livro" }))
            .Content.ReadFromJsonAsync<TodoItem>();

        Assert.Equal("media", item!.Priority);
    }

    [Theory]
    [InlineData("baixa")]
    [InlineData("media")]
    [InlineData("alta")]
    public async Task Create_WithPriority_StoresPriority(string priority)
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { title = "Pagar contas", priority });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<TodoItem>();
        Assert.Equal(priority, item!.Priority);
    }

    [Fact]
    public async Task Create_WithInvalidPriority_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { title = "Correr", priority = "urgente" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_NormalizesPriority()
    {
        var item = await (await _client.PostAsJsonAsync("/api/todos", new { title = "Lavar carro", priority = " ALTA " }))
            .Content.ReadFromJsonAsync<TodoItem>();

        Assert.Equal("alta", item!.Priority);
    }

    [Fact]
    public async Task List_IncludesPriority()
    {
        var created = await (await _client.PostAsJsonAsync("/api/todos", new { title = "Regar plantas", priority = "baixa" }))
            .Content.ReadFromJsonAsync<TodoItem>();

        var todos = await _client.GetFromJsonAsync<List<TodoItem>>("/api/todos");

        Assert.Equal("baixa", todos!.Single(t => t.Id == created!.Id).Priority);
    }

    [Fact]
    public async Task Toggle_KeepsPriority()
    {
        var created = await (await _client.PostAsJsonAsync("/api/todos", new { title = "Ligar mãe", priority = "alta" }))
            .Content.ReadFromJsonAsync<TodoItem>();

        var toggled = await (await _client.PutAsync($"/api/todos/{created!.Id}/toggle", null))
            .Content.ReadFromJsonAsync<TodoItem>();

        Assert.Equal("alta", toggled!.Priority);
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
