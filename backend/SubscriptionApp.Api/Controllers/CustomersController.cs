using Microsoft.AspNetCore.Mvc;
using SubscriptionApp.Api.Dtos.Customers;
using SubscriptionApp.Api.Mapping;
using SubscriptionApp.Infrastructure.Services;

namespace SubscriptionApp.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerResponse>>> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers.Select(c => c.ToResponse()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        return Ok(customer.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CreateCustomerRequest request)
    {
        var customer = await _customerService.CreateAsync(request.ToEntity());
        var response = customer.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _customerService.DeleteAsync(id);
        return NoContent();
    }
}
