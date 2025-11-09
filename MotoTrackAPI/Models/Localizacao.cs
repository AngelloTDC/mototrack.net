using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MotoTrackAPI.Models;

public class Localizacao
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MotoId { get; set; }

    [ForeignKey("MotoId")]
    public Moto Moto { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(10,7)")]
    public decimal Latitude { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,7)")]
    public decimal Longitude { get; set; }

    [StringLength(100)]
    public string? SetorDeposito { get; set; }

    public int? BeaconId { get; set; }

    [StringLength(20)]
    public string TipoLeitura { get; set; } = "Automática";

    public DateTime DataHoraRegistro { get; set; } = DateTime.Now;
}
