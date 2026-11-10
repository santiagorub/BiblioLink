using BiblioLink.Models.Enums;

namespace BiblioLink.Models;

public class Prestamo
{
    public int Id { get; set; }

    public int LibroId { get; set; }
    public Libro Libro { get; set; } = null!;

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public DateTime FechaPrestamo { get; set; }
    public DateTime FechaDevolucionPrevista { get; set; }
    public DateTime? FechaDevolucionReal { get; set; }

    public EstadoPrestamo Estado { get; set; } = EstadoPrestamo.Vigente;
}