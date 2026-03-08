using CloudBackend.Data;
using Microsoft.EntityFrameworkCore;
using CloudBackend.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();

        if (!context.Tasks.Any())
        {
            context.Tasks.AddRange(
                new CloudTask { Name = "Zrobić kawę", IsCompleted = true },
                new CloudTask { Name = "Uruchomić projekt w Dockerze", IsCompleted = false }
            );

            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Błąd podczas tworzenia bazy: {ex.Message}");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cloud API V1");
    c.RoutePrefix = string.Empty;
});

app.UseCors();
app.MapControllers();
app.Run();
