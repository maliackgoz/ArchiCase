using Microsoft.EntityFrameworkCore;
using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Domain.Exceptions;
using SubscriptionApp.Infrastructure.Persistence;
using SubscriptionApp.Infrastructure.Utilities;

namespace SubscriptionApp.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db) => _db = db;

    public async Task<User?> ValidateAsync(string email, string password)
    {
        var user = await _db.Users
            .Include(u => u.Customer)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null) return null;
        return PasswordHasher.Verify(password, user.PasswordHash) ? user : null;
    }

    public async Task<User> RegisterAsync(string email, string password, string fullName, string phoneNumber)
    {
        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new DomainException("DUPLICATE_EMAIL", "An account with this email already exists.");

        await using var transaction = await _db.Database.BeginTransactionAsync();
        bool committed = false;
        try
        {
            // If an admin pre-created a Customer record with this email, reuse it.
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == email);
            if (customer is null)
            {
                customer = new Customer
                {
                    FullName = fullName,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();
            }

            var user = new User
            {
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                Role = "Customer",
                CustomerId = customer.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
            committed = true;

            user.Customer = customer;
            return user;
        }
        catch
        {
            if (!committed) await transaction.RollbackAsync();
            throw;
        }
    }
}
