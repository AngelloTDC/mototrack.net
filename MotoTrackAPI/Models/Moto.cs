using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MotoTrackAPI.Models;

public class Moto
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(8)]
    public string Placa { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Modelo { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Fabricante { get; set; } = string.Empty;

    [Range(1900, 2100)]
    public int Ano { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Disponível";

    public int? BeaconId { get; set; }

    [ForeignKey("BeaconId")]
    public Beacon? Beacon { get; set; }

    public DateTime DataCadastro { get; set; } = DateTime.Now;
}
