using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendApp", builder =>
    {
        builder.WithOrigins("http://localhost:4200") // Frontend origin
               .AllowAnyMethod()                       // Allow all HTTP methods (GET, POST, etc.)
               .AllowAnyHeader();                      // Allow all headers
    });
});

var app = builder.Build();

// Use CORS policy before other middleware
app.UseCors("AllowFrontendApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
