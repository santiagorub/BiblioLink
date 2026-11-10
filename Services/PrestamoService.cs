using BiblioLink.DTOs;
using BiblioLink.Exceptions;
using BiblioLink.Interfaces;
using BiblioLink.Models;
using BiblioLink.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BiblioLink.Services;

public class PrestamoService : IPrestamoService
{
    private readonly BibliotecaContext _db;
    private readonly IUniversidadClient _universidad;
    private readonly PrestamosOptions _opts;
    private readonly ILogger<PrestamoService> _logger;

    public PrestamoService(
        BibliotecaContext db,
        IUniversidadClient universidad,
        IOptions<PrestamosOptions> opts,
        ILogger<PrestamoService> logger)
    {
        _db = db;
        _universidad = universidad;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task<PrestamoReadDto> CrearAsync(CreatePrestamoDto dto, CancellationToken ct = default)
    {
        // 2. El libro existe
        var libro = await _db.Libros.FirstOrDefaultAsync(l => l.Id == dto.LibroId, ct)
            ?? throw new NotFoundException($"No existe el libro con id {dto.LibroId}.");

        // 3. Hay stock
        if (libro.CantidadDisponible <= 0)
        {
            _logger.LogWarning("Préstamo denegado: sin stock del libro {LibroId}", libro.Id);
            throw new ReglaNegocioException("No hay ejemplares disponibles de este libro.");
        }

        // 4. El usuario existe
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == dto.UsuarioId, ct)
            ?? throw new NotFoundException($"No existe el usuario con id {dto.UsuarioId}.");

        var ahora = DateTime.UtcNow;

        // 5. Desafío: mora (vigente y con fecha prevista vencida)
        var tieneMora = await _db.Prestamos.AnyAsync(p =>
            p.UsuarioId == usuario.Id &&
            p.Estado == EstadoPrestamo.Vigente &&
            p.FechaDevolucionPrevista < ahora, ct);
        if (tieneMora)
        {
            _logger.LogWarning("Préstamo denegado: el usuario {UsuarioId} tiene préstamos en mora", usuario.Id);
            throw new NoElegibleException("El usuario tiene libros vencidos sin devolver.");
        }

        // 6. Límite de préstamos activos
        var activos = await _db.Prestamos.CountAsync(p =>
            p.UsuarioId == usuario.Id && p.Estado == EstadoPrestamo.Vigente, ct);
        if (activos >= _opts.LimitePrestamosActivos)
        {
            _logger.LogWarning("Préstamo denegado: el usuario {UsuarioId} alcanzó el límite de {Limite}",
                usuario.Id, _opts.LimitePrestamosActivos);
            throw new NoElegibleException(
                $"El usuario alcanzó el límite de {_opts.LimitePrestamosActivos} préstamos activos.");
        }

        // 7. API externa (la más cara, va al final)
        bool elegible;
        try
        {
            elegible = await _universidad.EsElegibleAsync(usuario.Matricula, ct);
        }
        catch (ServicioExternoException ex)
        {
            _logger.LogError(ex, "Falló la API externa al verificar la matrícula {Matricula}", usuario.Matricula);
            throw;
        }
        if (!elegible)
        {
            _logger.LogWarning("Préstamo denegado: matrícula {Matricula} no elegible según la universidad", usuario.Matricula);
            throw new NoElegibleException("La universidad indica que el usuario no está activo/vigente.");
        }

        // 8. Crear el préstamo y descontar stock en un solo SaveChanges (atómico)
        var prestamo = new Prestamo
        {
            LibroId = libro.Id,
            UsuarioId = usuario.Id,
            FechaPrestamo = ahora,
            FechaDevolucionPrevista = ahora.AddDays(_opts.DiasPrestamo),
            Estado = EstadoPrestamo.Vigente
        };
        libro.CantidadDisponible--;
        _db.Prestamos.Add(prestamo);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Préstamo {PrestamoId} creado: libro {LibroId}, usuario {UsuarioId}",
            prestamo.Id, libro.Id, usuario.Id);

        prestamo.Libro = libro;
        prestamo.Usuario = usuario;
        return ToDto(prestamo);
    }

    public async Task<PrestamoReadDto> DevolverAsync(int prestamoId, CancellationToken ct = default)
    {
        var prestamo = await _db.Prestamos
            .Include(p => p.Libro)
            .Include(p => p.Usuario)
            .FirstOrDefaultAsync(p => p.Id == prestamoId, ct)
            ?? throw new NotFoundException($"No existe el préstamo con id {prestamoId}.");

        if (prestamo.Estado == EstadoPrestamo.Devuelto)
            throw new ReglaNegocioException("El préstamo ya fue devuelto.");

        prestamo.FechaDevolucionReal = DateTime.UtcNow;
        prestamo.Estado = EstadoPrestamo.Devuelto;

        if (prestamo.Libro.CantidadDisponible < prestamo.Libro.CantidadTotal)
            prestamo.Libro.CantidadDisponible++;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Préstamo {PrestamoId} devuelto (libro {LibroId})", prestamo.Id, prestamo.LibroId);
        return ToDto(prestamo);
    }

    public async Task<IEnumerable<PrestamoReadDto>> ListarPorUsuarioAsync(int usuarioId, bool soloActivos, CancellationToken ct = default)
    {
        if (!await _db.Usuarios.AnyAsync(u => u.Id == usuarioId, ct))
            throw new NotFoundException($"No existe el usuario con id {usuarioId}.");

        var query = _db.Prestamos
            .Include(p => p.Libro).Include(p => p.Usuario)
            .AsNoTracking()
            .Where(p => p.UsuarioId == usuarioId);

        if (soloActivos)
            query = query.Where(p => p.Estado == EstadoPrestamo.Vigente);

        var lista = await query.OrderByDescending(p => p.FechaPrestamo).ToListAsync(ct);
        return lista.Select(ToDto);
    }

    public async Task<IEnumerable<PrestamoReadDto>> HistorialPorLibroAsync(int libroId, CancellationToken ct = default)
    {
        if (!await _db.Libros.AnyAsync(l => l.Id == libroId, ct))
            throw new NotFoundException($"No existe el libro con id {libroId}.");

        var lista = await _db.Prestamos
            .Include(p => p.Libro).Include(p => p.Usuario)
            .AsNoTracking()
            .Where(p => p.LibroId == libroId)
            .OrderByDescending(p => p.FechaPrestamo)
            .ToListAsync(ct);
        return lista.Select(ToDto);
    }

    private static PrestamoReadDto ToDto(Prestamo p) => new()
    {
        Id = p.Id,
        LibroId = p.LibroId,
        TituloLibro = p.Libro.Titulo,
        UsuarioId = p.UsuarioId,
        MatriculaUsuario = p.Usuario.Matricula,
        FechaPrestamo = p.FechaPrestamo,
        FechaDevolucionPrevista = p.FechaDevolucionPrevista,
        FechaDevolucionReal = p.FechaDevolucionReal,
        Estado = p.Estado.ToString(),
        Vencido = p.Estado == EstadoPrestamo.Vigente && p.FechaDevolucionPrevista < DateTime.UtcNow
    };
}