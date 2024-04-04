using Blazored.LocalStorage;
using YouToMp4.Shared;
using YouToMp4Blazor.Client.Services;
using YouToMp4Blazor.Components;
using YouToMp4Blazor.Data;
using YouToMp4Blazor.Features.Authentication;
using YouToMp4Blazor.Features.FFmpeg;
using YouToMp4Blazor.Features.Youtube;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCors( options =>
{
    options.AddPolicy( "CorsPolicy",
        b =>
        {
            b.WithMethods( "GET", "POST", "PUT", "DELETE" )
                .AllowCredentials()
                .SetIsOriginAllowed( hostName => true );
        } );
} );

builder.Services.AddLogging();

//builder.Services.AddHttpClient( "client", c => { c.BaseAddress = new Uri( HttpConsts.DevelopmentAddress ); } );
builder.Services.AddHttpClient( "client", c => { c.BaseAddress = new Uri( HttpConsts.ProductionAddress ); } );
// TODO: Figure out why environment variables arent being read here
// builder.Services.AddHttpClient( "client", c => { c.BaseAddress = new Uri( builder.Configuration[ "BaseAddress" ] ?? string.Empty ); } );
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddSingleton<DatabaseContext>();

builder.Services.AddScoped<ClientAdminService>();
builder.Services.AddScoped<ClientAuthenticator>();
builder.Services.AddScoped<ClientYoutube>();

builder.Services.AddSingleton<AuthRepository>();
builder.Services.AddSingleton<AuthManager>();

builder.Services.AddScoped<FFmpegService>();

builder.Services.AddScoped<YoutubeBrowser>();
builder.Services.AddScoped<YoutubeDownloader>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if ( app.Environment.IsDevelopment() )
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler( "/Error", createScopeForErrors: true );
    app.UseHsts(); // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseCors( "CorsPolicy" );

app.UseWhen( context => context.Request.Path.StartsWithSegments( "/api" ), appBuilder => { appBuilder.UseMiddleware<AuthMiddleware>(); } );
app.MapAuthenticatorEndpoints();
app.MapYoutubeEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies( typeof( YouToMp4Blazor.Client._Imports ).Assembly );

app.Run();
