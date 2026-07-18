using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BackendTestingStudio.Core.History;
using BackendTestingStudio.Core.Plugins;
using BackendTestingStudio.Http;
using BackendTestingStudio.Plugins;
using BackendTestingStudio.Storage;
using BackendTestingStudio.UI.History;
using BackendTestingStudio.UI.Components;

var builder = WebApplication.CreateBuilder(args);

// dotnet run --project may keep the caller's working directory and environment.
// Explicitly load the development manifest so framework and scoped CSS assets remain available.
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBackendTestingStudioHttp();
builder.Services.AddBackendTestingStudioStorage();
builder.Services.AddSingleton<IPluginLoader, PluginLoader>();
builder.Services.AddScoped<IRequestHistoryService, RequestHistoryService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
