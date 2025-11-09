using Microsoft.AspNetCore.Mvc;
using MotoTrackAPI.DTOs;
using MotoTrackAPI.Services;
using Asp.Versioning;

namespace MotoTrackAPI.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(JwtService jwtService, ILogger<AuthController> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Tentativa de login para usuário: {Username}", request.Username);

        if (!_jwtService.ValidarCredenciais(request.Username, request.Senha))
        {
            _logger.LogWarning("Login falhou para usuário: {Username}", request.Username);
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Credenciais inválidas"
            });
        }

        var role = _jwtService.ObterRole(request.Username);
        var userId = request.Username == "admin" ? 1 : 2;
        var token = _jwtService.GerarToken(request.Username, role, userId);
        var expiresAt = DateTime.Now.AddHours(8);

        _logger.LogInformation("Login bem-sucedido para usuário: {Username}", request.Username);

        return Ok(new ApiResponse<LoginResponse>
        {
            Success = true,
            Message = "Login realizado com sucesso",
            Data = new LoginResponse
            {
                Token = token,
                Username = request.Username,
                Role = role,
                ExpiresAt = expiresAt
            }
        });
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var username = User.Identity?.Name;
        var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Usuário autenticado",
            Data = new
            {
                Username = username,
                Role = role,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false
            }
        });
    }
}
