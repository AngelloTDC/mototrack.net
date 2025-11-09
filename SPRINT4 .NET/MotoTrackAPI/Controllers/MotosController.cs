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
public class MotosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<MotosController> _logger;

    public MotosController(AppDbContext context, ILogger<MotosController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<MotoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var totalItems = await _context.Motos.CountAsync();
        var motos = await _context.Motos
            .Include(m => m.Beacon)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MotoDto
            {
                Id = m.Id,
                Placa = m.Placa,
                Modelo = m.Modelo,
                Fabricante = m.Fabricante,
                Ano = m.Ano,
                Status = m.Status,
                BeaconId = m.BeaconId,
                BeaconIdentificador = m.Beacon != null ? m.Beacon.Identificador : null,
                DataCadastro = m.DataCadastro
            })
            .ToListAsync();

        var response = new PagedResponse<MotoDto>
        {
            Items = motos,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };

        return Ok(new ApiResponse<PagedResponse<MotoDto>>
        {
            Success = true,
            Message = "Motos recuperadas com sucesso",
            Data = response
        });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<MotoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var moto = await _context.Motos
            .Include(m => m.Beacon)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (moto == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Moto com ID {id} não encontrada"
            });
        }

        var dto = new MotoDto
        {
            Id = moto.Id,
            Placa = moto.Placa,
            Modelo = moto.Modelo,
            Fabricante = moto.Fabricante,
            Ano = moto.Ano,
            Status = moto.Status,
            BeaconId = moto.BeaconId,
            BeaconIdentificador = moto.Beacon?.Identificador,
            DataCadastro = moto.DataCadastro
        };

        return Ok(new ApiResponse<MotoDto>
        {
            Success = true,
            Message = "Moto encontrada",
            Data = dto
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Operador")]
    [ProducesResponseType(typeof(ApiResponse<MotoDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMotoRequest request)
    {
        if (await _context.Motos.AnyAsync(m => m.Placa == request.Placa))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Já existe uma moto com esta placa"
            });
        }

        var moto = new Moto
        {
            Placa = request.Placa,
            Modelo = request.Modelo,
            Fabricante = request.Fabricante,
            Ano = request.Ano,
            BeaconId = request.BeaconId,
            Status = "Disponível",
            DataCadastro = DateTime.Now
        };

        _context.Motos.Add(moto);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Nova moto criada: {Placa}", moto.Placa);

        var dto = new MotoDto
        {
            Id = moto.Id,
            Placa = moto.Placa,
            Modelo = moto.Modelo,
            Fabricante = moto.Fabricante,
            Ano = moto.Ano,
            Status = moto.Status,
            BeaconId = moto.BeaconId,
            DataCadastro = moto.DataCadastro
        };

        return CreatedAtAction(nameof(GetById), new { id = moto.Id }, new ApiResponse<MotoDto>
        {
            Success = true,
            Message = "Moto criada com sucesso",
            Data = dto
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Operador")]
    [ProducesResponseType(typeof(ApiResponse<MotoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMotoRequest request)
    {
        var moto = await _context.Motos.FindAsync(id);
        if (moto == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Moto com ID {id} não encontrada"
            });
        }

        if (request.Modelo != null) moto.Modelo = request.Modelo;
        if (request.Fabricante != null) moto.Fabricante = request.Fabricante;
        if (request.Ano.HasValue) moto.Ano = request.Ano.Value;
        if (request.Status != null) moto.Status = request.Status;
        if (request.BeaconId.HasValue) moto.BeaconId = request.BeaconId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Moto atualizada: {Placa}", moto.Placa);

        var dto = new MotoDto
        {
            Id = moto.Id,
            Placa = moto.Placa,
            Modelo = moto.Modelo,
            Fabricante = moto.Fabricante,
            Ano = moto.Ano,
            Status = moto.Status,
            BeaconId = moto.BeaconId,
            DataCadastro = moto.DataCadastro
        };

        return Ok(new ApiResponse<MotoDto>
        {
            Success = true,
            Message = "Moto atualizada com sucesso",
            Data = dto
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var moto = await _context.Motos.FindAsync(id);
        if (moto == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"Moto com ID {id} não encontrada"
            });
        }

        _context.Motos.Remove(moto);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Moto removida: {Placa}", moto.Placa);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Moto removida com sucesso"
        });
    }

    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<MotoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(string status)
    {
        var motos = await _context.Motos
            .Include(m => m.Beacon)
            .Where(m => m.Status == status)
            .Select(m => new MotoDto
            {
                Id = m.Id,
                Placa = m.Placa,
                Modelo = m.Modelo,
                Fabricante = m.Fabricante,
                Ano = m.Ano,
                Status = m.Status,
                BeaconId = m.BeaconId,
                BeaconIdentificador = m.Beacon != null ? m.Beacon.Identificador : null,
                DataCadastro = m.DataCadastro
            })
            .ToListAsync();

        return Ok(new ApiResponse<List<MotoDto>>
        {
            Success = true,
            Message = $"{motos.Count} motos encontradas com status '{status}'",
            Data = motos
        });
    }
}
