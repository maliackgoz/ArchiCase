using Microsoft.AspNetCore.Mvc;
using SubscriptionApp.Api.Dtos.Auth;
using SubscriptionApp.Api.Services;
using SubscriptionApp.Infrastructure.Services;

namespace SubscriptionApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtService _jwtService;

    public AuthController(IAuthService authService, JwtService jwtService)
    {
        _authService = authService;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _authService.ValidateAsync(request.Email, request.Password);
        if (user is null)
            return Unauthorized(new
            {
                error = new { code = "INVALID_CREDENTIALS", message = "Email or password is incorrect." }
            });

        var token = _jwtService.GenerateToken(user);
        return Ok(new LoginResponse
        {
            Token = token,
            User = new AuthUserInfo
            {
                Id = user.Id,
                Email = user.Email,
                CustomerId = user.CustomerId,
                FullName = user.Customer?.FullName ?? (user.Role == "Admin" ? "Bank Admin" : user.Email),
                Role = user.Role
            }
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = await _authService.RegisterAsync(
            request.Email, request.Password, request.FullName, request.PhoneNumber);

        var token = _jwtService.GenerateToken(user);
        return CreatedAtAction(nameof(Login), new LoginResponse
        {
            Token = token,
            User = new AuthUserInfo
            {
                Id = user.Id,
                Email = user.Email,
                CustomerId = user.CustomerId,
                FullName = user.Customer?.FullName ?? (user.Role == "Admin" ? "Bank Admin" : user.Email),
                Role = user.Role
            }
        });
    }
}
