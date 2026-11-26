using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RutaBol.Api.Data;
using RutaBol.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace RutaBol.Api.Controllers;

[ApiController]
[Route("api/lineas")]
public class LineasController : ControllerBase
{
    private readonly RutaBolDbContext _db;
    public LineasController(RutaBolDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Consultar()
    {
        var items = await _db.Lineas.AsNoTracking()
            .Where(l => l.Activa)
            .OrderBy(l => l.Codigo)
            .Select(l => new
            {
                l.Id, l.Codigo, l.Activa,
                UnidadesActivas = _db.Micros.Count(m => m.LineaId == l.Id && m.Activo)
            }).ToListAsync();
        return Ok(items);
    }

    [HttpGet("todas")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Todas()
    {
        var items = await _db.Lineas.AsNoTracking().OrderBy(l => l.Codigo)
            .Select(l => new
            {
                l.Id, l.Codigo, l.Activa, l.FechaRegistro,
                UnidadesActivas = _db.Micros.Count(m => m.LineaId == l.Id && m.Activo),
                TotalUnidades = _db.Micros.Count(m => m.LineaId == l.Id)
            }).ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Crear([FromBody] NuevaLineaDto datos)
    {
        var codigo = (datos.Codigo ?? "").Trim().ToUpperInvariant();
        if (codigo.Length is < 1 or > 20)
            return BadRequest(new { mensaje = "El código de línea debe tener entre 1 y 20 caracteres." });
        if (await _db.Lineas.AnyAsync(l => l.Codigo == codigo))
            return Conflict(new { mensaje = "La línea ya existe. Puedes reactivarla si estaba inactiva." });

        var linea = new LineaTransporte { Codigo = codigo };
        _db.Lineas.Add(linea);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                             pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new { mensaje = "La línea ya existe." });
        }
        return Ok(new { mensaje = "Línea registrada correctamente.", linea });
    }

    [HttpPut("{id:int}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] EstadoLineaDto datos)
    {
        var linea = await _db.Lineas.FirstOrDefaultAsync(l => l.Id == id);
        if (linea is null) return NotFound(new { mensaje = "Línea no encontrada." });
        if (!datos.Activa && await _db.Micros.AnyAsync(m => m.LineaId == id && m.Activo))
            return Conflict(new { mensaje = "Primero retira los internos activos de esta línea." });

        linea.Activa = datos.Activa;
        await _db.SaveChangesAsync();
        return Ok(new { mensaje = datos.Activa ? "Línea reactivada." : "Línea desactivada sin borrar su historial.", linea });
    }

    // Vincula los micros antiguos con las nuevas líneas sin perder registros.
    [HttpPost("sincronizar")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Sincronizar()
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        var existentes = await _db.Micros.Select(m => m.Linea).Distinct().ToListAsync();
        var nuevas = 0;
        foreach (var nombre in existentes)
        {
            var codigo = nombre.Trim().ToUpperInvariant();
            if (codigo.Length is < 1 or > 20) continue;
            if (!await _db.Lineas.AnyAsync(l => l.Codigo == codigo))
            {
                _db.Lineas.Add(new LineaTransporte { Codigo = codigo });
                await _db.SaveChangesAsync();
                nuevas++;
            }
        }

        var lineas = await _db.Lineas.ToListAsync();
        var micros = await _db.Micros.Where(m => m.LineaId == null).ToListAsync();
        var vinculados = 0;
        foreach (var micro in micros)
        {
            var linea = lineas.FirstOrDefault(l => l.Codigo == micro.Linea.Trim().ToUpperInvariant());
            if (linea is null) continue;
            micro.LineaId = linea.Id;
            vinculados++;
        }
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { mensaje = $"Sincronización finalizada: {nuevas} líneas nuevas y {vinculados} micros vinculados.", nuevas, vinculados });
    }
}

public class NuevaLineaDto
{
    [Required] public string Codigo { get; set; } = "";
}
public class EstadoLineaDto
{
    public bool Activa { get; set; }
}
