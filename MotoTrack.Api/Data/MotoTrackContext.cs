using Microsoft.EntityFrameworkCore;
using MotoTrack.Api.Models;

namespace MotoTrack.Api.Data;

public class MotoTrackContext(DbContextOptions<MotoTrackContext> options) : DbContext(options)
{
    public DbSet<Moto> Motos => Set<Moto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<OrdemServico> OrdensServico => Set<OrdemServico>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Moto>().Property(p => p.Placa).IsRequired();
        modelBuilder.Entity<Cliente>().Property(p => p.Email).IsRequired(false);
        base.OnModelCreating(modelBuilder);
    }
}