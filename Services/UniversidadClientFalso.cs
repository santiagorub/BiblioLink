using BiblioLink.Interfaces;

namespace BiblioLink.Services;

// TEMPORAL: se borra cuando exista el UniversidadClient real (etapa 5).
public class UniversidadClientFalso : IUniversidadClient
{
    public Task<bool> EsElegibleAsync(string matricula, CancellationToken ct = default)
        => Task.FromResult(true);
}