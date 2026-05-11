using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubscriptionApp.Api.Middleware;
using SubscriptionApp.Api.Validators.Customers;
using SubscriptionApp.Api.Validators.Subscriptions;
using SubscriptionApp.Infrastructure.ExternalServices;
using SubscriptionApp.Infrastructure.Persistence;
using SubscriptionApp.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Return consistent error shape for FluentValidation failures (HTTP 400)
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

// ── Validation ───────────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerRequestValidator>();

// ── Application services ─────────────────────────────────────────────────────
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
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
// TODO (payment-feature-builder): register IPaymentService → PaymentService (Scoped)

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
app.UseAuthorization();
app.MapControllers();

app.Run();
