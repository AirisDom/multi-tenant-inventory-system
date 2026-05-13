using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using multi_tenant_inventory_system.Data;
using multi_tenant_inventory_system.DTOs;
using multi_tenant_inventory_system.Filters;
using multi_tenant_inventory_system.Middleware;
using multi_tenant_inventory_system.Models;
using multi_tenant_inventory_system.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
});
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseTenantResolution();

app.MapControllers();

app.MapPost("/api/register", async (RegisterRequest request, AppDbContext db, IPasswordHasher passwordHasher) =>
{
    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (existingUser != null)
        return Results.Conflict(new { error = "Email already registered" });

    await using var transaction = await db.Database.BeginTransactionAsync();

    try
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.TenantName,
            CreatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            TenantId = tenant.Id,
            Role = "TenantAdmin"
        };

        db.Tenants.Add(tenant);
        db.Users.Add(user);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        var response = new RegisterResponse
        {
            TenantId = tenant.Id,
            UserId = user.Id,
            Email = user.Email,
            TenantName = tenant.Name
        };

        return Results.Created($"/api/tenants/{tenant.Id}", response);
    }
    catch
    {
        await transaction.RollbackAsync();
        return Results.Problem("An error occurred during registration");
    }
})
.AddEndpointFilter<ValidationFilter<RegisterRequest>>()
.WithName("Register")
.AllowAnonymous();

app.MapPost("/api/login", async (LoginRequest request, AppDbContext db, IPasswordHasher passwordHasher, ITokenService tokenService) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null)
        return Results.Unauthorized();

    if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        return Results.Unauthorized();

    var token = tokenService.GenerateToken(user);

    var response = new LoginResponse
    {
        Token = token,
        UserId = user.Id,
        Email = user.Email,
        TenantId = user.TenantId
    };

    return Results.Ok(response);
})
.AddEndpointFilter<ValidationFilter<LoginRequest>>()
.WithName("Login")
.AllowAnonymous();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public partial class Program { }
