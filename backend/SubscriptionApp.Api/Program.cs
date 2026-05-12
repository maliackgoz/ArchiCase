using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SubscriptionApp.Api.Middleware;
using SubscriptionApp.Api.Services;
using SubscriptionApp.Api.Validators.Subscriptions;
using SubscriptionApp.Infrastructure.ExternalServices;
using SubscriptionApp.Infrastructure.Persistence;
using SubscriptionApp.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            var body = new
            {
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "One or more validation errors occurred.",
                    details = errors
                }
            };
            return new BadRequestObjectResult(body);
        };
    });

// ── Swagger (development only) ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        // Return consistent error shape for 401 responses.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = 401;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(new
                {
                    error = new { code = "UNAUTHORIZED", message = "Authentication required." }
                });
            }
        };
    });

builder.Services.AddAuthorization();

// ── Validation ───────────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateSubscriptionRequestValidator>();

// ── Application services ─────────────────────────────────────────────────────
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<JwtService>();

// ── Typed HttpClients (self-loopback to mock external endpoints) ──────────────
var externalBase = builder.Configuration["ExternalServices:BaseUrl"]!;
builder.Services.AddHttpClient<IDebtInquiryClient, DebtInquiryClient>(c =>
{
    c.BaseAddress = new Uri(externalBase);
    c.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>(c =>
{
    c.BaseAddress = new Uri(externalBase);
    c.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<INotificationClient, NotificationClient>(c =>
{
    c.BaseAddress = new Uri(externalBase);
});
builder.Services.AddHttpClient<IProviderInfoClient, ProviderInfoClient>(c =>
{
    c.BaseAddress = new Uri(externalBase);
    c.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<IPaymentService, PaymentService>();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Exception handling middleware — must be first ────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

// ── Seed data (development only) ─────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DbInitializer.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
