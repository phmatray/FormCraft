using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FormCraft.DemoBlazorApp.Components;
using FormCraft.DemoBlazorApp.Services;
using FormCraft;
using FormCraft.ForMudBlazor.Extensions;
using MudBlazor.Services;
using FluentValidation;
using FormCraft.DemoBlazorApp.Components.Pages;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add services to the container.
builder.Services.AddMudServices();
builder.Services.AddFormCraft();
builder.Services.AddFormCraftMudBlazor(); // Add MudBlazor-specific renderers
builder.Services.AddScoped<IMarkdownService>(sp =>
    new MarkdownService(sp.GetRequiredService<HttpClient>()));
builder.Services.AddScoped<IVersionService>(sp =>
    new VersionService(sp.GetRequiredService<HttpClient>()));

// Register FluentValidation validators
builder.Services.AddScoped<IValidator<FluentValidationDemo.CustomerModel>, FluentValidationDemo.CustomerValidator>();

// Custom field renderers are now registered by AddFormCraftMudBlazor

await builder.Build().RunAsync();