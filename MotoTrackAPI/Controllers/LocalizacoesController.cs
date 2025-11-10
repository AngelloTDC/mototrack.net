using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotoTrackAPI.Data;
using MotoTrackAPI.DTOs;
using MotoTrackAPI.Models;
using Asp.Versioning;

namespace MotoTrackAPI.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
public class LocalizacoesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<LocalizacoesController> _logger;

    public LocalizacoesController(AppDbContext context, ILogger<LocalizacoesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LocalizacaoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var localizacoes = await _context.Localizacoes
            .Include(l => l.Moto)
            .OrderByDescending(l => l.DataHoraRegistro)
            .Take(100) // Limitar a 100 registros mais recentes
            .Select(l => new LocalizacaoDto
            {
                Id = l.Id,
                MotoId = l.MotoId,
                MotoPlaca = l.Moto.Placa,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                SetorDeposito = l.SetorDeposito,
                DataHoraRegistro = l.DataHoraRegistro,
                TipoLeitura = l.TipoLeitura
            })
            .ToListAsync();

        return Ok(new ApiResponse<List<LocalizacaoDto>>
        {
            Success = true,
            Message = $"{localizacoes.Count} localizações recuperadas",
            Data = localizacoes
        });
    }

    [HttpGet("moto/{motoId}/atual")]
    [ProducesResponseType(typeof(ApiResponse<LocalizacaoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentLocation(int motoId)
    {
        var localizacao = await _context.Localizacoes
            .Include(l => l.Moto)
            .Where(l => l.MotoId == motoId)
            .OrderByDescending(l => l.DataHoraRegistro)
            .FirstOrDefaultAsync();

        if (localizacao == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Nenhuma localização encontrada para a moto ID {motoId}"
            });
        }

        var dto = new LocalizacaoDto
        {
            Id = localizacao.Id,
            MotoId = localizacao.MotoId,
            MotoPlaca = localizacao.Moto.Placa,
            Latitude = localizacao.Latitude,
            Longitude = localizacao.Longitude,
            SetorDeposito = localizacao.SetorDeposito,
            DataHoraRegistro = localizacao.DataHoraRegistro,
            TipoLeitura = localizacao.TipoLeitura
        };

        return Ok(new ApiResponse<LocalizacaoDto>
        {
            Success = true,
            Message = "Localização atual encontrada",
            Data = dto
        });
    }

    [HttpGet("moto/{motoId}/historico")]
    [ProducesResponseType(typeof(ApiResponse<List<LocalizacaoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(int motoId, [FromQuery] int limit = 50)
    {
        var localizacoes = await _context.Localizacoes
            .Include(l => l.Moto)
            .Where(l => l.MotoId == motoId)
            .OrderByDescending(l => l.DataHoraRegistro)
            .Take(limit)
            .Select(l => new LocalizacaoDto
            {
                Id = l.Id,
                MotoId = l.MotoId,
                MotoPlaca = l.Moto.Placa,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                SetorDeposito = l.SetorDeposito,
                DataHoraRegistro = l.DataHoraRegistro,
                TipoLeitura = l.TipoLeitura
            })
            .ToListAsync();

        return Ok(new ApiResponse<List<LocalizacaoDto>>
        {
            Success = true,
            Message = $"{localizacoes.Count} localizações no histórico",
            Data = localizacoes
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Operador")]
    [ProducesResponseType(typeof(ApiResponse<LocalizacaoDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateLocalizacaoRequest request)
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

        var localizacao = new Localizacao
        {
            MotoId = request.MotoId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SetorDeposito = request.SetorDeposito,
            BeaconId = request.BeaconId,
            TipoLeitura = request.TipoLeitura,
            DataHoraRegistro = DateTime.Now
        };

        _context.Localizacoes.Add(localizacao);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Nova localização registrada para moto {Placa}: Lat {Lat}, Long {Long}",
            moto.Placa, request.Latitude, request.Longitude);

        var dto = new LocalizacaoDto
        {
            Id = localizacao.Id,
            MotoId = localizacao.MotoId,
            MotoPlaca = moto.Placa,
            Latitude = localizacao.Latitude,
            Longitude = localizacao.Longitude,
            SetorDeposito = localizacao.SetorDeposito,
            DataHoraRegistro = localizacao.DataHoraRegistro,
            TipoLeitura = localizacao.TipoLeitura
        };

        return CreatedAtAction(nameof(GetCurrentLocation), new { motoId = localizacao.MotoId },
            new ApiResponse<LocalizacaoDto>
            {
                Success = true,
                Message = "Localização registrada com sucesso",
                Data = dto
            });
    }

    [HttpGet("setor/{setor}")]
    [ProducesResponseType(typeof(ApiResponse<List<LocalizacaoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySetor(string setor)
    {
        var localizacoes = await _context.Localizacoes
            .Include(l => l.Moto)
            .Where(l => l.SetorDeposito != null && l.SetorDeposito.Contains(setor))
            .GroupBy(l => l.MotoId)
            .Select(g => g.OrderByDescending(l => l.DataHoraRegistro).FirstOrDefault())
            .Where(l => l != null)
            .Select(l => new LocalizacaoDto
            {
                Id = l!.Id,
                MotoId = l.MotoId,
                MotoPlaca = l.Moto.Placa,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                SetorDeposito = l.SetorDeposito,
                DataHoraRegistro = l.DataHoraRegistro,
                TipoLeitura = l.TipoLeitura
            })
            .ToListAsync();

        return Ok(new ApiResponse<List<LocalizacaoDto>>
        {
            Success = true,
            Message = $"{localizacoes.Count} motos encontradas no setor '{setor}'",
            Data = localizacoes
        });
    }

    [HttpGet("proximidade")]
    [ProducesResponseType(typeof(ApiResponse<List<LocalizacaoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNearby([FromQuery] decimal latitude, [FromQuery] decimal longitude)
    {
        decimal raio = 0.001m;

        var localizacoes = await _context.Localizacoes
            .Include(l => l.Moto)
            .GroupBy(l => l.MotoId)
            .Select(g => g.OrderByDescending(l => l.DataHoraRegistro).FirstOrDefault())
            .Where(l => l != null &&
                Math.Abs(l.Latitude - latitude) <= raio &&
                Math.Abs(l.Longitude - longitude) <= raio)
            .Select(l => new LocalizacaoDto
            {
                Id = l!.Id,
                MotoId = l.MotoId,
                MotoPlaca = l.Moto.Placa,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                SetorDeposito = l.SetorDeposito,
                DataHoraRegistro = l.DataHoraRegistro,
                TipoLeitura = l.TipoLeitura
            })
            .ToListAsync();

        return Ok(new ApiResponse<List<LocalizacaoDto>>
        {
            Success = true,
            Message = $"{localizacoes.Count} motos encontradas próximas à coordenada",
            Data = localizacoes
        });
    }
}
