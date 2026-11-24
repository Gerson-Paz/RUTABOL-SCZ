
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RutaBol.Api.Data;
using RutaBol.Api.Models;

namespace RutaBol.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RutaBolDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public AuthController(
        RutaBolDbContext context,
        IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<IActionResult> Registro(
        [FromBody] RegistroDto datos)
    {
        string correo = datos.Correo.Trim().ToLowerInvariant();
        string nombre = datos.NombreCompleto.Trim();

        if (nombre.Length < 3)
        {
            return BadRequest(new
            {
                mensaje = "El nombre debe tener al menos 3 caracteres."
            });
        }

        if (datos.Password.Length < 8)
        {
            return BadRequest(new
            {
                mensaje = "La contraseña debe tener al menos 8 caracteres."
            });
        }

        bool existe = await _context.Usuarios
            .AnyAsync(u => u.Correo == correo);

        if (existe)
        {
            return Conflict(new
            {
                mensaje = "El correo ya está registrado."
            });
        }

        var usuario = new Usuario
        {
            NombreCompleto = nombre,
            Correo = correo,
            Rol = "Pasajero",
            FechaRegistro = DateTime.UtcNow,
            Activo = true
        };

        usuario.PasswordHash =
            _passwordHasher.HashPassword(
                usuario, datos.Password);

        _context.Usuarios.Add(usuario);

        await _context.SaveChangesAsync();

        return StatusCode(201, new
        {
            mensaje = "Usuario registrado correctamente.",
            usuario = new
            {
                usuario.Id,
                usuario.NombreCompleto,
                usuario.Correo,
                usuario.Rol
            }
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto datos)
    {
        string correo = datos.Correo.Trim().ToLowerInvariant();

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == correo);

        if (usuario == null || !usuario.Activo)
        {
            return Unauthorized(new
            {
                mensaje = "Credenciales incorrectas."
            });
        }

        var resultado = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            datos.Password);

        if (resultado == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                mensaje = "Credenciales incorrectas."
            });
        }

        if (resultado ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PasswordHash =
                _passwordHasher.HashPassword(
                    usuario, datos.Password);

            await _context.SaveChangesAsync();
        }

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                usuario.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                usuario.NombreCompleto),

            new Claim(
                ClaimTypes.Email,
                usuario.Correo),

            new Claim(
                ClaimTypes.Role,
                usuario.Rol)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        HttpContext.Session.SetString(
            "NombreUsuario",
            usuario.NombreCompleto);

        HttpContext.Session.SetString(
            "UltimoAcceso",
            DateTime.UtcNow.ToString("O"));

        return Ok(new
        {
            mensaje = "Inicio de sesión exitoso.",
            usuario = new
            {
                usuario.Id,
                usuario.NombreCompleto,
                usuario.Correo,
                usuario.Rol
            }
        });
    }

    [HttpGet("perfil")]
    [Authorize]
    public async Task<IActionResult> Perfil()
    {
        string? idTexto =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(idTexto, out int id))
        {
            return Unauthorized();
        }

        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == id && u.Activo);

        if (usuario == null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            usuario.Id,
            usuario.NombreCompleto,
            usuario.Correo,
            usuario.Rol
        });
    }

    [HttpGet("sesion")]
    [Authorize]
    public IActionResult ConsultarSesion()
    {
        return Ok(new
        {
            autenticado =
                User.Identity?.IsAuthenticated == true,

            nombreGuardado = HttpContext.Session
                .GetString("NombreUsuario"),

            ultimoAcceso = HttpContext.Session
                .GetString("UltimoAcceso")
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        HttpContext.Session.Clear();

        return Ok(new
        {
            mensaje = "Sesión cerrada correctamente."
        });
    }
}

public class RegistroDto
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string NombreCompleto { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Correo { get; set; } = "";

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = "";
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Correo { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}
