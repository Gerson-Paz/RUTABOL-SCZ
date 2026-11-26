using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RutaBol.Api.Data;
using RutaBol.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace RutaBol.Api.Controllers;

[ApiController]
[Route("api/conductores")]
[Authorize(Roles = "Administrador")]
public class ConductoresController : ControllerBase
{
    private readonly RutaBolDbContext _db;
    public ConductoresController(RutaBolDbContext db) => _db = db;

    // Los datos de asignación solo se muestran al administrador.
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var conductores = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Rol == "Conductor" && u.Activo)
            .OrderBy(u => u.NombreCompleto).ThenBy(u => u.Correo)
            .Select(u => new { u.Id, u.NombreCompleto, u.Correo, u.Rol })
            .ToListAsync();

        var asignaciones = await (from gps in _db.UbicacionesMicro.AsNoTracking()
                                  join micro in _db.Micros.AsNoTracking() on gps.MicroId equals micro.Id
                                  where gps.ConductorUsuarioId != null
                                  select new
                                  {
                                      conductorId = gps.ConductorUsuarioId!.Value,
                                      microId = micro.Id,
                                      linea = micro.Linea,
                                      numeroInterno = micro.NumeroInterno,
                                      activo = micro.Activo
                                  }).ToListAsync();

        return Ok(conductores.Select(c => new {
            c.Id, c.NombreCompleto, c.Correo, c.Rol,
            unidad = asignaciones.Where(a => a.conductorId == c.Id)
                .Select(a => new { a.microId, a.linea, a.numeroInterno, a.activo })
                .FirstOrDefault(),
            cantidadAsignaciones = asignaciones.Count(a => a.conductorId == c.Id)
        }));
    }

    [HttpPut("habilitar")]
    public async Task<IActionResult> Habilitar([FromBody] CorreoConductorDto datos)
    {
        var correo = (datos.Correo ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(correo))
            return BadRequest(new { mensaje = "Indica el correo del usuario." });

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo.ToLower() == correo);
        if (usuario is null || !usuario.Activo)
            return NotFound(new { mensaje = "No se encontró una cuenta activa con ese correo." });
        if (usuario.Rol == "Administrador")
            return Conflict(new { mensaje = "No puedes cambiar el rol de un administrador desde aquí." });
        if (usuario.Rol == "Conductor")
            return Ok(new { mensaje = "Esta cuenta ya tiene el rol Conductor.", usuario.Id, usuario.Correo, usuario.Rol });
        if (usuario.Rol != "Pasajero")
            return Conflict(new { mensaje = "Solo se pueden habilitar cuentas de pasajeros." });

        usuario.Rol = "Conductor";
        await _db.SaveChangesAsync();
        return Ok(new { mensaje = "Conductor habilitado. Debe cerrar sesión y volver a ingresar.", usuario.Id, usuario.Correo, usuario.Rol });
    }

    // Al guardar una nueva unidad liberamos la anterior. Nadie puede compartir un micro asignado.
    [HttpPut("{conductorId:int}/unidad")]
    public async Task<IActionResult> GuardarUnidad(int conductorId, [FromBody] UnidadConductorDto datos)
    {
        if (datos.MicroId is <= 0)
            return BadRequest(new { mensaje = "Selecciona una unidad válida o elige Sin unidad." });

        await using var transaccion = await _db.Database.BeginTransactionAsync();

        // Serializamos los cambios de asignación, incluidos los realizados desde dos paneles.
        // El bloqueo es transaccional y no afecta al registro de ocupación de otros micros.
        await _db.Database.ExecuteSqlRawAsync("LOCK TABLE \"UbicacionesMicro\" IN SHARE ROW EXCLUSIVE MODE");

        var conductor = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(
            u => u.Id == conductorId && u.Activo && u.Rol == "Conductor");
        if (conductor is null)
            return NotFound(new { mensaje = "El conductor no existe o no tiene el rol Conductor." });

        Micro? micro = null;
        if (datos.MicroId.HasValue)
        {
            micro = await _db.Micros.AsNoTracking().FirstOrDefaultAsync(
                m => m.Id == datos.MicroId.Value && m.Activo);
            if (micro is null)
                return BadRequest(new { mensaje = "El interno elegido no existe o está retirado." });

            var ocupado = await _db.UbicacionesMicro.AsNoTracking()
                .AnyAsync(u => u.MicroId == micro.Id && u.ConductorUsuarioId != null && u.ConductorUsuarioId != conductorId);
            if (ocupado)
                return Conflict(new { mensaje = "Ese micro ya está asignado a otro conductor. Primero debes desasignarlo." });
        }

        // Se quitan todas las asignaciones anteriores de este conductor sin borrar el GPS.
        await _db.UbicacionesMicro.Where(u => u.ConductorUsuarioId == conductorId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.ConductorUsuarioId, (int?)null));

        if (micro is not null)
        {
            var ubicacion = await _db.UbicacionesMicro.FindAsync(micro.Id);
            if (ubicacion is null)
            {
                ubicacion = new UbicacionMicro { MicroId = micro.Id };
                _db.UbicacionesMicro.Add(ubicacion);
            }
            ubicacion.ConductorUsuarioId = conductorId;
            await _db.SaveChangesAsync();
        }
        await transaccion.CommitAsync();
        return Ok(new {
            mensaje = micro is null ? "Conductor sin unidad asignada." : "Asignación guardada correctamente.",
            conductorId, microId = micro?.Id, linea = micro?.Linea, numeroInterno = micro?.NumeroInterno
        });
    }
}

public class CorreoConductorDto
{
    [Required, EmailAddress, MaxLength(254)]
    public string Correo { get; set; } = "";
}

public class UnidadConductorDto
{
    public int? MicroId { get; set; }
}
