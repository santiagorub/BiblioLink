using BiblioLink.DTOs;

namespace BiblioLink.Interfaces;

public interface IPrestamoService
{
    Task<PrestamoReadDto> CrearAsync(CreatePrestamoDto dto, CancellationToken ct = default);
    Task<PrestamoReadDto> DevolverAsync(int prestamoId, CancellationToken ct = default);
    Task<IEnumerable<PrestamoReadDto>> ListarPorUsuarioAsync(int usuarioId, bool soloActivos, CancellationToken ct = default);
    Task<IEnumerable<PrestamoReadDto>> HistorialPorLibroAsync(int libroId, CancellationToken ct = default);
}