using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RutaBol.Api.Data;
using RutaBol.Api.Models;
using System.Security.Claims;

namespace RutaBol.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pasajes")]
public class PasajesController : ControllerBase
{
    private readonly RutaBolDbContext _db;
    public PasajesController(RutaBolDbContext db) => _db = db;
    private int? IdUsuario() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    // Esto simula el cálculo del pasaje, sin cobrar dinero real
    [HttpPost("simular")]
    public async Task<IActionResult> Simular([FromBody] SimularPasajeDto datos)
    {
        var usuarioId = IdUsuario();
        if (usuarioId == null) return Unauthorized(new { mensaje = "Falta el ID de usuario en la sesión." });
        if (!await _db.Micros.AnyAsync(m => m.Id == datos.MicroId))
            return NotFound(new { mensaje = "Micro no encontrado." });

        var categoria = "General";
        var aprobada = await _db.SolicitudesCategoria.AsNoTracking()
            .Where(s => s.UsuarioId == usuarioId && s.Estado == "Aprobada")
            .OrderByDescending(s => s.FechaRevision)
            .FirstOrDefaultAsync();
        if (aprobada != null) categoria = aprobada.Categoria;

        var candidatas = await _db.Tarifas.AsNoTracking()
            .Where(t => t.Vigente && t.Nombre == categoria)
            .ToListAsync();
        if (candidatas.Count != 1)
            return Conflict(new { mensaje = "Debe existir exactamente una tarifa vigente para " + categoria + "." });

        var tarifa = candidatas[0];
        var pasaje = new Pasaje
        {
            UsuarioId = usuarioId.Value,
            MicroId = datos.MicroId,
            TarifaId = tarifa.Id,
            CategoriaAplicada = categoria,
            Monto = tarifa.Monto,
            Estado = "Simulado"
        };
        _db.Pasajes.Add(pasaje);
        await _db.SaveChangesAsync();
        return Ok(new {
            mensaje = "Simulación registrada. NO es un pago confirmado.",
            pasaje.Id, pasaje.MicroId, pasaje.CategoriaAplicada,
            pasaje.Monto, pasaje.CodigoComprobante, pasaje.Estado, pasaje.FechaRegistro
        });
    }

    [HttpGet("mios")]
    public async Task<IActionResult> MisPasajes()
    {
        var usuarioId = IdUsuario();
        if (usuarioId == null) return Unauthorized();
        return Ok(await _db.Pasajes.AsNoTracking().Where(p => p.UsuarioId == usuarioId)
            .OrderByDescending(p => p.FechaRegistro).Take(50).ToListAsync());
    }

    [HttpGet("todos")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Todos() => Ok(await _db.Pasajes.AsNoTracking()
        .OrderByDescending(p => p.FechaRegistro).Take(100).ToListAsync());
}

public class SimularPasajeDto
{
    public int MicroId { get; set; }
}
