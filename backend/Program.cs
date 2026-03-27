using CloudBackend.Data;
using CloudBackend.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Connection string (Azure ENV > appsettings)
var connectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// DbContext
// Rejestracja bazy danych z mechanizmem ponawiania prób (Retry Logic)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    ));

// CORS (na start wszystko otwarte)
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

// 🔥 ROOT endpoint (żeby Azure nie wywalał błędu)
app.MapGet("/", () => Results.Ok("API działa 🚀"));

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Middleware
app.UseCors();
app.MapControllers();

// 🔥 DB init (bezpieczniejsza wersja)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        db.Database.Migrate(); // zamiast EnsureCreated()

        if (!db.Tasks.Any())
        {
            db.Tasks.AddRange(
                new CloudTask { Name = "Zrobić kawę", IsCompleted = true },
                new CloudTask { Name = "Uruchomić projekt w Dockerze", IsCompleted = false }
            );

            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("DB ERROR: " + ex.Message);
    }
}

app.Run();