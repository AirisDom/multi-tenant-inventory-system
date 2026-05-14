using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
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

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Multi-Tenant Inventory System API",
        Version = "v1",
        Description = "A multi-tenant inventory management API with tenant isolation"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Multi-Tenant Inventory API v1");
    });
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
.WithTags("Authentication")
.WithSummary("Register a new tenant and user")
.WithDescription("Creates a new tenant organization and an admin user account simultaneously")
.Produces<RegisterResponse>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status409Conflict)
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
.WithTags("Authentication")
.WithSummary("Authenticate and obtain JWT token")
.WithDescription("Validates user credentials and returns a JWT token for API authorization")
.Produces<LoginResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.AllowAnonymous();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

app.Run();

public partial class Program { }
