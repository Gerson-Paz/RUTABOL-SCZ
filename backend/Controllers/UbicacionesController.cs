using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RutaBol.Api.Data;
using RutaBol.Api.Models;
using System.Security.Claims;

namespace RutaBol.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ubicaciones")]
public class UbicacionesController : ControllerBase
{
    private readonly RutaBolDbContext _db;
    private const int VigenciaMinutos = 15;
    public UbicacionesController(RutaBolDbContext db) => _db = db;

    private int? UsuarioActualId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private bool EsAdministrador() => User.IsInRole("Administrador");
    private static bool CoordenadasValidas(double lat, double lon) =>
        double.IsFinite(lat) && double.IsFinite(lon) && lat is >= -90 and <= 90 && lon is >= -180 and <= 180;

    private static double DistanciaKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double radioKm = 6371.0088;
        double a = Math.Pow(Math.Sin((lat2 - lat1) * Math.PI / 360), 2)
                 + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                 * Math.Pow(Math.Sin((lon2 - lon1) * Math.PI / 360), 2);
        return 2 * radioKm * Math.Asin(Math.Min(1, Math.Sqrt(Math.Max(0, a))));
    }

    // El pasajero ve solo micros activos con posición reciente.
    [HttpGet("micros")]
    public async Task<IActionResult> ObtenerMapa()
    {
        var desde = DateTime.UtcNow.AddMinutes(-VigenciaMinutos);
        var lista = await (from u in _db.UbicacionesMicro.AsNoTracking()
                           join m in _db.Micros.AsNoTracking() on u.MicroId equals m.Id
                           where m.Activo && u.ActualizadoEn != null && u.ActualizadoEn >= desde
                           select new {
                               microId = m.Id, linea = m.Linea, numeroInterno = m.NumeroInterno,
                               capacidadMaxima = m.CapacidadMaxima, pasajerosActuales = m.PasajerosActuales,
                               estado = m.Estado, latitud = u.Latitud, longitud = u.Longitud,
                               precisionMetros = u.PrecisionMetros, actualizadoEn = u.ActualizadoEn
                           }).ToListAsync();
        return Ok(lista);
    }

    [HttpGet("cercanos")]
    public async Task<IActionResult> Cercanos([FromQuery] double lat, [FromQuery] double lon, [FromQuery] double radioKm = 5)
    {
        if (!CoordenadasValidas(lat, lon) || !double.IsFinite(radioKm) || radioKm <= 0 || radioKm > 5)
            return BadRequest(new { mensaje = "Indica coordenadas válidas y un radio de 0 a 5 km." });
        var desde = DateTime.UtcNow.AddMinutes(-VigenciaMinutos);
        var registros = await (from u in _db.UbicacionesMicro.AsNoTracking()
                               join m in _db.Micros.AsNoTracking() on u.MicroId equals m.Id
                               where m.Activo && u.ActualizadoEn != null && u.ActualizadoEn >= desde
                               select new { microId = m.Id, linea = m.Linea, numeroInterno = m.NumeroInterno,
                                   pasajerosActuales = m.PasajerosActuales, capacidadMaxima = m.CapacidadMaxima,
                                   estado = m.Estado, latitud = u.Latitud, longitud = u.Longitud,
                                   precisionMetros = u.PrecisionMetros, actualizadoEn = u.ActualizadoEn }).ToListAsync();
        var cercanos = registros.Select(u => new {
            u.microId, u.linea, u.numeroInterno, u.pasajerosActuales, u.capacidadMaxima,
            u.estado, u.latitud, u.longitud, u.precisionMetros, u.actualizadoEn,
            distanciaKm = DistanciaKm(lat, lon, u.latitud, u.longitud)
        }).Where(u => u.distanciaKm <= radioKm).OrderBy(u => u.distanciaKm)
          .Select(u => new { u.microId, u.linea, u.numeroInterno, u.pasajerosActuales, u.capacidadMaxima,
              u.estado, u.latitud, u.longitud, u.precisionMetros, u.actualizadoEn,
              distanciaKm = Math.Round(u.distanciaKm, 3) }).ToList();
        return Ok(cercanos);
    }

    [HttpGet("mis-unidades")]
    [Authorize(Roles = "Administrador,Conductor")]
    public async Task<IActionResult> MisUnidades()
    {
        if (EsAdministrador())
            return Ok(await _db.Micros.AsNoTracking().Where(m => m.Activo)
                .OrderBy(m => m.Linea).ThenBy(m => m.NumeroInterno)
                .Select(m => new { m.Id, m.Linea, m.NumeroInterno }).ToListAsync());
        var id = UsuarioActualId();
        if (id is null) return Unauthorized();
        var micros = await (from u in _db.UbicacionesMicro.AsNoTracking()
                            join m in _db.Micros.AsNoTracking() on u.MicroId equals m.Id
                            where u.ConductorUsuarioId == id && m.Activo
                            orderby m.Linea, m.NumeroInterno
                            select new { m.Id, m.Linea, m.NumeroInterno }).ToListAsync();
        return Ok(micros);
    }

    [HttpPut("micros/{microId:int}")]
    [Authorize(Roles = "Administrador,Conductor")]
    public async Task<IActionResult> Reportar(int microId, [FromBody] ReportarUbicacionDto datos)
    {
        if (!CoordenadasValidas(datos.Latitud, datos.Longitud) ||
            (datos.PrecisionMetros.HasValue && (!double.IsFinite(datos.PrecisionMetros.Value) || datos.PrecisionMetros < 0 || datos.PrecisionMetros > 100000)))
            return BadRequest(new { mensaje = "Coordenadas o precisión inválidas." });
        if (!await _db.Micros.AnyAsync(m => m.Id == microId && m.Activo))
            return NotFound(new { mensaje = "Micro no disponible." });
        var id = UsuarioActualId();
        if (id is null) return Unauthorized();
        var ubicacion = await _db.UbicacionesMicro.FindAsync(microId);
        if (!EsAdministrador() && (ubicacion is null || ubicacion.ConductorUsuarioId != id))
            return Forbid();
        if (ubicacion is null)
        {
            ubicacion = new UbicacionMicro { MicroId = microId };
            _db.UbicacionesMicro.Add(ubicacion);
        }
        ubicacion.Latitud = datos.Latitud;
        ubicacion.Longitud = datos.Longitud;
        ubicacion.PrecisionMetros = datos.PrecisionMetros;
        ubicacion.ActualizadoEn = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { mensaje = "Ubicación recibida.", microId,
            actualizadoEn = ubicacion.ActualizadoEn });
    }
}

public class ReportarUbicacionDto
{
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public double? PrecisionMetros { get; set; }
}

