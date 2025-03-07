using Grpc.Core;
using Grpc.Net.Client;
using KIlian.Dashboard.Components;
using KIlian.Generated.Rpc.Conversations;
using KIlian.Shared.Configuration.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddFluentUIComponents();

builder.Services.AddScoped<ChannelBase>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return GrpcChannel.ForAddress(config.GetRequiredValue("KIlian:Endpoints:Http2"));
});

builder.Services.AddScoped<Conversation.ConversationClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();