using Microsoft.EntityFrameworkCore;
using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Domain.Exceptions;
using SubscriptionApp.Infrastructure.Persistence;

namespace SubscriptionApp.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        return await _db.Customers
            .Include(c => c.Subscriptions)
            .ToListAsync();
    }

    public async Task<Customer> GetByIdAsync(int id)
    {
        var customer = await _db.Customers
            .Include(c => c.Subscriptions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null)
            throw new NotFoundException(nameof(Customer), id);

        return customer;
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        var emailExists = await _db.Customers
            .AnyAsync(c => c.Email == customer.Email);

        if (emailExists)
            throw new DomainException("DUPLICATE_EMAIL",
                $"A customer with email '{customer.Email}' already exists.");

        customer.CreatedAt = DateTime.UtcNow;
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id);

        if (customer is null)
            throw new NotFoundException(nameof(Customer), id);

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
    }
}
