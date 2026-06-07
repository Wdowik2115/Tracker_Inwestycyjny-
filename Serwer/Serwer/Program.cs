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
using QuestPDF.Infrastructure;
using Serwer.BackgroundServices;
using Serwer.Middleware;
using Polly;
using Polly.Extensions.Http;

namespace Serwer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var builder = WebApplication.CreateBuilder(args);
            
            // ── Polly Policies ────────────────────────────────────────────────
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

            // Add local configuration for secrets
            builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

            // ── CORS ──────────────────────────────────────────────────────────
            builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
                policy
                    .WithOrigins(
                        "http://localhost:4200",
                        Environment.GetEnvironmentVariable("ALLOWED_ORIGIN") ?? "https://placeholder.vercel.app")
                    .AllowAnyHeader()
                    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")));

            // ── Database ──────────────────────────────────────────────────────
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // ── Memory cache + HTTP clients ───────────────────────────────────
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient("CoinGecko", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["CoinGecko:BaseUrl"]!);
                client.Timeout = TimeSpan.FromSeconds(
                    builder.Configuration.GetValue<int>("CoinGecko:TimeoutSeconds", 10));
                client.DefaultRequestHeaders.Add("User-Agent", "InvesteTracker/1.0");
                var apiKey = builder.Configuration["CoinGecko:ApiKey"];
                if (!string.IsNullOrEmpty(apiKey))
                    client.DefaultRequestHeaders.Add("x-cg-demo-api-key", apiKey);
            });

            builder.Services.AddHttpClient("Gemini", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Gemini:BaseUrl"]!);
            })
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);

            // ── Repositories & Unit of Work ───────────────────────────────────
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IWalletRepository, WalletRepository>();
            builder.Services.AddScoped<IAssetRepository, AssetRepository>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddScoped<IPriceAlertRepository, PriceAlertRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IReportRepository, ReportRepository>();
            builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
            builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();

            // ── Application Services ──────────────────────────────────────────
            builder.Services.AddScoped<ICoinPriceService, CoinPriceService>();
            builder.Services.AddScoped<IWalletService, WalletService>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<IPortfolioService, PortfolioService>();
            builder.Services.AddScoped<IPriceAlertService, PriceAlertService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<IWatchlistService, WatchlistService>();
            builder.Services.AddScoped<IGeminiApiService, GeminiApiService>();

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

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseCors("Frontend");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
            }

            app.Run();
        }
    }
}
