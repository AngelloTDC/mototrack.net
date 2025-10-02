namespace MotoTrack.Api.Models;

public class Moto
{
    public int Id { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Ano { get; set; }
    public bool Ativa { get; set; } = true;

    // relacionamento
    public List<OrdemServico> Ordens { get; set; } = new();
}