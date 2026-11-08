namespace BiblioLink.Services;

public class PrestamosOptions
{
    public const string Seccion = "Prestamos";
    public int DiasPrestamo { get; set; } = 14;
    public int LimitePrestamosActivos { get; set; } = 5;
}