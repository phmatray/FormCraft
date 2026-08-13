using FormCraft;
using FormCraft.DemoFluentApp;
using FormCraft.ForFluentUI.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddFluentUIComponents();
builder.Services.AddFormCraft();

// The whole point of this second app: AddFormCraftFluentUI() would throw if the MudBlazor adapter
// were registered here, so the Fluent showcase gets a container of its own.
builder.Services.AddFormCraftFluentUI();

await builder.Build().RunAsync();
