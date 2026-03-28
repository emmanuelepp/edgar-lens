using EdgarLens.Core.Models;
using EdgarLens.Core.Interfaces;
using EdgarLens.Infrastructure.Edgar;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<EdgarSettings>(
    builder.Configuration.GetSection("EdgarSettings"));

// HTTP Clients
builder.Services.AddHttpClient<IEdgarClient, EdgarClient>();

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EdgarLens API v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.MapControllers();

app.Run();