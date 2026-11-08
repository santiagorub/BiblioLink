using BiblioLink.Models;
using Microsoft.EntityFrameworkCore;

public class BibliotecaContext : DbContext
{
    public BibliotecaContext(DbContextOptions<BibliotecaContext> options) : base(options) {}
    public DbSet<Libro> Libros {get; set;}
    public DbSet<Usuario> Usuarios {get; set;}
    public DbSet<Prestamo> Prestamos {get; set;}
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BibliotecaContext). Assembly);
    }
}