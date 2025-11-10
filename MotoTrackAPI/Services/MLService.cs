using Microsoft.ML;
using Microsoft.ML.Data;
using MotoTrackAPI.DTOs;

namespace MotoTrackAPI.Services;

public class MLService
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private readonly string _modelPath = "modelo_manutencao.zip";

    public MLService()
    {
        _mlContext = new MLContext(seed: 0);

        if (File.Exists(_modelPath))
        {
            Console.WriteLine("Carregando modelo existente...");
            using var fileStream = new FileStream(_modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _model = _mlContext.Model.Load(fileStream, out _);
            Console.WriteLine("✅ Modelo carregado com sucesso!");
        }
        else
        {
            TreinarModelo();
        }
    }

    private void TreinarModelo()
    {
        Console.WriteLine("Iniciando treinamento do modelo ML.NET...");

        var dadosTreinamento = new List<DadosManutencao>
        {
            new() { Quilometragem = 1000, NivelBateria = 100, DiasDesdeUltimaManutencao = 30, RequerManutencao = false },
            new() { Quilometragem = 5000, NivelBateria = 85, DiasDesdeUltimaManutencao = 90, RequerManutencao = false },
            new() { Quilometragem = 10000, NivelBateria = 70, DiasDesdeUltimaManutencao = 180, RequerManutencao = true },
            new() { Quilometragem = 15000, NivelBateria = 60, DiasDesdeUltimaManutencao = 200, RequerManutencao = true },
            new() { Quilometragem = 3000, NivelBateria = 95, DiasDesdeUltimaManutencao = 45, RequerManutencao = false },
            new() { Quilometragem = 8000, NivelBateria = 75, DiasDesdeUltimaManutencao = 150, RequerManutencao = true },
            new() { Quilometragem = 2000, NivelBateria = 98, DiasDesdeUltimaManutencao = 20, RequerManutencao = false },
            new() { Quilometragem = 12000, NivelBateria = 65, DiasDesdeUltimaManutencao = 190, RequerManutencao = true },
            new() { Quilometragem = 4000, NivelBateria = 90, DiasDesdeUltimaManutencao = 60, RequerManutencao = false },
            new() { Quilometragem = 20000, NivelBateria = 50, DiasDesdeUltimaManutencao = 250, RequerManutencao = true },
        };

        var dadosView = _mlContext.Data.LoadFromEnumerable(dadosTreinamento);

        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(DadosManutencao.Quilometragem),
                nameof(DadosManutencao.NivelBateria),
                nameof(DadosManutencao.DiasDesdeUltimaManutencao))
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: "Label",
                numberOfLeaves: 20,
                numberOfTrees: 100,
                minimumExampleCountPerLeaf: 1));

        _model = pipeline.Fit(dadosView);

        _mlContext.Model.Save(_model, dadosView.Schema, _modelPath);

        Console.WriteLine("✅ Modelo ML.NET treinado e salvo com sucesso!");
    }

    public PredicaoManutencaoResponse PreverManutencao(PredicaoManutencaoRequest request)
    {
        if (_model == null)
            throw new InvalidOperationException("Modelo não foi treinado.");

        var dadosEntrada = new DadosManutencao
        {
            Quilometragem = request.Quilometragem,
            NivelBateria = request.NivelBateria,
            DiasDesdeUltimaManutencao = request.DiasDesdeUltimaManutencao
        };

        var predictionEngine = _mlContext.Model.CreatePredictionEngine<DadosManutencao, PredicaoManutencao>(_model);
        var predicao = predictionEngine.Predict(dadosEntrada);

        int diasEstimados = CalcularDiasEstimados(request.DiasDesdeUltimaManutencao, request.Quilometragem);

        return new PredicaoManutencaoResponse
        {
            MotoId = request.MotoId,
            Placa = string.Empty,
            RequerManutencao = predicao.Prediction,
            ProbabilidadeManutencao = predicao.Probability,
            DiasEstimados = diasEstimados,
            Recomendacao = GerarRecomendacao(predicao.Prediction, predicao.Probability, diasEstimados)
        };
    }

    private int CalcularDiasEstimados(int diasDesdeUltima, float quilometragem)
    {
        if (diasDesdeUltima >= 180 || quilometragem >= 10000)
            return 0;
        if (diasDesdeUltima >= 120 || quilometragem >= 7000)
            return 7;
        if (diasDesdeUltima >= 90 || quilometragem >= 5000)
            return 30;
        return 60;
    }

    private string GerarRecomendacao(bool requerManutencao, float probabilidade, int diasEstimados)
    {
        if (requerManutencao)
        {
            if (diasEstimados == 0)
                return "URGENTE: Manutenção imediata necessária!";
            if (diasEstimados <= 7)
                return $"ATENÇÃO: Agendar manutenção em até {diasEstimados} dias.";
            return $"Manutenção recomendada em {diasEstimados} dias.";
        }

        return $"✅ Moto em boas condições. Próxima revisão em aproximadamente {diasEstimados} dias.";
    }

    public void AvaliarModelo()
    {
        if (_model == null)
        {
            Console.WriteLine("Modelo não foi treinado.");
            return;
        }

        var dadosTeste = new List<DadosManutencao>
        {
            new() { Quilometragem = 6000, NivelBateria = 80, DiasDesdeUltimaManutencao = 100, RequerManutencao = false },
            new() { Quilometragem = 11000, NivelBateria = 65, DiasDesdeUltimaManutencao = 185, RequerManutencao = true },
        };

        var testDataView = _mlContext.Data.LoadFromEnumerable(dadosTeste);
        var predictions = _model.Transform(testDataView);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

        Console.WriteLine($"Acurácia: {metrics.Accuracy:P2}");
        Console.WriteLine($"AUC: {metrics.AreaUnderRocCurve:F3}");
        Console.WriteLine($"F1 Score: {metrics.F1Score:F3}");
    }
}

public class DadosManutencao
{
    [LoadColumn(0)]
    public float Quilometragem { get; set; }

    [LoadColumn(1)]
    public float NivelBateria { get; set; }

    [LoadColumn(2)]
    public float DiasDesdeUltimaManutencao { get; set; }

    [LoadColumn(3), ColumnName("Label")]
    public bool RequerManutencao { get; set; }
}

public class PredicaoManutencao
{
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}