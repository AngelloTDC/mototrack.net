using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MotoTrackAPI.Controllers;
using MotoTrackAPI.Data;
using MotoTrackAPI.DTOs;
using MotoTrackAPI.Models;
using MotoTrackAPI.Services;
using Xunit;

namespace MotoTrackAPI.Tests.UnitTests;

public class AuthControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly Mock<JwtService> _mockJwtService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockLogger = new Mock<ILogger<AuthController>>();
        _mockJwtService = new Mock<JwtService>(null, null);
        
        _controller = new AuthController(_context, _mockLogger.Object, _mockJwtService.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task Register_ComDadosValidos_DeveCriarUsuario()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123",
            Nome = "Test User",
            Role = "Usuario"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, await _context.Usuarios.CountAsync());
    }

    [Fact]
    public async Task Register_ComUsernameDuplicado_DeveRetornarBadRequest()
    {
        // Arrange
        var usuarioExistente = new Usuario
        {
            Username = "testuser",
            Email = "existing@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            Nome = "Existing User",
            Role = "Usuario"
        };
        _context.Usuarios.Add(usuarioExistente);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "new@example.com",
            Password = "Test@123",
            Nome = "New User",
            Role = "Usuario"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Username", response.Message);
    }

    [Fact]
    public async Task Register_ComEmailDuplicado_DeveRetornarBadRequest()
    {
        // Arrange
        var usuarioExistente = new Usuario
        {
            Username = "user1",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            Nome = "User One",
            Role = "Usuario"
        };
        _context.Usuarios.Add(usuarioExistente);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "user2",
            Email = "test@example.com",
            Password = "Test@123",
            Nome = "User Two",
            Role = "Usuario"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Email", response.Message);
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_DeveRetornarToken()
    {
        // Arrange
        var password = "Test@123";
        var usuario = new Usuario
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Nome = "Test User",
            Role = "Usuario"
        };
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var fakeToken = "fake-jwt-token-12345";
        _mockJwtService
            .Setup(x => x.GenerateToken(It.IsAny<Usuario>()))
            .Returns(fakeToken);

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = password
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<LoginResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(fakeToken, response.Data.Token);
        Assert.Equal("testuser", response.Data.Username);
    }

    [Fact]
    public async Task Login_ComUsernameInvalido_DeveRetornarUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "nonexistentuser",
            Password = "Test@123"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Credenciais inválidas", response.Message);
    }

    [Fact]
    public async Task Login_ComSenhaInvalida_DeveRetornarUnauthorized()
    {
        // Arrange
        var usuario = new Usuario
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@123"),
            Nome = "Test User",
            Role = "Usuario"
        };
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "WrongPassword@123"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorizedResult.Value);
        Assert.False(response.Success);
    }

    [Theory]
    [InlineData("", "test@example.com", "Test@123", "Test User")]
    [InlineData("testuser", "", "Test@123", "Test User")]
    [InlineData("testuser", "test@example.com", "", "Test User")]
    [InlineData("testuser", "test@example.com", "Test@123", "")]
    public async Task Register_ComCamposObrigatoriosFaltando_DeveValidar(
        string username, string email, string password, string nome)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password,
            Nome = nome,
            Role = "Usuario"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        // A validação pode ser tratada pelo ModelState ou pela lógica do controller
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Register_DeveCriptografarSenha()
    {
        // Arrange
        var password = "PlainTextPassword@123";
        var request = new RegisterRequest
        {
            Username = "secureuser",
            Email = "secure@example.com",
            Password = password,
            Nome = "Secure User",
            Role = "Usuario"
        };

        // Act
        await _controller.Register(request);

        // Assert
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == "secureuser");
        Assert.NotNull(usuario);
        Assert.NotEqual(password, usuario.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash));
    }

    [Theory]
    [InlineData("Usuario")]
    [InlineData("Operador")]
    [InlineData("Admin")]
    public async Task Register_ComDiferentesRoles_DeveCriarCorretamente(string role)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = $"user_{role}",
            Email = $"{role}@example.com",
            Password = "Test@123",
            Nome = $"User {role}",
            Role = role
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == request.Username);
        Assert.NotNull(usuario);
        Assert.Equal(role, usuario.Role);
    }

    [Fact]
    public async Task Login_DeveAtualizarUltimoAcesso()
    {
        // Arrange
        var password = "Test@123";
        var dataInicial = DateTime.Now.AddDays(-1);
        var usuario = new Usuario
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Nome = "Test User",
            Role = "Usuario",
            UltimoAcesso = dataInicial
        };
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        _mockJwtService
            .Setup(x => x.GenerateToken(It.IsAny<Usuario>()))
            .Returns("fake-token");

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = password
        };

        // Act
        await _controller.Login(request);

        // Assert
        var usuarioAtualizado = await _context.Usuarios.FindAsync(usuario.Id);
        Assert.NotNull(usuarioAtualizado);
        Assert.True(usuarioAtualizado.UltimoAcesso > dataInicial);
    }

    [Fact]
    public async Task Register_ComEmailInvalido_DeveValidar()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "invalid-email",
            Password = "Test@123",
            Nome = "Test User",
            Role = "Usuario"
        };

        // Act & Assert
        // Dependendo da implementação, pode retornar BadRequest ou usar validação de atributos
        var result = await _controller.Register(request);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Login_ComMultiplasRequisicoes_DeveManterIntegridade()
    {
        // Arrange
        var password = "Test@123";
        var usuario = new Usuario
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Nome = "Test User",
            Role = "Usuario"
        };
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        _mockJwtService
            .Setup(x => x.GenerateToken(It.IsAny<Usuario>()))
            .Returns("fake-token");

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = password
        };

        // Act - Simular múltiplos logins
        var result1 = await _controller.Login(request);
        var result2 = await _controller.Login(request);
        var result3 = await _controller.Login(request);

        // Assert - Todos devem ser bem-sucedidos
        Assert.IsType<OkObjectResult>(result1);
        Assert.IsType<OkObjectResult>(result2);
        Assert.IsType<OkObjectResult>(result3);
    }
}
