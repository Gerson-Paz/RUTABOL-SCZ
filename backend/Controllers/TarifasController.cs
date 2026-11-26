
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RutaBol.Api.Data;
using RutaBol.Api.Models;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;

namespace RutaBol.Api.Controllers;

[ApiController]
[Route("api/tarifas")]
public class TarifasController : ControllerBase
{
    private readonly RutaBolDbContext _context;

    public TarifasController(RutaBolDbContext context)
    {
        _context = context;
    }

    // Mostramos las tarifas que están vigentes
    [HttpGet]
    public async Task<IActionResult> ObtenerTarifas()
    {
        var tarifas = await _context.Tarifas
            .AsNoTracking()
            .Where(t => t.Vigente)
            .OrderBy(t => t.Nombre)
            .ToListAsync();

        return Ok(tarifas);
    }

    // Podemos consultar una tarifa por su ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var tarifa = await _context.Tarifas
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tarifa == null)
        {
            return NotFound(new
            {
                mensaje = "Tarifa no encontrada."
            });
        }

        return Ok(tarifa);
    }
    

    // El administrador puede revisar los cambios anteriores
    [HttpGet("historial")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ObtenerHistorial()
    {
        var historial = await _context.Tarifas
            .AsNoTracking()
            .OrderBy(t => t.Nombre)
            .ThenByDescending(t => t.FechaInicio)
            .ToListAsync();

        return Ok(historial);
    }

    // Registramos una tarifa o creamos una versión nueva
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> GuardarTarifa(
        [FromBody] TarifaDto datos)
    {
        string nombre = datos.Nombre?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(nombre))
        {
            return BadRequest(new
            {
                mensaje = "Debes escribir el nombre de la tarifa."
            });
        }

        if (nombre.Length > 100)
        {
            return BadRequest(new
            {
                mensaje = "El nombre es demasiado largo."
            });
        }

        if (datos.Monto <= 0 || datos.Monto > 9999.99m)
        {
            return BadRequest(new
            {
                mensaje = "El monto debe ser mayor a cero y no superar Bs 9999,99."
            });
        }

        if (decimal.Round(datos.Monto, 2) != datos.Monto)
        {
            return BadRequest(new
            {
                mensaje = "El monto solo puede tener dos decimales."
            });
        }

        int? administradorId = null;

        string? idUsuario = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (int.TryParse(idUsuario, out int id))
        {
            administradorId = id;
        }

        DateTime fechaActual = DateTime.UtcNow;

        try
        {
            // Guardamos el cambio completo en una transacción
            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            // Buscamos si ya hay una tarifa con este nombre
            var anteriores = await _context.Tarifas
                .Where(t =>
                    t.Vigente &&
                    t.Nombre.ToLower() == nombre.ToLower())
                .ToListAsync();

            // Cerramos la vigencia de las tarifas anteriores
            foreach (var anterior in anteriores)
            {
                anterior.Vigente = false;
                anterior.FechaFin = fechaActual;
            }

            var nuevaTarifa = new Tarifa
            {
                Nombre = nombre,
                Monto = datos.Monto,
                Moneda = "BOB",
                Vigente = true,
                FechaInicio = fechaActual,
                FechaFin = null,
                UsuarioAdministradorId = administradorId
            };

            _context.Tarifas.Add(nuevaTarifa);

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = nuevaTarifa.Id },
                new
                {
                    mensaje = "Tarifa guardada correctamente.",
                    tarifa = nuevaTarifa
                });
        }
        catch (PostgresException ex)
            when (ex.SqlState ==
                PostgresErrorCodes.SerializationFailure)
        {
            return Conflict(new
            {
                mensaje = "Otro administrador modificó las tarifas al mismo tiempo. Vuelve a intentarlo."
            });
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg &&
                  pg.SqlState ==
                    PostgresErrorCodes.SerializationFailure)
        {
            return Conflict(new
            {
                mensaje = "Otro administrador modificó las tarifas al mismo tiempo. Vuelve a intentarlo."
            });
        }
    }
}

public class TarifaDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    public decimal Monto { get; set; }
}