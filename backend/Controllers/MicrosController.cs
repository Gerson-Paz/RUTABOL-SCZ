
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RutaBol.Api.Data;
using RutaBol.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace RutaBol.Api.Controllers;

[ApiController]
[Route("api/micros")]
public class MicrosController : ControllerBase
{
    private readonly RutaBolDbContext _context;

    public MicrosController(RutaBolDbContext context)
    {
        _context = context;
    }

    // Consultar todos los micros registrados
    [HttpGet]
    public async Task<IActionResult> ObtenerMicros()
    {
        var micros = await _context.Micros
            .AsNoTracking()
            .OrderBy(m => m.Linea)
            .ThenBy(m => m.NumeroInterno)
            .ToListAsync();

        return Ok(micros);
    }

    // Consultar un micro mediante su ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var micro = await _context.Micros
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (micro == null)
        {
            return NotFound(new
            {
                mensaje = "Micro no encontrado."
            });
        }

        return Ok(micro);
    }

    // Registrar un micro
    // Acceso exclusivo para administradores
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Registrar(
        [FromBody] MicroDto datos)
    {
        if (string.IsNullOrWhiteSpace(datos.Linea) ||
            string.IsNullOrWhiteSpace(datos.NumeroInterno))
        {
            return BadRequest(new
            {
                mensaje = "La línea y el número interno son obligatorios."
            });
        }

        string linea = datos.Linea.Trim();
        string numero = datos.NumeroInterno.Trim();

        if (linea.Length > 20 || numero.Length > 20)
        {
            return BadRequest(new
            {
                mensaje = "La línea y el número interno deben tener como máximo 20 caracteres."
            });
        }

        if (datos.CapacidadMaxima < 18 ||
            datos.CapacidadMaxima > 25)
        {
            return BadRequest(new
            {
                mensaje = "La capacidad debe estar entre 18 y 25 pasajeros."
            });
        }

        bool existe = await _context.Micros
            .AnyAsync(m =>
                m.Linea == linea &&
                m.NumeroInterno == numero);

        if (existe)
        {
            return Conflict(new
            {
                mensaje = "Este micro ya se encuentra registrado."
            });
        }

        var micro = new Micro
        {
            Linea = linea,
            NumeroInterno = numero,
            CapacidadMaxima = datos.CapacidadMaxima,
            PasajerosActuales = 0,
            Estado = "Disponible",
            FechaRegistro = DateTime.UtcNow
        };

        _context.Micros.Add(micro);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg &&
                  pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new
            {
                mensaje = "Este micro ya se encuentra registrado."
            });
        }

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = micro.Id },
            micro);
    }

    // Registrar subida o bajada de un pasajero
    // Administradores y conductores autorizados
    [HttpPut("{id:int}/movimiento")]
    [Authorize(Roles = "Administrador,Conductor")]
    public async Task<IActionResult> RegistrarMovimiento(
        int id,
        [FromBody] MovimientoDto datos)
    {
        if (datos.Cambio != 1 && datos.Cambio != -1)
        {
            return BadRequest(new
            {
                mensaje = "El movimiento debe ser +1 o -1."
            });
        }

        int actualizados = await _context.Micros
            .Where(m =>
                m.Id == id &&
                m.PasajerosActuales + datos.Cambio >= 0 &&
                m.PasajerosActuales + datos.Cambio <=
                    m.CapacidadMaxima)
            .ExecuteUpdateAsync(setters => setters

                .SetProperty(
                    m => m.PasajerosActuales,
                    m => m.PasajerosActuales + datos.Cambio)

                .SetProperty(
                    m => m.Estado,
                    m => m.PasajerosActuales + datos.Cambio == 0
                        ? "Disponible"
                        : m.PasajerosActuales + datos.Cambio ==
                          m.CapacidadMaxima
                            ? "Lleno"
                            : "En servicio")
            );

        if (actualizados == 0)
        {
            bool existe = await _context.Micros
                .AnyAsync(m => m.Id == id);

            if (!existe)
            {
                return NotFound(new
                {
                    mensaje = "Micro no encontrado."
                });
            }

            return Conflict(new
            {
                mensaje = "Movimiento rechazado: el micro está lleno o no tiene suficientes pasajeros."
            });
        }

        var micro = await _context.Micros
            .AsNoTracking()
            .FirstAsync(m => m.Id == id);

        return Ok(new
        {
            mensaje = "Ocupación actualizada correctamente.",
            micro
        });
    }
}

// Datos para registrar un micro
public class MicroDto
{
    [Required(ErrorMessage = "La línea es obligatoria.")]
    [StringLength(20, MinimumLength = 1)]
    public string Linea { get; set; } = "";

    [Required(ErrorMessage = "El número interno es obligatorio.")]
    [StringLength(20, MinimumLength = 1)]
    public string NumeroInterno { get; set; } = "";

    [Range(18, 25, ErrorMessage =
        "La capacidad debe estar entre 18 y 25 pasajeros.")]
    public int CapacidadMaxima { get; set; }
}

// Datos para actualizar la ocupación
public class MovimientoDto
{
    [Range(-1, 1)]
    public int Cambio { get; set; }
}
