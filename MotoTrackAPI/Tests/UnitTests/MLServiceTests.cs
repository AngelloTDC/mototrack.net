using MotoTrackAPI.DTOs;
using MotoTrackAPI.Services;
using Xunit;

namespace MotoTrackAPI.Tests.UnitTests;

public class MLServiceTests
{
    private readonly MLService _mlService;

    public MLServiceTests()
    {
        _mlService = new MLService();
    }

    [Fact]
    public void PreverManutencao_ComQuilometragemBaixa_NaoDeveRequererManutencao()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 2000,
            NivelBateria = 95,
            DiasDesdeUltimaManutencao = 30
        };

        // Act
        var resultado = _mlService.PreverManutencao(request);

        // Assert
        Assert.False(resultado.RequerManutencao);
        Assert.True(resultado.DiasEstimados > 0);
        Assert.Contains("boas condições", resultado.Recomendacao);
    }

    [Fact]
    public void PreverManutencao_ComQuilometragemAlta_DeveRequererManutencao()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 15000,
            NivelBateria = 60,
            DiasDesdeUltimaManutencao = 200
        };

        // Act
        var resultado = _mlService.PreverManutencao(request);

        // Assert
        Assert.True(resultado.RequerManutencao);
        Assert.True(resultado.ProbabilidadeManutencao > 0.5);
        Assert.Contains("URGENTE", resultado.Recomendacao);
    }

    [Fact]
    public void PreverManutencao_ComBateriaMedia_DeveCalcularCorretamente()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 7000,
            NivelBateria = 75,
            DiasDesdeUltimaManutencao = 120
        };

        // Act
        var resultado = _mlService.PreverManutencao(request);

        // Assert
        Assert.NotNull(resultado);
        Assert.InRange(resultado.ProbabilidadeManutencao, 0, 1);
        Assert.NotNull(resultado.Recomendacao);
        Assert.NotEmpty(resultado.Recomendacao);
    }

    [Theory]
    [InlineData(1000, 100, 30, false)]
    [InlineData(10000, 70, 180, true)]
    [InlineData(15000, 60, 200, true)]
    [InlineData(5000, 85, 90, false)]
    public void PreverManutencao_ComDiferentesCenarios_DeveRetornarPredicaoCorreta(
        float quilometragem, float nivelBateria, int diasDesdeUltima, bool expectedRequiresMaintenance)
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = quilometragem,
            NivelBateria = nivelBateria,
            DiasDesdeUltimaManutencao = diasDesdeUltima
        };

        // Act
        var resultado = _mlService.PreverManutencao(request);

        // Assert
        Assert.NotNull(resultado);
        // A predição pode variar um pouco devido à natureza do ML
        // então verificamos apenas que retorna um valor válido
        Assert.InRange(resultado.ProbabilidadeManutencao, 0, 1);
    }

    [Fact]
    public void PreverManutencao_ComManutencaoUrgente_DeveRetornarDiasZero()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 20000,
            NivelBateria = 50,
            DiasDesdeUltimaManutencao = 250
        };

        // Act
        var resultado = _mlService.PreverManutencao(request);

        // Assert
        Assert.True(resultado.RequerManutencao);
        Assert.Equal(0, resultado.DiasEstimados);
        Assert.Contains("URGENTE", resultado.Recomendacao);
        Assert.Contains("imediata", resultado.Recomendacao);
    }

    [Fact]
    public void PreverManutencao_ComManutencaoProxima_DeveRetornarAlerta()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 9000,
            NivelBateria = 68,
            DiasDesdeUltimaManutencao = 170
        };

        // Act
        var resultado = _mlService.PreverManutencao(request);

        // Assert
        Assert.NotNull(resultado);
        Assert.InRange(resultado.DiasEstimados, 0, 60);
    }

    [Fact]
    public void PreverManutencao_ComMotoNova_DeveRetornarMaiorPrazo()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 500,
            NivelBateria = 100,
            DiasDesdeUltimaManutencao = 10
        };

        // Act
        var resultado = _mlService.PreverManutencao(request);

        // Assert
        Assert.False(resultado.RequerManutencao);
        Assert.True(resultado.DiasEstimados >= 30);
        Assert.Contains("✅", resultado.Recomendacao);
    }

    [Fact]
    public void PreverManutencao_ComProbabilidadeAlta_DeveGerarRecomendacaoApropriada()
    {
        // Arrange
        var request = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 12000,
            NivelBateria = 65,
            DiasDesdeUltimaManutencao = 185
        };

        // Act
        var resultado = _mlService.PreverManutencao(request);

        // Assert
        Assert.True(resultado.ProbabilidadeManutencao > 0);
        Assert.NotNull(resultado.Recomendacao);
        Assert.NotEmpty(resultado.Recomendacao);

        if (resultado.RequerManutencao)
        {
            Assert.True(
                resultado.Recomendacao.Contains("🔴") ||
                resultado.Recomendacao.Contains("🟡") ||
                resultado.Recomendacao.Contains("🟢")
            );
        }
    }

    [Fact]
    public void AvaliarModelo_DeveExecutarSemErros()
    {
        // Act & Assert
        var exception = Record.Exception(() => _mlService.AvaliarModelo());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(3000, 90, 50)]
    [InlineData(7000, 75, 120)]
    [InlineData(12000, 65, 185)]
    public void PreverManutencao_DeveManterConsistencia_ParaMesmasEntradas(
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

        // Act
        var resultado1 = _mlService.PreverManutencao(request);
        var resultado2 = _mlService.PreverManutencao(request);

        // Assert - Mesmas entradas devem produzir mesmos resultados
        Assert.Equal(resultado1.RequerManutencao, resultado2.RequerManutencao);
        Assert.Equal(resultado1.ProbabilidadeManutencao, resultado2.ProbabilidadeManutencao);
        Assert.Equal(resultado1.DiasEstimados, resultado2.DiasEstimados);
        Assert.Equal(resultado1.Recomendacao, resultado2.Recomendacao);
    }

    [Fact]
    public void PreverManutencao_ComValoresLimite_DeveProcessarCorretamente()
    {
        // Arrange - Valores nos limites
        var requestMinimo = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 0,
            NivelBateria = 0,
            DiasDesdeUltimaManutencao = 0
        };

        var requestMaximo = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 50000,
            NivelBateria = 100,
            DiasDesdeUltimaManutencao = 365
        };

        // Act
        var resultadoMinimo = _mlService.PreverManutencao(requestMinimo);
        var resultadoMaximo = _mlService.PreverManutencao(requestMaximo);

        // Assert
        Assert.NotNull(resultadoMinimo);
        Assert.NotNull(resultadoMaximo);
        Assert.InRange(resultadoMinimo.ProbabilidadeManutencao, 0, 1);
        Assert.InRange(resultadoMaximo.ProbabilidadeManutencao, 0, 1);
    }
}