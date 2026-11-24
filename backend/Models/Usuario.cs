
namespace RutaBol.Api.Models;

public class Usuario
{
    public int Id { get; set; }

    public string NombreCompleto { get; set; } = "";

    public string Correo { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Rol { get; set; } = "Pasajero";

    public DateTime FechaRegistro { get; set; } =
        DateTime.UtcNow;

    public bool Activo { get; set; } = true;
}
