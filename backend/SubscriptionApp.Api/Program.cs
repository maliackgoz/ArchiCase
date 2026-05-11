using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger (development only) ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Database ─────────────────────────────────────────────────────────────────
// TODO (domain-modeler): register AppDbContext here, e.g.:
//   builder.Services.AddDbContext<AppDbContext>(opt =>
//       opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Validation ───────────────────────────────────────────────────────────────
// TODO (customer-feature-builder): register FluentValidation here, e.g.:
//   builder.Services.AddFluentValidationAutoValidation();
//   builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerRequestValidator>();

// ── Application services ─────────────────────────────────────────────────────
// TODO (customer-feature-builder): register ICustomerService → CustomerService (Scoped)
// TODO (subscription-feature-builder): register ISubscriptionService → SubscriptionService (Scoped)
// TODO (external-services-builder): register typed HttpClients (IDebtInquiryClient, IPaymentGatewayClient, INotificationClient)
// TODO (payment-feature-builder): register IPaymentService → PaymentService (Scoped)

// ── Exception handling middleware ─────────────────────────────────────────────
// TODO (customer-feature-builder): register ExceptionHandlingMiddleware as the FIRST middleware
//   app.UseMiddleware<ExceptionHandlingMiddleware>();
// This must go before all other app.Use* calls below.

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Seed data (development only) ─────────────────────────────────────────────
// TODO (domain-modeler): call DbInitializer here, e.g.:
//   if (app.Environment.IsDevelopment())
//   {
//       using var scope = app.Services.CreateScope();
//       await DbInitializer.SeedAsync(scope.ServiceProvider);
//   }

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
