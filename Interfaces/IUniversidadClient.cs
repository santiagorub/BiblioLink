namespace BiblioLink.Interfaces;

public interface IUniversidadClient
{
    Task<bool> EsElegibleAsync(string matricula, CancellationToken ct = default);
}