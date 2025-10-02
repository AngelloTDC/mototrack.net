using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotoTrack.Api.Data;
using MotoTrack.Api.DTOs;
using MotoTrack.Api.Models;
using MotoTrack.Api.Utils;

namespace MotoTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdensController(MotoTrackContext db) : ControllerBase
{
    [HttpGet(Name = "GetOrdens")]
    [ProducesResponseType(typeof(PagedResult<OrdemServico>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrdemServico>>> GetAll([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var q = db.OrdensServico.AsNoTracking().Include(o=>o.Moto).Include(o=>o.Cliente).OrderBy(o => o.Id);
        var total = await q.CountAsync();
        var items = await q.Skip((page-1)*size).Take(size).ToListAsync();
        var result = new PagedResult<OrdemServico>{ Items = items, Page = page, Size = size, TotalItems = total };
        Hateoas.AddPaginationLinks(result, Url, "GetOrdens");
        return Ok(result);
    }

    [HttpGet("{id:int}", Name = "GetOrdemById")]
    [ProducesResponseType(typeof(OrdemServico), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrdemServico>> GetById(int id)
    {
        var ent = await db.OrdensServico.Include(o=>o.Moto).Include(o=>o.Cliente).FirstOrDefaultAsync(o=>o.Id==id);
        return ent is null ? NotFound() : Ok(ent);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrdemServico), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrdemServico>> Create(OrdemServico o)
    {
        var existsMoto = await db.Motos.AnyAsync(m => m.Id == o.MotoId);
        var existsCli = await db.Clientes.AnyAsync(c => c.Id == o.ClienteId);
        if (!existsMoto || !existsCli) return ValidationProblem("MotoId/ClienteId inválidos.");

        db.OrdensServico.Add(o);
        await db.SaveChangesAsync();
        return CreatedAtRoute("GetOrdemById", new { id = o.Id }, o);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var ent = await db.OrdensServico.FindAsync(id);
        if (ent is null) return NotFound();
        ent.Status = status;
        await db.SaveChangesAsync();
        return NoContent();
    }
}