using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RutaBol.Api.Data;
using RutaBol.Api.Models;
using System.Security.Claims;

namespace RutaBol.Api.Controllers;

[ApiController]
[Route("api/categorias")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly RutaBolDbContext _db;
    private readonly IWebHostEnvironment _env;
    private static readonly HashSet<string> Permitidas = new(StringComparer.Ordinal)
    {
        "Universitaria", "Secundaria", "Primaria y menores",
        "Adulto mayor", "Persona con discapacidad"
    };

    public CategoriasController(RutaBolDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private int? IdUsuario() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    // Enviamos una solicitud para revisar la categoría
    [HttpPost("solicitar")]
    [RequestSizeLimit(3_000_000)]
    public async Task<IActionResult> Solicitar([FromForm] SolicitudCategoriaDto datos)
    {
        var usuarioId = IdUsuario();
        if (usuarioId == null) return Unauthorized(new { mensaje = "La sesión no contiene el ID del usuario." });
        if (!Permitidas.Contains(datos.Categoria)) return BadRequest(new { mensaje = "Categoría no válida." });
        if (string.IsNullOrWhiteSpace(datos.Codigo) || datos.Codigo.Trim().Length > 50)
            return BadRequest(new { mensaje = "El código es obligatorio y no puede superar 50 caracteres." });

        if (datos.Categoria == "Universitaria" &&
            (string.IsNullOrWhiteSpace(datos.CorreoInstitucional) ||
             !datos.CorreoInstitucional.Trim().EndsWith("@upds.edu.bo", StringComparison.OrdinalIgnoreCase) ||
             datos.CorreoInstitucional.Contains(' ')))
            return BadRequest(new { mensaje = "El correo debe terminar en @upds.edu.bo." });

        // Esta revisión es manual; todavía no consultamos RUDE ni padrones externos
        if (datos.Foto == null || datos.Foto.Length == 0 || datos.Foto.Length > 2_000_000)
            return BadRequest(new { mensaje = "Adjunta una foto JPG o PNG de máximo 2 MB." });

        var esJpg = datos.Foto.ContentType == "image/jpeg";
        var esPng = datos.Foto.ContentType == "image/png";
        if (!esJpg && !esPng) return BadRequest(new { mensaje = "Solo se permiten imágenes JPG o PNG." });

        await using var contenido = datos.Foto.OpenReadStream();
        byte[] cabecera = new byte[8];
        var leidos = await contenido.ReadAsync(cabecera.AsMemory());
        bool jpgValido = leidos >= 3 && cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF;
        bool pngValido = leidos == 8 && cabecera.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        if ((esJpg && !jpgValido) || (esPng && !pngValido))
            return BadRequest(new { mensaje = "El contenido de la imagen no es válido." });

        if (await _db.SolicitudesCategoria.AnyAsync(s => s.UsuarioId == usuarioId && s.Estado == "Pendiente"))
            return Conflict(new { mensaje = "Ya tienes una solicitud pendiente." });

        var carpeta = Path.Combine(_env.ContentRootPath, "archivos_privados", "categorias");
        Directory.CreateDirectory(carpeta);
        string nombreArchivo = Guid.NewGuid().ToString("N") + (esJpg ? ".jpg" : ".png");
        string ruta = Path.Combine(carpeta, nombreArchivo);
        await using (var archivo = System.IO.File.Create(ruta))
        await using (var fotoCompleta = datos.Foto.OpenReadStream())
            await fotoCompleta.CopyToAsync(archivo);

        var solicitud = new SolicitudCategoria
        {
            UsuarioId = usuarioId.Value,
            Categoria = datos.Categoria,
            Codigo = datos.Codigo.Trim(),
            CorreoInstitucional = datos.CorreoInstitucional?.Trim(),
            NombreArchivo = nombreArchivo,
            TipoArchivo = datos.Foto.ContentType,
            Estado = "Pendiente"
        };
        try
        {
            _db.SolicitudesCategoria.Add(solicitud);
            await _db.SaveChangesAsync();
        }
        catch
        {
            System.IO.File.Delete(ruta);
            throw;
        }
        return Ok(new { mensaje = "Solicitud enviada para revisión.", solicitud.Id, solicitud.Estado });
    }

    [HttpGet("mis-solicitudes")]
    public async Task<IActionResult> MisSolicitudes()
    {
        var id = IdUsuario();
        if (id == null) return Unauthorized(new { mensaje = "Falta el ID de usuario en la sesión." });
        return Ok(await _db.SolicitudesCategoria.AsNoTracking()
            .Where(s => s.UsuarioId == id).OrderByDescending(s => s.FechaSolicitud)
            .Select(s => new { s.Id, s.Categoria, s.Codigo, s.CorreoInstitucional, s.Estado, s.FechaSolicitud, s.FechaRevision })
            .ToListAsync());
    }

    [HttpGet("pendientes")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Pendientes() => Ok(await _db.SolicitudesCategoria.AsNoTracking()
        .Where(s => s.Estado == "Pendiente").OrderBy(s => s.FechaSolicitud)
        .Select(s => new { s.Id, s.UsuarioId, s.Categoria, s.Codigo, s.CorreoInstitucional, s.FechaSolicitud })
        .ToListAsync());

    [HttpGet("{id:int}/foto")]
    public async Task<IActionResult> Foto(int id)
    {
        var solicitud = await _db.SolicitudesCategoria.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (solicitud == null) return NotFound();
        var usuarioId = IdUsuario();
        if (usuarioId == null || (solicitud.UsuarioId != usuarioId && !User.IsInRole("Administrador"))) return Forbid();
        var ruta = Path.Combine(_env.ContentRootPath, "archivos_privados", "categorias", solicitud.NombreArchivo);
        if (!System.IO.File.Exists(ruta)) return NotFound();
        return PhysicalFile(ruta, solicitud.TipoArchivo);
    }

    // El administrador revisa los documentos y decide
    [HttpPut("{id:int}/revisar")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Revisar(int id, [FromBody] RevisionCategoriaDto datos)
    {
        if (datos.Estado != "Aprobada" && datos.Estado != "Rechazada")
            return BadRequest(new { mensaje = "El estado debe ser Aprobada o Rechazada." });
        var adminId = IdUsuario();
        if (adminId == null) return Unauthorized();
        int actualizados = await _db.SolicitudesCategoria
            .Where(s => s.Id == id && s.Estado == "Pendiente")
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Estado, datos.Estado)
                .SetProperty(x => x.FechaRevision, (DateTime?)DateTime.UtcNow)
                .SetProperty(x => x.AdministradorId, adminId));
        if (actualizados == 0) return Conflict(new { mensaje = "La solicitud no existe o ya fue revisada." });
        return Ok(new { mensaje = "Solicitud revisada.", estado = datos.Estado });
    }
}

public class SolicitudCategoriaDto
{
    public string Categoria { get; set; } = "";
    public string Codigo { get; set; } = "";
    public string? CorreoInstitucional { get; set; }
    public IFormFile? Foto { get; set; }
}

public class RevisionCategoriaDto
{
    public string Estado { get; set; } = "";
}
