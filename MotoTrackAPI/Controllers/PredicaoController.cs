using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotoTrackAPI.Data;
using MotoTrackAPI.DTOs;
using MotoTrackAPI.Services;
using Asp.Versioning;

namespace MotoTrackAPI.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
public class PredicaoController : ControllerBase
{
    private readonly MLService _mlService;
    private readonly AppDbContext _context;
    private readonly ILogger<PredicaoController> _logger;

    public PredicaoController(MLService mlService, AppDbContext context, ILogger<PredicaoController> logger)
    {
        _mlService = mlService;
        _context = context;
        _logger = logger;
    }

    [HttpPost("prever-manutencao")]
    [ProducesResponseType(typeof(ApiResponse<PredicaoManutencaoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreverManutencao([FromBody] PredicaoManutencaoRequest request)
    {
        var moto = await _context.Motos.FindAsync(request.MotoId);
        if (moto == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Moto com ID {request.MotoId} não encontrada"
            });
        }

        _logger.LogInformation("Iniciando predição de manutenção para moto {Placa}", moto.Placa);

        try
        {
            var predicao = _mlService.PreverManutencao(request);

            predicao.Placa = moto.Placa;

            _logger.LogInformation(
                "Predição concluída para moto {Placa}: RequerManutencao={Requer}, Probabilidade={Prob:P2}",
                moto.Placa, predicao.RequerManutencao, predicao.ProbabilidadeManutencao);

            return Ok(new ApiResponse<PredicaoManutencaoResponse>
            {
                Success = true,
                Message = "Predição realizada com sucesso",
                Data = predicao
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer predição para moto {Placa}", moto.Placa);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Erro ao processar predição: " + ex.Message
            });
        }
    }

    [HttpPost("prever-manutencao-lote")]
    [Authorize(Roles = "Admin,Operador")]
    [ProducesResponseType(typeof(ApiResponse<List<PredicaoManutencaoResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreverManutencaoLote([FromBody] List<PredicaoManutencaoRequest> requests)
    {
        var predicoes = new List<PredicaoManutencaoResponse>();

        foreach (var request in requests)
        {
            var moto = await _context.Motos.FindAsync(request.MotoId);
            if (moto == null) continue;

            try
            {
                var predicao = _mlService.PreverManutencao(request);
                predicao.Placa = moto.Placa;
                predicoes.Add(predicao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar moto {MotoId}", request.MotoId);
            }
        }

        return Ok(new ApiResponse<List<PredicaoManutencaoResponse>>
        {
            Success = true,
            Message = $"{predicoes.Count} predições realizadas",
            Data = predicoes
        });
    }

    [HttpGet("analise-frota")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AnaliseFrota()
    {
        var motos = await _context.Motos.ToListAsync();

        var analise = new
        {
            TotalMotos = motos.Count,
            MotosDisponiveis = motos.Count(m => m.Status == "Disponível"),
            MotosAlugadas = motos.Count(m => m.Status == "Alugada"),
            MotosManutencao = motos.Count(m => m.Status == "Manutenção"),
            Recomendacao = "Use o endpoint /prever-manutencao para análise detalhada de cada moto",
            DataAnalise = DateTime.Now
        };

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Análise da frota concluída",
            Data = analise
        });
    }

    [HttpGet("metricas-modelo")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GetMetricasModelo()
    {
        _mlService.AvaliarModelo();

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Métricas do modelo avaliadas. Verifique os logs do servidor.",
            Data = new
            {
                ModeloTreinado = true,
                Algoritmo = "FastTree (Boosted Decision Tree)",
                Features = new[] { "Quilometragem", "NivelBateria", "DiasDesdeUltimaManutencao" },
                Target = "RequerManutencao (bool)"
            }
        });
    }

    [HttpGet("exemplo")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PredicaoManutencaoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Exemplo()
    {
        var moto = await _context.Motos.FirstOrDefaultAsync();
        if (moto == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Nenhuma moto cadastrada no sistema"
            });
        }

        var request = new PredicaoManutencaoRequest
        {
            MotoId = moto.Id,
            Quilometragem = 8500,
            NivelBateria = 75,
            DiasDesdeUltimaManutencao = 150
        };

        var predicao = _mlService.PreverManutencao(request);
        predicao.Placa = moto.Placa;

        return Ok(new ApiResponse<PredicaoManutencaoResponse>
        {
            Success = true,
            Message = "Exemplo de predição (use /prever-manutencao com seus dados reais)",
            Data = predicao
        });
    }
}
