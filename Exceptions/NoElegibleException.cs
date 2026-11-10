namespace BiblioLink.Exceptions;

public class NoElegibleException : Exception
{
    public NoElegibleException(string mensaje) : base(mensaje) { }
}