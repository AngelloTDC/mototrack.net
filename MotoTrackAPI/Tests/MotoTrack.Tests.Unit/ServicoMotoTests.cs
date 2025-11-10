using Xunit;
using Moq;
using MotoTrackAPI.Services;
using MotoTrackAPI.Repositories;
using MotoTrackAPI.Models;
using System.Threading.Tasks;

namespace MotoTrack.Tests.Unit
{
    public class ServicoMotoTests
    {
        private readonly Mock<IMotoRepository> _repoMock;
        private readonly MotoService _service;

        public ServicoMotoTests()
        {
            _repoMock = new Mock<IMotoRepository>();
            _service = new MotoService(_repoMock.Object);
        }

        [Fact]
        public async Task AdicionarMoto_QuandoModeloValido_DeveChamarRepositorioAdicionar()
        {
            var moto = new Moto { Id = 0, Modelo = "Yamaha XJ6", Ano = 2020 };
            _repoMock.Setup(r => r.AdicionarAsync(moto))
                     .ReturnsAsync(new Moto { Id = 1, Modelo = "Yamaha XJ6", Ano = 2020 });

            var resultado = await _service.AdicionarMotoAsync(moto);

            _repoMock.Verify(r => r.AdicionarAsync(moto), Times.Once);
            Assert.Equal(1, resultado.Id);
            Assert.Equal("Yamaha XJ6", resultado.Modelo);
        }

        [Fact]
        public async Task AdicionarMoto_QuandoAnoMenorQue1900_DeveLancarArgumentException()
        {
            var moto = new Moto { Id = 0, Modelo = "Modelo", Ano = 1800 };

            await Assert.ThrowsAsync<System.ArgumentException>(() => _service.AdicionarMotoAsync(moto));
        }
    }
}
