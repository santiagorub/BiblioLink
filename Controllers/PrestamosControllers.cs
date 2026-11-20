using BiblioLink.DTOs;
using BiblioLink.Exceptions;
using BiblioLink.Interfaces;
using BiblioLink.Filters;
using BiblioLink.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BiblioLink.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/prestamos")]
public class PrestamosController : ControllerBase
{
    private readonly IPrestamoService _service;

    public PrestamosController(IPrestamoService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CreatePrestamoDto dto, CancellationToken ct)
    {
        try
        {
            var creado = await _service.CrearAsync(dto, ct);
            return Created($"/api/prestamos/{creado.Id}", creado);
        }
        catch (Exception ex) when (ex is NotFoundException or ReglaNegocioException or NoElegibleException)
        {
            return Traducir(ex);
        }
    }

    [HttpPost("{id:int}/devolucion")]
    public async Task<IActionResult> Devolver(int id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.DevolverAsync(id, ct));
        }
        catch (Exception ex) when (ex is NotFoundException or ReglaNegocioException)
        {
            return Traducir(ex);
        }
    }

    [HttpGet("/api/usuarios/{usuarioId:int}/prestamos")]
    public async Task<IActionResult> PorUsuario(int usuarioId, [FromQuery] bool activos = false, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.ListarPorUsuarioAsync(usuarioId, activos, ct));
        }
        catch (NotFoundException ex) { return Traducir(ex); }
    }

    [HttpGet("/api/libros/{libroId:int}/prestamos")]
    public async Task<IActionResult> HistorialLibro(int libroId, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.HistorialPorLibroAsync(libroId, ct));
        }
        catch (NotFoundException ex) { return Traducir(ex); }
    }

    [ApiKey]
    [HttpGet("activos/{matricula}")]
    [AllowAnonymous]
    //no lleva try/catch porque el servicio no lanza excepciones de negocio en este caso
    public async Task<IActionResult> ActivosPorMatricula(string matricula, CancellationToken ct) => Ok(await _service.ListarActivosPorMatriculaAsync(matricula, ct));

    private ObjectResult Traducir(Exception ex) => ex switch
    {
        NotFoundException         => Problem(ex.Message, statusCode: StatusCodes.Status404NotFound),
        ReglaNegocioException     => Problem(ex.Message, statusCode: StatusCodes.Status409Conflict),
        NoElegibleException       => Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden),
        _                         => Problem("Error interno.", statusCode: StatusCodes.Status500InternalServerError)
    };
}