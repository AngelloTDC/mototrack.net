using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MotoTrackAPI.Controllers;
using MotoTrackAPI.Data;
using MotoTrackAPI.DTOs;
using MotoTrackAPI.Models;
using Xunit;

namespace MotoTrackAPI.Tests.UnitTests;

public class MotosControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<MotosController>> _mockLogger;
    private readonly MotosController _controller;

    public MotosControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockLogger = new Mock<ILogger<MotosController>>();
        _controller = new MotosController(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetAll_DeveRetornarListaPaginada()
    {
        // Arrange
        SeedDatabase();

        // Act
        var result = await _controller.GetAll(page: 1, pageSize: 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResponse<MotoDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(3, response.Data.TotalItems);
        Assert.Equal(1, response.Data.Page);
    }

    [Fact]
    public async Task GetById_ComIdValido_DeveRetornarMoto()
    {
        // Arrange
        var moto = new Moto
        {
            Placa = "ABC1234",
            Modelo = "CB 500",
            Fabricante = "Honda",
            Ano = 2023,
            Status = "Disponível"
        };
        _context.Motos.Add(moto);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(moto.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<MotoDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("ABC1234", response.Data.Placa);
    }

    [Fact]
    public async Task GetById_ComIdInvalido_DeveRetornarNotFound()
    {
        // Act
        var result = await _controller.GetById(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("não encontrada", response.Message);
    }

    [Fact]
    public async Task Create_ComDadosValidos_DeveCriarMoto()
    {
        // Arrange
        var request = new CreateMotoRequest
        {
            Placa = "XYZ9876",
            Modelo = "MT-07",
            Fabricante = "Yamaha",
            Ano = 2024
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ApiResponse<MotoDto>>(createdResult.Value);
        Assert.True(response.Success);
        Assert.Equal("XYZ9876", response.Data.Placa);
        Assert.Equal(1, await _context.Motos.CountAsync());
    }

    [Fact]
    public async Task Create_ComPlacaDuplicada_DeveRetornarBadRequest()
    {
        // Arrange
        var motoExistente = new Moto
        {
            Placa = "ABC1234",
            Modelo = "CB 500",
            Fabricante = "Honda",
            Ano = 2023
        };
        _context.Motos.Add(motoExistente);
        await _context.SaveChangesAsync();

        var request = new CreateMotoRequest
        {
            Placa = "ABC1234",
            Modelo = "MT-07",
            Fabricante = "Yamaha",
            Ano = 2024
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Já existe", response.Message);
    }

    [Fact]
    public async Task Update_ComDadosValidos_DeveAtualizarMoto()
    {
        // Arrange
        var moto = new Moto
        {
            Placa = "ABC1234",
            Modelo = "CB 500",
            Fabricante = "Honda",
            Ano = 2023,
            Status = "Disponível"
        };
        _context.Motos.Add(moto);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateMotoRequest
        {
            Modelo = "CB 500X",
            Status = "Em Manutenção"
        };

        // Act
        var result = await _controller.Update(moto.Id, updateRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<MotoDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("CB 500X", response.Data.Modelo);
        Assert.Equal("Em Manutenção", response.Data.Status);
    }

    [Fact]
    public async Task Update_ComIdInvalido_DeveRetornarNotFound()
    {
        // Arrange
        var updateRequest = new UpdateMotoRequest
        {
            Modelo = "CB 500X"
        };

        // Act
        var result = await _controller.Update(999, updateRequest);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Delete_ComIdValido_DeveRemoverMoto()
    {
        // Arrange
        var moto = new Moto
        {
            Placa = "ABC1234",
            Modelo = "CB 500",
            Fabricante = "Honda",
            Ano = 2023
        };
        _context.Motos.Add(moto);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(moto.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(0, await _context.Motos.CountAsync());
    }

    [Fact]
    public async Task Delete_ComIdInvalido_DeveRetornarNotFound()
    {
        // Act
        var result = await _controller.Delete(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetByStatus_DeveRetornarMotosComStatusEspecifico()
    {
        // Arrange
        _context.Motos.AddRange(
            new Moto { Placa = "ABC1234", Modelo = "CB 500", Fabricante = "Honda", Ano = 2023, Status = "Disponível" },
            new Moto { Placa = "DEF5678", Modelo = "MT-07", Fabricante = "Yamaha", Ano = 2024, Status = "Em Manutenção" },
            new Moto { Placa = "GHI9012", Modelo = "Ninja 400", Fabricante = "Kawasaki", Ano = 2023, Status = "Disponível" }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetByStatus("Disponível");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<MotoDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Data.Count);
        Assert.All(response.Data, m => Assert.Equal("Disponível", m.Status));
    }

    [Fact]
    public async Task GetAll_ComPaginacaoInvalida_DeveUsarValoresPadrao()
    {
        // Arrange
        SeedDatabase();

        // Act
        var result = await _controller.GetAll(page: 0, pageSize: -1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResponse<MotoDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotEmpty(response.Data.Items);
    }

    private void SeedDatabase()
    {
        _context.Motos.AddRange(
            new Moto { Placa = "ABC1234", Modelo = "CB 500", Fabricante = "Honda", Ano = 2023, Status = "Disponível" },
            new Moto { Placa = "DEF5678", Modelo = "MT-07", Fabricante = "Yamaha", Ano = 2024, Status = "Em Uso" },
            new Moto { Placa = "GHI9012", Modelo = "Ninja 400", Fabricante = "Kawasaki", Ano = 2023, Status = "Disponível" }
        );
        _context.SaveChanges();
    }
}
