using CloudBackend.Data;
using CloudBackend.Models;
using Microsoft.EntityFrameworkCore;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// --- KEY VAULT (Production only) ---
if (builder.Environment.IsProduction())
{
    var vaultName = builder.Configuration["KeyVaultName"];
    if (!string.IsNullOrEmpty(vaultName))
    {
        var keyVaultEndpoint = new Uri($"https://{vaultName}.vault.azure.net/");
        builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
    }
}

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Connection string
var connectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    ));

// ✅ CORS — konkretny frontend (ważne!)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://cloud-task-manager-frontend-96346-c7a9g7gacdbrhbgm.germanywestcentral-01.azurewebsites.net")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// ✅ ważne dla Azure
app.UseHttpsRedirection();

app.UseRouting();

// ✅ KLUCZOWE: CORS w dobrym miejscu
app.UseCors("AllowFrontend");

app.UseAuthorization();

// Controllers
app.MapControllers();

// ✅ Obsługa preflight (OPTIONS) – zabezpieczenie
app.MapMethods("{*path}", new[] { "OPTIONS" }, () => Results.Ok());

// Root endpoint (health check)
app.MapGet("/", () => Results.Ok("API działa 🚀"));

// DB init
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        db.Database.Migrate();

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