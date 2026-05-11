using System.Text;
using Investe.Application.Interfaces.Services;
using Investe.Application.Services;
using Investe.Infrastructure.Persistence;
using Investe.Infrastructure.Persistence.Repositories;
using Investe.Infrastructure.Persistence.Repositories.Implementations;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serwer.BackgroundServices;
using Serwer.Middleware;

namespace Serwer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── CORS ──────────────────────────────────────────────────────────
            builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
                policy
                    .WithOrigins(
                        "http://localhost:4200",
                        Environment.GetEnvironmentVariable("ALLOWED_ORIGIN") ?? "https://placeholder.vercel.app")
                    .AllowAnyHeader()
                    .AllowAnyMethod()));

            // ── Database ──────────────────────────────────────────────────────
            var isDevelopment = builder.Environment.IsDevelopment();
            if (isDevelopment)
            {
                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TrackerDev"));
            }
            else
            {
                var connectionString =
                    builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

                builder.Services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }

            // ── Memory cache + HTTP clients ───────────────────────────────────
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient("CoinGecko", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["CoinGecko:BaseUrl"]!);
                client.Timeout = TimeSpan.FromSeconds(
                    builder.Configuration.GetValue<int>("CoinGecko:TimeoutSeconds", 10));
                var apiKey = builder.Configuration["CoinGecko:ApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                    client.DefaultRequestHeaders.Add("x-cg-demo-api-key", apiKey);
            });

            // ── Repositories & Unit of Work ───────────────────────────────────
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IWalletRepository, WalletRepository>();
            builder.Services.AddScoped<IAssetRepository, AssetRepository>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddScoped<IPriceAlertRepository, PriceAlertRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            // ── Application Services ──────────────────────────────────────────
            builder.Services.AddScoped<ICoinPriceService, CoinPriceService>();
            builder.Services.AddScoped<IWalletService, WalletService>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<IPortfolioService, PortfolioService>();
            builder.Services.AddScoped<IPriceAlertService, PriceAlertService>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            // ── Background Services ───────────────────────────────────────────
            builder.Services.AddHostedService<PriceAlertBackgroundService>();

            // ── JWT Authentication ────────────────────────────────────────────
            var jwtSection = builder.Configuration.GetSection("Jwt");
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSection["Issuer"],
                        ValidateAudience = true,
                        ValidAudience = jwtSection["Audience"],
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                Environment.GetEnvironmentVariable("Jwt__Key") ?? jwtSection["Key"]!))
                    };
                });

            builder.Services.AddAuthorization();

            // ── Controllers + Swagger ─────────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "CryptoTracker API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var app = builder.Build();

            // ── Middleware pipeline ───────────────────────────────────────────
            app.UseMiddleware<ErrorHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("Frontend");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (app.Environment.IsDevelopment())
                {
                    db.Database.EnsureCreated();
                }
                else
                {
                    db.Database.Migrate();
                }
            }

            app.Run();
        }
    }
}
