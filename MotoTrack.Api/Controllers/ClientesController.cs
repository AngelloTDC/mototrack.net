using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotoTrack.Api.Data;
using MotoTrack.Api.DTOs;
using MotoTrack.Api.Models;
using MotoTrack.Api.Utils;

namespace MotoTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController(MotoTrackContext db) : ControllerBase
{
    [HttpGet(Name = "GetClientes")]
    [ProducesResponseType(typeof(PagedResult<Cliente>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Cliente>>> GetAll([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var q = db.Clientes.AsNoTracking().OrderBy(c => c.Id);
        var total = await q.CountAsync();
        var items = await q.Skip((page-1)*size).Take(size).ToListAsync();
        var result = new PagedResult<Cliente>{ Items = items, Page = page, Size = size, TotalItems = total };
        Hateoas.AddPaginationLinks(result, Url, "GetClientes");
        return Ok(result);
    }

    [HttpGet("{id:int}", Name = "GetClienteById")]
    [ProducesResponseType(typeof(Cliente), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Cliente>> GetById(int id)
    {
        var ent = await db.Clientes.FindAsync(id);
        return ent is null ? NotFound() : Ok(ent);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Cliente), StatusCodes.Status201Created)]
    public async Task<ActionResult<Cliente>> Create(Cliente c)
    {
        db.Clientes.Add(c);
        await db.SaveChangesAsync();
        return CreatedAtRoute("GetClienteById", new { id = c.Id }, c);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, Cliente c)
    {
        if (id != c.Id) return BadRequest();
        var exists = await db.Clientes.AnyAsync(x => x.Id == id);
        if (!exists) return NotFound();
        db.Entry(c).State = EntityState.Modified;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ent = await db.Clientes.FindAsync(id);
        if (ent is null) return NotFound();
        db.Clientes.Remove(ent);
        await db.SaveChangesAsync();
        return NoContent();
    }
}