namespace BiblioLink.Exceptions;

public class ServicioExternoException : Exception
{
    public int StatusCode { get; }

    public ServicioExternoException(string mensaje, int statusCode, Exception? inner = null)
        : base(mensaje, inner)
    {
        StatusCode = statusCode;
    }
}