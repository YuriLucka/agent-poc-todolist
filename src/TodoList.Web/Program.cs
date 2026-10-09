using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TodoList.Web;
using TodoList.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// No server: the tasks live in this browser's localStorage.
builder.Services.AddScoped<ITodoStore, LocalStorageTodoStore>();
builder.Services.AddScoped<TodoService>();

await builder.Build().RunAsync();
