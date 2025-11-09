using System.ComponentModel.DataAnnotations;

namespace MotoTrackAPI.Models;

public class Beacon
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string MacAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Identificador { get; set; } = string.Empty;

    [StringLength(20)]
    public string TipoSensor { get; set; } = "Bluetooth";

    [Range(0, 100)]
    public int NivelBateria { get; set; } = 100;

    public bool Ativo { get; set; } = true;

    public DateTime DataInstalacao { get; set; } = DateTime.Now;

    public DateTime? UltimaAtualizacao { get; set; }
}
