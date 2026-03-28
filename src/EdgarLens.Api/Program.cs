using EdgarLens.Core.Interfaces;
using EdgarLens.Core.Models;
using EdgarLens.Infrastructure.Edgar;
using EdgarLens.Infrastructure.Rag;
using EdgarLens.Api.Mcp;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<EdgarSettings>(
    builder.Configuration.GetSection("EdgarSettings"));

// HTTP Clients
builder.Services.AddHttpClient<IEdgarClient, EdgarClient>();
builder.Services.AddHttpClient<IFilingDownloader, FilingDownloader>();
builder.Services.AddHttpClient<IChunkingService, ChunkingService>();
builder.Services.AddHttpClient<IRagService, RagService>();

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MCP
builder.Services.AddMcpServer().WithHttpTransport(options => options.Stateless = true).WithTools<EdgarMcpServer>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EdgarLens API v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.MapControllers();
app.MapMcp("/mcp");

app.Run();