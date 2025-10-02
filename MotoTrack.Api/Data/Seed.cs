using MotoTrack.Api.Models;

namespace MotoTrack.Api.Data;

public static class Seed
{
    public static void Load(MotoTrackContext db)
    {
        if (db.Motos.Any()) return;

        var motos = new List<Moto>{
            new() { Placa="ABC1D23", Modelo="Honda CG 160", Ano=2021 },
            new() { Placa="EFG4H56", Modelo="Yamaha Fazer 250", Ano=2022 },
            new() { Placa="IJK7L89", Modelo="Honda Biz 125", Ano=2020 }
        };

        var clientes = new List<Cliente>{
            new(){ Nome="Leonardo Bianchi", Email="leonardo@example.com", Telefone="11 99999-0000"},
            new(){ Nome="Maria Silva", Email="maria@example.com", Telefone="11 98888-1111"}
        };

        db.Motos.AddRange(motos);
        db.Clientes.AddRange(clientes);
        db.SaveChanges();

        db.OrdensServico.AddRange(new List<OrdemServico>{
            new(){ MotoId=motos[0].Id, ClienteId=clientes[0].Id, Descricao="Troca de óleo", Status="FECHADA" },
            new(){ MotoId=motos[1].Id, ClienteId=clientes[1].Id, Descricao="Revisão geral", Status="EM_ANDAMENTO" }
        });

        db.SaveChanges();
    }
}