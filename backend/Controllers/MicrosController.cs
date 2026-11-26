using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RutaBol.Api.Data;
using RutaBol.Api.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RutaBol.Api.Controllers;

[ApiController]
[Route("api/micros")]
public class MicrosController : ControllerBase
{
    private readonly RutaBolDbContext _db;
    public MicrosController(RutaBolDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> ObtenerMicros() => Ok(await _db.Micros.AsNoTracking()
        .Where(m => m.Activo).OrderBy(m => m.Linea).ThenBy(m => m.NumeroInterno).ToListAsync());

    [HttpGet("todos")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ObtenerTodos() => Ok(await _db.Micros.AsNoTracking()
        .OrderBy(m => m.Linea).ThenBy(m => m.NumeroInterno).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var micro = await _db.Micros.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id && m.Activo);
        return micro is null ? NotFound(new { mensaje = "Micro no disponible." }) : Ok(micro);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Registrar([FromBody] MicroDto datos)
    {
        var lineaCodigo = (datos.Linea ?? "").Trim().ToUpperInvariant();
        var numero = (datos.NumeroInterno ?? "").Trim();
        if (lineaCodigo.Length is < 1 or > 20 || numero.Length is < 1 or > 20)
            return BadRequest(new { mensaje = "Línea e interno: entre 1 y 20 caracteres." });
        if (datos.CapacidadMaxima is < 18 or > 25)
            return BadRequest(new { mensaje = "La capacidad debe estar entre 18 y 25 pasajeros." });

        var linea = await _db.Lineas.FirstOrDefaultAsync(l => l.Codigo == lineaCodigo);
        if (linea is null || !linea.Activa)
            return BadRequest(new { mensaje = "Primero registra o reactiva esta línea en Administración de líneas." });
        if (await _db.Micros.AnyAsync(m => m.Linea == lineaCodigo && m.NumeroInterno == numero))
            return Conflict(new { mensaje = "Ese interno ya existe en la línea. Reactívalo si está retirado." });

        var micro = new Micro
        {
            Linea = lineaCodigo, LineaId = linea.Id, NumeroInterno = numero,
            CapacidadMaxima = datos.CapacidadMaxima, PasajerosActuales = 0,
            Estado = "Disponible", Activo = true
        };
        _db.Micros.Add(micro);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                             pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new { mensaje = "Ese interno ya está registrado en la línea." });
        }
        return CreatedAtAction(nameof(ObtenerPorId), new { id = micro.Id }, micro);
    }

    [HttpPut("{id:int}/editar")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarMicroDto datos)
    {
        var micro = await _db.Micros.FirstOrDefaultAsync(m => m.Id == id);
        if (micro is null) return NotFound(new { mensaje = "Interno no encontrado." });
        var numero = (datos.NumeroInterno ?? "").Trim();
        if (numero.Length is < 1 or > 20)
            return BadRequest(new { mensaje = "El número interno debe tener entre 1 y 20 caracteres." });
        if (datos.CapacidadMaxima is < 18 or > 25 || datos.CapacidadMaxima < micro.PasajerosActuales)
            return BadRequest(new { mensaje = "Capacidad inválida o menor que la ocupación actual." });
        if (await _db.Micros.AnyAsync(m => m.Id != id && m.Linea == micro.Linea && m.NumeroInterno == numero))
            return Conflict(new { mensaje = "El número interno ya existe en esa línea." });

        micro.NumeroInterno = numero;
        micro.CapacidadMaxima = datos.CapacidadMaxima;
        if (micro.Activo)
            micro.Estado = micro.PasajerosActuales == micro.CapacidadMaxima ? "Lleno"
                : micro.PasajerosActuales == 0 ? "Disponible" : "En servicio";
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                             pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new { mensaje = "Ya existe un interno con ese número." });
        }
        return Ok(new { mensaje = "Datos del interno actualizados.", micro });
    }

    [HttpPut("{id:int}/estado")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] EstadoMicroDto datos)
    {
        var micro = await _db.Micros.FirstOrDefaultAsync(m => m.Id == id);
        if (micro is null) return NotFound(new { mensaje = "Interno no encontrado." });
        if (!datos.Activo && micro.PasajerosActuales > 0)
            return Conflict(new { mensaje = "Primero deben descender los pasajeros antes de retirar el micro." });
        if (datos.Activo)
        {
            var linea = await _db.Lineas.FirstOrDefaultAsync(l => l.Id == micro.LineaId);
            if (linea is null || !linea.Activa)
                return Conflict(new { mensaje = "La línea del interno debe estar activa." });
        }
        micro.Activo = datos.Activo;
        micro.FechaRetiro = datos.Activo ? null : DateTime.UtcNow;
        micro.Estado = datos.Activo ? "Disponible" : "Fuera de servicio";
        await _db.SaveChangesAsync();
        return Ok(new { mensaje = datos.Activo ? "Interno reactivado." : "Interno retirado; se conserva su historial.", micro });
    }

    [HttpPut("{id:int}/movimiento")]
    [Authorize(Roles = "Administrador,Conductor")]
    public async Task<IActionResult> RegistrarMovimiento(int id, [FromBody] MovimientoDto datos)
    {
        if (datos.Cambio != 1 && datos.Cambio != -1)
            return BadRequest(new { mensaje = "El movimiento debe ser +1 o -1." });

        // Un conductor solo opera unidades asignadas por el administrador.
        if (User.IsInRole("Conductor") && !User.IsInRole("Administrador"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var conductorId))
                return Unauthorized();

            var autorizado = await _db.UbicacionesMicro.AsNoTracking()
                .AnyAsync(u => u.MicroId == id && u.ConductorUsuarioId == conductorId);
            if (!autorizado) return StatusCode(403, new { mensaje = "Este micro no está asignado a tu cuenta de conductor." });
        }

        var actualizados = await _db.Micros
            .Where(m => m.Id == id && m.Activo &&
                m.PasajerosActuales + datos.Cambio >= 0 &&
                m.PasajerosActuales + datos.Cambio <= m.CapacidadMaxima)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.PasajerosActuales, m => m.PasajerosActuales + datos.Cambio)
                .SetProperty(m => m.Estado,
                    m => m.PasajerosActuales + datos.Cambio == 0 ? "Disponible"
                        : m.PasajerosActuales + datos.Cambio == m.CapacidadMaxima ? "Lleno" : "En servicio"));
        if (actualizados == 0)
        {
            if (!await _db.Micros.AnyAsync(m => m.Id == id && m.Activo))
                return NotFound(new { mensaje = "Micro no disponible." });
            return Conflict(new { mensaje = "Movimiento rechazado: ocupación fuera de los límites." });
        }
        var micro = await _db.Micros.AsNoTracking().FirstAsync(m => m.Id == id);
        return Ok(new { mensaje = "Ocupación actualizada correctamente.", micro });
    }
}

public class MicroDto
{
    [Required, StringLength(20, MinimumLength = 1)] public string Linea { get; set; } = "";
    [Required, StringLength(20, MinimumLength = 1)] public string NumeroInterno { get; set; } = "";
    [Range(18, 25)] public int CapacidadMaxima { get; set; }
}
public class EditarMicroDto
{
    [Required, StringLength(20, MinimumLength = 1)] public string NumeroInterno { get; set; } = "";
    [Range(18, 25)] public int CapacidadMaxima { get; set; }
}
public class EstadoMicroDto
{
    public bool Activo { get; set; }
}
public class MovimientoDto
{
    [Range(-1, 1)] public int Cambio { get; set; }
}
