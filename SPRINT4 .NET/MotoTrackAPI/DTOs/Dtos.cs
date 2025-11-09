using System.ComponentModel.DataAnnotations;

namespace MotoTrackAPI.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Username é obrigatório")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória")]
    public string Senha { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class MotoDto
{
    public int Id { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Fabricante { get; set; } = string.Empty;
    public int Ano { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? BeaconId { get; set; }
    public string? BeaconIdentificador { get; set; }
    public DateTime DataCadastro { get; set; }
}

public class CreateMotoRequest
{
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

    public int? BeaconId { get; set; }
}

public class UpdateMotoRequest
{
    [StringLength(100)]
    public string? Modelo { get; set; }

    [StringLength(50)]
    public string? Fabricante { get; set; }

    [Range(1900, 2100)]
    public int? Ano { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    public int? BeaconId { get; set; }
}

public class LocalizacaoDto
{
    public int Id { get; set; }
    public int MotoId { get; set; }
    public string? MotoPlaca { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? SetorDeposito { get; set; }
    public DateTime DataHoraRegistro { get; set; }
    public string TipoLeitura { get; set; } = string.Empty;
}

public class CreateLocalizacaoRequest
{
    [Required]
    public int MotoId { get; set; }

    [Required]
    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    [Required]
    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    [StringLength(100)]
    public string? SetorDeposito { get; set; }

    public int? BeaconId { get; set; }

    [StringLength(20)]
    public string TipoLeitura { get; set; } = "Automática";
}

public class BeaconDto
{
    public int Id { get; set; }
    public string MacAddress { get; set; } = string.Empty;
    public string Identificador { get; set; } = string.Empty;
    public string TipoSensor { get; set; } = string.Empty;
    public int NivelBateria { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataInstalacao { get; set; }
    public DateTime? UltimaAtualizacao { get; set; }
}

public class CreateBeaconRequest
{
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
}

public class UpdateBeaconRequest
{
    [Range(0, 100)]
    public int? NivelBateria { get; set; }

    public bool? Ativo { get; set; }
}

public class PredicaoManutencaoRequest
{
    [Required]
    public int MotoId { get; set; }

    [Range(0, 500000)]
    public float Quilometragem { get; set; }

    [Range(0, 100)]
    public int NivelBateria { get; set; }

    [Range(0, 365)]
    public int DiasDesdeUltimaManutencao { get; set; }
}

public class PredicaoManutencaoResponse
{
    public int MotoId { get; set; }
    public string Placa { get; set; } = string.Empty;
    public bool RequerManutencao { get; set; }
    public float ProbabilidadeManutencao { get; set; }
    public int DiasEstimados { get; set; }
    public string Recomendacao { get; set; } = string.Empty;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
