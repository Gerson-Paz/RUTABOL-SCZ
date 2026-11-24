
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        if (datos.CapacidadMaxima < 18 ||
            datos.CapacidadMaxima > 25)
        {
            return BadRequest(new
            {
                mensaje = "La capacidad debe estar entre 18 y 25 pasajeros."
            });
        }

        string linea = datos.Linea.Trim();
        string numero = datos.NumeroInterno.Trim();

        bool existe = await _context.Micros.AnyAsync(m =>
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
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = micro.Id },
            micro);
    }

    [HttpPut("{id:int}/ocupacion")]
    [Authorize(Roles = "Administrador,Conductor")]
    public async Task<IActionResult> ActualizarOcupacion(
        int id,
        [FromBody] OcupacionDto datos)
    {
        var micro = await _context.Micros.FindAsync(id);

        if (micro == null)
        {
            return NotFound(new
            {
                mensaje = "Micro no encontrado."
            });
        }

        if (datos.PasajerosActuales < 0 ||
            datos.PasajerosActuales > micro.CapacidadMaxima)
        {
            return BadRequest(new
            {
                mensaje = "La cantidad de pasajeros excede la capacidad permitida."
            });
        }

        micro.PasajerosActuales = datos.PasajerosActuales;

        micro.Estado =
            micro.PasajerosActuales == micro.CapacidadMaxima
                ? "Lleno"
                : "En servicio";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Ocupación actualizada.",
            micro
        });
    }
}

public class MicroDto
{
    [Required]
    public string Linea { get; set; } = "";

    [Required]
    public string NumeroInterno { get; set; } = "";

    [Range(18, 25)]
    public int CapacidadMaxima { get; set; }
}

public class OcupacionDto
{
    [Range(0, 1000)]
    public int PasajerosActuales { get; set; }
}