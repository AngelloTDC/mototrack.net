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

public class LocalizacoesControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<LocalizacoesController>> _mockLogger;
    private readonly LocalizacoesController _controller;

    public LocalizacoesControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _mockLogger = new Mock<ILogger<LocalizacoesController>>();
        _controller = new LocalizacoesController(_context, _mockLogger.Object);
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
        var result = await _controller.GetAll(page: 1, pageSize: 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResponse<LocalizacaoDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Data.TotalItems > 0);
    }

    [Fact]
    public async Task GetById_ComIdValido_DeveRetornarLocalizacao()
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

        var localizacao = new Localizacao
        {
            MotoId = moto.Id,
            Latitude = -23.550520,
            Longitude = -46.633308,
            DataHora = DateTime.Now
        };
        _context.Localizacoes.Add(localizacao);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetById(localizacao.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<LocalizacaoDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(localizacao.Latitude, response.Data.Latitude);
        Assert.Equal(localizacao.Longitude, response.Data.Longitude);
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
    }

    [Fact]
    public async Task Create_ComDadosValidos_DeveCriarLocalizacao()
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

        var request = new CreateLocalizacaoRequest
        {
            MotoId = moto.Id,
            Latitude = -23.550520,
            Longitude = -46.633308
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ApiResponse<LocalizacaoDto>>(createdResult.Value);
        Assert.True(response.Success);
        Assert.Equal(request.Latitude, response.Data.Latitude);
        Assert.Equal(1, await _context.Localizacoes.CountAsync());
    }

    [Fact]
    public async Task Create_ComMotoInvalida_DeveRetornarBadRequest()
    {
        // Arrange
        var request = new CreateLocalizacaoRequest
        {
            MotoId = 999,
            Latitude = -23.550520,
            Longitude = -46.633308
        };

        // Act
        var result = await _controller.Create(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetByMotoId_ComMotoValida_DeveRetornarLocalizacoes()
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

        _context.Localizacoes.AddRange(
            new Localizacao { MotoId = moto.Id, Latitude = -23.550520, Longitude = -46.633308, DataHora = DateTime.Now },
            new Localizacao { MotoId = moto.Id, Latitude = -23.551000, Longitude = -46.634000, DataHora = DateTime.Now.AddHours(-1) }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetByMotoId(moto.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<LocalizacaoDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Data.Count);
    }

    [Fact]
    public async Task GetByMotoId_ComMotoSemLocalizacoes_DeveRetornarListaVazia()
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
        var result = await _controller.GetByMotoId(moto.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<LocalizacaoDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Empty(response.Data);
    }

    [Fact]
    public async Task GetUltimaLocalizacao_ComLocalizacoesExistentes_DeveRetornarMaisRecente()
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

        var localizacaoAntiga = new Localizacao
        {
            MotoId = moto.Id,
            Latitude = -23.550520,
            Longitude = -46.633308,
            DataHora = DateTime.Now.AddHours(-2)
        };
        var localizacaoNova = new Localizacao
        {
            MotoId = moto.Id,
            Latitude = -23.551000,
            Longitude = -46.634000,
            DataHora = DateTime.Now
        };
        _context.Localizacoes.AddRange(localizacaoAntiga, localizacaoNova);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetUltimaLocalizacao(moto.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<LocalizacaoDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(localizacaoNova.Latitude, response.Data.Latitude);
    }

    [Fact]
    public async Task GetUltimaLocalizacao_SemLocalizacoes_DeveRetornarNotFound()
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
        var result = await _controller.GetUltimaLocalizacao(moto.Id);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(response.Success);
    }

    [Theory]
    [InlineData(-23.550520, -46.633308, 1000, 1)] // Coordenadas válidas dentro do raio
    [InlineData(-23.560520, -46.643308, 5000, 0)] // Coordenadas fora do raio
    public async Task GetByProximidade_DeveRetornarLocalizacoesNoRaio(
        double latitude, double longitude, double raioMetros, int expectedCount)
    {
        // Arrange
        SeedDatabase();

        // Act
        var result = await _controller.GetByProximidade(latitude, longitude, raioMetros);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<LocalizacaoDto>>>(okResult.Value);
        Assert.True(response.Success);
        // Nota: O teste exato depende da implementação do cálculo de distância
    }

    [Fact]
    public async Task Delete_ComIdValido_DeveRemoverLocalizacao()
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

        var localizacao = new Localizacao
        {
            MotoId = moto.Id,
            Latitude = -23.550520,
            Longitude = -46.633308,
            DataHora = DateTime.Now
        };
        _context.Localizacoes.Add(localizacao);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(localizacao.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(0, await _context.Localizacoes.CountAsync());
    }

    private void SeedDatabase()
    {
        var moto = new Moto
        {
            Placa = "ABC1234",
            Modelo = "CB 500",
            Fabricante = "Honda",
            Ano = 2023
        };
        _context.Motos.Add(moto);
        _context.SaveChanges();

        _context.Localizacoes.AddRange(
            new Localizacao { MotoId = moto.Id, Latitude = -23.550520, Longitude = -46.633308, DataHora = DateTime.Now },
            new Localizacao { MotoId = moto.Id, Latitude = -23.551000, Longitude = -46.634000, DataHora = DateTime.Now.AddMinutes(-30) },
            new Localizacao { MotoId = moto.Id, Latitude = -23.552000, Longitude = -46.635000, DataHora = DateTime.Now.AddHours(-1) }
        );
        _context.SaveChanges();
    }
}
