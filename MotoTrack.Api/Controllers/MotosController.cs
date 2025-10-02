using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotoTrack.Api.Data;
using MotoTrack.Api.DTOs;
using MotoTrack.Api.Models;
using MotoTrack.Api.Utils;

namespace MotoTrack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MotosController(MotoTrackContext db, LinkGenerator linkGen) : ControllerBase
{
    /// <summary>Lista motos com paginação.</summary>
    /// <param name="page">Página (1..N)</param>
    /// <param name="size">Tamanho da página</param>
    /// <returns>Página de motos com links HATEOAS</returns>
    [HttpGet(Name = "GetMotos")]
    [ProducesResponseType(typeof(PagedResult<Moto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Moto>>> GetAll([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var query = db.Motos.AsNoTracking().OrderBy(m => m.Id);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();

        var result = new PagedResult<Moto>{ Items = items, Page = page, Size = size, TotalItems = total };
        Hateoas.AddPaginationLinks(result, Url, "GetMotos");
        return Ok(result);
    }

    [HttpGet("{id:int}", Name = "GetMotoById")]
    [ProducesResponseType(typeof(Moto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Moto>> GetById(int id)
    {
        var moto = await db.Motos.FindAsync(id);
        return moto is null ? NotFound() : Ok(moto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Moto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Moto>> Create(Moto moto)
    {
        db.Motos.Add(moto);
        await db.SaveChangesAsync();
        return CreatedAtRoute("GetMotoById", new { id = moto.Id }, moto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, Moto input)
    {
        if (id != input.Id) return BadRequest();
        var exists = await db.Motos.AnyAsync(m => m.Id == id);
        if (!exists) return NotFound();

        db.Entry(input).State = EntityState.Modified;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var moto = await db.Motos.FindAsync(id);
        if (moto is null) return NotFound();
        db.Motos.Remove(moto);
        await db.SaveChangesAsync();
        return NoContent();
    }
}