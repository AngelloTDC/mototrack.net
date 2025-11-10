using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MotoTrackAPI.Controllers;
using MotoTrackAPI.DTOs;
using MotoTrackAPI.Services;
using Xunit;

namespace MotoTrackAPI.Tests.UnitTests;

public class PredicaoControllerTests
{
    private readonly Mock<ILogger<PredicaoController>> _mockLogger;
    private readonly Mock<MLService> _mockMlService;
    private readonly PredicaoController _controller;

    public PredicaoControllerTests()
    {
        _mockLogger = new Mock<ILogger<PredicaoController>>();
        _mockMlService = new Mock<MLService>();
        _controller = new PredicaoController(_mockLogger.Object, _mockMlService.Object);
    }

    [Fact]
    public async Task PreverManutencao_ComDadosValidos_DeveRetornarPredicao()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 5000,
            NivelBateria = 85,
            DiasDesdeUltimaManutencao = 90
        };

        var expectedResponse = new PredicaoManutencaoResponse
        {
            MotoId = 1,
            Placa = "ABC1234",
            RequerManutencao = false,
            ProbabilidadeManutencao = 0.35f,
            DiasEstimados = 30,
            Recomendacao = "✅ Moto em boas condições"
        };

        _mockMlService
            .Setup(x => x.PreverManutencao(It.IsAny<PredicaoManutencaoRequest>()))
            .Returns(expectedResponse);

        // Act
        var result = _controller.PreverManutencao(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PredicaoManutencaoResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedResponse.MotoId, response.Data.MotoId);
        Assert.False(response.Data.RequerManutencao);
    }

    [Fact]
    public async Task PreverManutencao_ComQuilometragemAlta_DeveIndicarManutencao()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 2,
            Quilometragem = 15000,
            NivelBateria = 60,
            DiasDesdeUltimaManutencao = 200
        };

        var expectedResponse = new PredicaoManutencaoResponse
        {
            MotoId = 2,
            Placa = "XYZ9876",
            RequerManutencao = true,
            ProbabilidadeManutencao = 0.92f,
            DiasEstimados = 0,
            Recomendacao = "🔴 URGENTE: Manutenção imediata necessária!"
        };

        _mockMlService
            .Setup(x => x.PreverManutencao(It.IsAny<PredicaoManutencaoRequest>()))
            .Returns(expectedResponse);

        // Act
        var result = _controller.PreverManutencao(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PredicaoManutencaoResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Data.RequerManutencao);
        Assert.True(response.Data.ProbabilidadeManutencao > 0.5);
        Assert.Equal(0, response.Data.DiasEstimados);
    }

    [Fact]
    public async Task PreverManutencao_ComBateriaBaixa_DeveAlertarManutencao()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 3,
            Quilometragem = 7000,
            NivelBateria = 50,
            DiasDesdeUltimaManutencao = 120
        };

        var expectedResponse = new PredicaoManutencaoResponse
        {
            MotoId = 3,
            Placa = "DEF5678",
            RequerManutencao = true,
            ProbabilidadeManutencao = 0.75f,
            DiasEstimados = 7,
            Recomendacao = "🟡 ATENÇÃO: Agendar manutenção em até 7 dias."
        };

        _mockMlService
            .Setup(x => x.PreverManutencao(It.IsAny<PredicaoManutencaoRequest>()))
            .Returns(expectedResponse);

        // Act
        var result = _controller.PreverManutencao(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PredicaoManutencaoResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Data.RequerManutencao);
        Assert.InRange(response.Data.DiasEstimados, 1, 7);
    }

    [Theory]
    [InlineData(1000, 100, 30)]
    [InlineData(5000, 85, 90)]
    [InlineData(10000, 70, 180)]
    [InlineData(15000, 60, 200)]
    public async Task PreverManutencao_ComDiferentesParametros_DeveRetornarPredicao(
        float quilometragem, float nivelBateria, int diasDesdeUltima)
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = quilometragem,
            NivelBateria = nivelBateria,
            DiasDesdeUltimaManutencao = diasDesdeUltima
        };

        var expectedResponse = new PredicaoManutencaoResponse
        {
            MotoId = 1,
            Placa = "TEST123",
            RequerManutencao = quilometragem > 10000,
            ProbabilidadeManutencao = 0.5f,
            DiasEstimados = 30,
            Recomendacao = "Recomendação teste"
        };

        _mockMlService
            .Setup(x => x.PreverManutencao(It.IsAny<PredicaoManutencaoRequest>()))
            .Returns(expectedResponse);

        // Act
        var result = _controller.PreverManutencao(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PredicaoManutencaoResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.InRange(response.Data.ProbabilidadeManutencao, 0, 1);
    }

    [Fact]
    public async Task PreverManutencao_ComErroNoServico_DeveLancarExcecao()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 5000,
            NivelBateria = 85,
            DiasDesdeUltimaManutencao = 90
        };

        _mockMlService
            .Setup(x => x.PreverManutencao(It.IsAny<PredicaoManutencaoRequest>()))
            .Throws(new InvalidOperationException("Modelo não foi treinado"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _controller.PreverManutencao(request));
    }

    [Fact]
    public async Task PreverManutencao_DeveLogarRequisicao()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 5000,
            NivelBateria = 85,
            DiasDesdeUltimaManutencao = 90
        };

        var expectedResponse = new PredicaoManutencaoResponse
        {
            MotoId = 1,
            Placa = "ABC1234",
            RequerManutencao = false,
            ProbabilidadeManutencao = 0.35f,
            DiasEstimados = 30,
            Recomendacao = "✅ Moto em boas condições"
        };

        _mockMlService
            .Setup(x => x.PreverManutencao(It.IsAny<PredicaoManutencaoRequest>()))
            .Returns(expectedResponse);

        // Act
        _controller.PreverManutencao(request);

        // Assert
        _mockMlService.Verify(
            x => x.PreverManutencao(It.Is<PredicaoManutencaoRequest>(r => r.MotoId == 1)),
            Times.Once);
    }

    [Fact]
    public async Task PreverManutencao_ComMotoNova_NaoDeveRequererManutencao()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 5,
            Quilometragem = 500,
            NivelBateria = 100,
            DiasDesdeUltimaManutencao = 10
        };

        var expectedResponse = new PredicaoManutencaoResponse
        {
            MotoId = 5,
            Placa = "NEW0001",
            RequerManutencao = false,
            ProbabilidadeManutencao = 0.05f,
            DiasEstimados = 60,
            Recomendacao = "✅ Moto em boas condições. Próxima revisão em aproximadamente 60 dias."
        };

        _mockMlService
            .Setup(x => x.PreverManutencao(It.IsAny<PredicaoManutencaoRequest>()))
            .Returns(expectedResponse);

        // Act
        var result = _controller.PreverManutencao(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PredicaoManutencaoResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.False(response.Data.RequerManutencao);
        Assert.True(response.Data.DiasEstimados >= 30);
        Assert.True(response.Data.ProbabilidadeManutencao < 0.3);
    }

    [Fact]
    public async Task PreverManutencao_ComProbabilidadeLimite_DeveRetornarRecomendacaoApropriada()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 6,
            Quilometragem = 9000,
            NivelBateria = 72,
            DiasDesdeUltimaManutencao = 150
        };

        var expectedResponse = new PredicaoManutencaoResponse
        {
            MotoId = 6,
            Placa = "MID1234",
            RequerManutencao = true,
            ProbabilidadeManutencao = 0.51f,
            DiasEstimados = 30,
            Recomendacao = "🟢 Manutenção recomendada em 30 dias."
        };

        _mockMlService
            .Setup(x => x.PreverManutencao(It.IsAny<PredicaoManutencaoRequest>()))
            .Returns(expectedResponse);

        // Act
        var result = _controller.PreverManutencao(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PredicaoManutencaoResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data.Recomendacao);
        Assert.Contains("🟢", response.Data.Recomendacao);
    }

    [Fact]
    public async Task PreverManutencao_DeveRetornarProbabilidadeEntre0e1()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 7,
            Quilometragem = 12000,
            NivelBateria = 65,
            DiasDesdeUltimaManutencao = 185
        };

        var expectedResponse = new PredicaoManutencaoResponse
        {
            MotoId = 7,
            Placa = "TST7890",
            RequerManutencao = true,
            ProbabilidadeManutencao = 0.87f,
            DiasEstimados = 7,
            Recomendacao = "🟡 ATENÇÃO: Agendar manutenção em até 7 dias."
        };

        _mockMlService
            .Setup(x => x.PreverManutencao(It.IsAny<PredicaoManutencaoRequest>()))
            .Returns(expectedResponse);

        // Act
        var result = _controller.PreverManutencao(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PredicaoManutencaoResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.InRange(response.Data.ProbabilidadeManutencao, 0, 1);
    }

    [Fact]
    public async Task PreverManutencao_ComDadosExtremos_DeveProcessarCorretamente()
    {
        // Arrange - Valores extremos
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 8,
            Quilometragem = 50000,
            NivelBateria = 0,
            DiasDesdeUltimaManutencao = 365
        };

        var expectedResponse = new PredicaoManutencaoResponse
        {
            MotoId = 8,
            Placa = "EXT0001",
            RequerManutencao = true,
            ProbabilidadeManutencao = 0.99f,
            DiasEstimados = 0,
            Recomendacao = "🔴 URGENTE: Manutenção imediata necessária!"
        };

        _mockMlService
            .Setup(x => x.PreverManutencao(It.IsAny<PredicaoManutencaoRequest>()))
            .Returns(expectedResponse);

        // Act
        var result = _controller.PreverManutencao(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PredicaoManutencaoResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Data.RequerManutencao);
        Assert.Equal(0, response.Data.DiasEstimados);
    }
}
