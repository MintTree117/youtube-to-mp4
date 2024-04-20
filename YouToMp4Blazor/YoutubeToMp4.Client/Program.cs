using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using YouToMp4Blazor.Client.Services;
using YoutubeToMp4.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient( "client", c => { c.BaseAddress = new Uri( HttpConsts.ProductionAddress ); } );
//builder.Services.AddHttpClient( "client", c => { c.BaseAddress = new Uri( HttpConsts.DevelopmentAddress ); } );
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<ClientAuthenticator>();
builder.Services.AddScoped<ClientYoutube>();
builder.Services.AddScoped<ClientAdminService>();

await builder.Build().RunAsync();
