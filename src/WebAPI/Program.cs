using Microsoft.EntityFrameworkCore;
using BioLicense_Portal.Infrastructure.Data;
using BioLicense_Portal.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;
using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Infrastructure.Security;
using BioLicense_Portal.Infrastructure.Services;
using BioLicense_Portal.WebAPI.Middleware;

var currentDir = Directory.GetCurrentDirectory();
string? dotenvPath = null;
while (currentDir != null)
{
    var testPath = Path.Combine(currentDir, ".env");
    if (File.Exists(testPath))
    {
        dotenvPath = testPath;
        break;
    }
    currentDir = Directory.GetParent(currentDir)?.FullName;
}

if (dotenvPath != null)
{
    Env.Load(dotenvPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var urls = builder.Configuration["ASPNETCORE_URLS"];
if (!string.IsNullOrEmpty(urls))
{
    builder.WebHost.UseUrls(urls);
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("Warning: Connection string 'DefaultConnection' not found in configuration.");
}

builder.Services.AddDbContext<BioLicenseDbContext>(options =>
    options.UseSqlServer(connectionString));

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey) && !EF.IsDesignTime) 
{
    throw new InvalidOperationException("JWT Key is not configured");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? "temporary_key_for_migrations_only_32_chars_long_12345"))
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<CustomExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
