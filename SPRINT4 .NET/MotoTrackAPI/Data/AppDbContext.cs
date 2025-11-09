using Microsoft.EntityFrameworkCore;
using MotoTrackAPI.Models;

namespace MotoTrackAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Moto> Motos { get; set; } = null!;
    public DbSet<Beacon> Beacons { get; set; } = null!;
    public DbSet<Localizacao> Localizacoes { get; set; } = null!;
    public DbSet<Usuario> Usuarios { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Localizacao>()
            .Property(l => l.Latitude)
            .HasPrecision(10, 7);

        modelBuilder.Entity<Localizacao>()
            .Property(l => l.Longitude)
            .HasPrecision(10, 7);

        modelBuilder.Entity<Moto>()
            .HasIndex(m => m.Placa)
            .IsUnique();

        modelBuilder.Entity<Beacon>()
            .HasIndex(b => b.MacAddress)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Username)
            .IsUnique();

        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().HasData(
            new Usuario
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "admin123",
                Role = "Admin",
                DataCriacao = DateTime.Now
            },
            new Usuario
            {
                Id = 2,
                Username = "operador",
                PasswordHash = "operador123",
                Role = "Operador",
                DataCriacao = DateTime.Now
            }
        );

        modelBuilder.Entity<Beacon>().HasData(
            new Beacon { Id = 1, MacAddress = "AA:BB:CC:DD:EE:01", Identificador = "BEACON-001", TipoSensor = "Bluetooth", NivelBateria = 95 },
            new Beacon { Id = 2, MacAddress = "AA:BB:CC:DD:EE:02", Identificador = "BEACON-002", TipoSensor = "Bluetooth", NivelBateria = 88 },
            new Beacon { Id = 3, MacAddress = "AA:BB:CC:DD:EE:03", Identificador = "BEACON-003", TipoSensor = "RFID", NivelBateria = 100 }
        );

        modelBuilder.Entity<Moto>().HasData(
            new Moto { Id = 1, Placa = "ABC1234", Modelo = "Honda CG 160", Fabricante = "Honda", Ano = 2023, Status = "Disponível", BeaconId = 1 },
            new Moto { Id = 2, Placa = "XYZ5678", Modelo = "Yamaha Factor 150", Fabricante = "Yamaha", Ano = 2023, Status = "Disponível", BeaconId = 2 },
            new Moto { Id = 3, Placa = "DEF9012", Modelo = "Suzuki Intruder 150", Fabricante = "Suzuki", Ano = 2022, Status = "Manutenção", BeaconId = 3 }
        );

        modelBuilder.Entity<Localizacao>().HasData(
            new Localizacao { Id = 1, MotoId = 1, Latitude = -23.5505m, Longitude = -46.6333m, SetorDeposito = "Setor A - Corredor 1", BeaconId = 1 },
            new Localizacao { Id = 2, MotoId = 2, Latitude = -23.5515m, Longitude = -46.6343m, SetorDeposito = "Setor A - Corredor 2", BeaconId = 2 },
            new Localizacao { Id = 3, MotoId = 3, Latitude = -23.5525m, Longitude = -46.6353m, SetorDeposito = "Setor B - Área de Manutenção", BeaconId = 3 }
        );
    }
}
