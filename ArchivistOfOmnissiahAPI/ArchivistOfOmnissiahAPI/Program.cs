using Microsoft.EntityFrameworkCore;
using Archivist.Data;
using Archivist.Middleware;
using Archivist.Models;
using Archivist.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<WikiService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Регистрируем DbContext
builder.Services.AddDbContext<ArchivistDbContext>(options =>
    options.UseNpgsql(connectionString, x => x.UseVector())); // .UseVector() обязателен!

builder.Services.AddHttpClient("Ollama", client =>
{
   client.DefaultRequestHeaders.Add("User-Agent", "ArchivistBot/1.0");
   client.BaseAddress = new Uri("http://localhost:11434/");
   client.Timeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Адрес твоего фронтенда
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IBackgroundTaskQueue>(new BackgroundTaskQueue(100));
builder.Services.AddHostedService<LoreBackgroundWorker>();


var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();

