using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json");
builder.Services.AddOcelot();

var app = builder.Build();
await app.UseOcelot();
app.MapGet("/", () => "Wisdom Pet Medicine - API Gateway");

app.Run();
