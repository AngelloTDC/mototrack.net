namespace MotoTrack.Api.Models;

public class OrdemServico
{
    public int Id { get; set; }
    public int MotoId { get; set; }
    public Moto? Moto { get; set; }
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public DateTime DataAbertura { get; set; } = DateTime.UtcNow;
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = "ABERTA"; // ABERTA, EM_ANDAMENTO, FECHADA
}