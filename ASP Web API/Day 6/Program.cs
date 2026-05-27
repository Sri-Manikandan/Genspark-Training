using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ExcelAPI.Interfaces;
using ExcelAPI.Repositories;
using ExcelAPI.Services;
using ExcelAPI.Models;
using ExcelAPI.Contexts;
using Serilog;  

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
.ReadFrom.Configuration(builder.Configuration)
.Enrich.FromLogContext()
.WriteTo.Console()
.WriteTo.File("logs/MyAppLog.txt")
.CreateLogger();

builder.Host.UseSerilog();


builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddDbContext<UserContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserContext>();
    db.Database.EnsureCreated();

    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new User(0, "Alice Johnson", "alice@example.com", "9876543210"),
            new User(0, "Bob Smith", "bob@example.com", "9123456780"),
            new User(0, "Carol White", "carol@example.com", "9988776655"),
            new User(0, "David Brown", "david@example.com", "9001122334"),
            new User(0, "Eva Green", "eva@example.com", "9765432100")
        );
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    
app.UseSwagger();
app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
