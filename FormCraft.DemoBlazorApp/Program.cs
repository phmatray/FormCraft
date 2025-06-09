using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FormCraft.DemoBlazorApp.Components;
using FormCraft.DemoBlazorApp.Services;
using FormCraft;
using FormCraft.ForMudBlazor.Extensions;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add services to the container.
builder.Services.AddMudServices();
builder.Services.AddFormCraft();
builder.Services.AddFormCraftMudBlazor(); // Add MudBlazor-specific renderers
builder.Services.AddScoped<IMarkdownService>(sp => 
    new MarkdownService(sp.GetRequiredService<HttpClient>()));
builder.Services.AddScoped<FormCodeGeneratorService>();
builder.Services.AddScoped<IVersionService>(sp => 
    new VersionService(sp.GetRequiredService<HttpClient>()));

// Custom field renderers are now registered by AddFormCraftMudBlazor

await builder.Build().RunAsync();