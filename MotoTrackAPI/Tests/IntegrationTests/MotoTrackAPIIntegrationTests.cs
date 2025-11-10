using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MotoTrackAPI.Data;
using MotoTrackAPI.DTOs;
using MotoTrackAPI.Models;
using Xunit;

namespace MotoTrackAPI.Tests.IntegrationTests;

public class MotoTrackAPIIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MotoTrackAPIIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove o DbContext existente
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Adiciona DbContext usando InMemory para testes
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                });

                // Build the service provider
                var sp = services.BuildServiceProvider();

                // Create a scope para obter o contexto
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AppDbContext>();

                // Garante que o banco está criado
                db.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_DeveRetornarOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Auth_FluxoCompletoRegistroELogin_DeveSerBemSucedido()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Username = "integrationtest",
            Email = "integration@test.com",
            Password = "Test@123456",
            Nome = "Integration Test User",
            Role = "Usuario"
        };

        // Act - Registro
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert - Registro
        Assert.True(registerResponse.IsSuccessStatusCode);

        // Act - Login
        var loginRequest = new LoginRequest
        {
            Username = "integrationtest",
            Password = "Test@123456"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert - Login
        Assert.True(loginResponse.IsSuccessStatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.NotNull(loginResult);
        Assert.True(loginResult.Success);
        Assert.NotNull(loginResult.Data.Token);
    }

    [Fact]
    public async Task Motos_CRUDCompleto_DeveSerBemSucedido()
    {
        // Arrange - Primeiro fazer login para obter token
        var token = await ObterTokenAutenticacao();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateMotoRequest
        {
            Placa = "INT1234",
            Modelo = "CB 650R",
            Fabricante = "Honda",
            Ano = 2024
        };

        // Act - Create
        var createResponse = await _client.PostAsJsonAsync("/api/v1/motos", createRequest);
        Assert.True(createResponse.IsSuccessStatusCode);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<MotoDto>>();
        var motoId = createResult.Data.Id;

        // Act - Read (GetById)
        var getResponse = await _client.GetAsync($"/api/v1/motos/{motoId}");
        Assert.True(getResponse.IsSuccessStatusCode);
        var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<MotoDto>>();
        Assert.Equal("INT1234", getResult.Data.Placa);

        // Act - Update
        var updateRequest = new UpdateMotoRequest
        {
            Status = "Em Manutenção"
        };
        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/motos/{motoId}", updateRequest);
        Assert.True(updateResponse.IsSuccessStatusCode);

        // Act - Read After Update
        var getAfterUpdateResponse = await _client.GetAsync($"/api/v1/motos/{motoId}");
        var getAfterUpdateResult = await getAfterUpdateResponse.Content
            .ReadFromJsonAsync<ApiResponse<MotoDto>>();
        Assert.Equal("Em Manutenção", getAfterUpdateResult.Data.Status);

        // Act - Delete
        var deleteResponse = await _client.DeleteAsync($"/api/v1/motos/{motoId}");
        Assert.True(deleteResponse.IsSuccessStatusCode);

        // Act - Verify Deletion
        var getAfterDeleteResponse = await _client.GetAsync($"/api/v1/motos/{motoId}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task Motos_GetAll_DeveFuncionarComPaginacao()
    {
        // Arrange
        var token = await ObterTokenAutenticacao();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar várias motos
        for (int i = 0; i < 15; i++)
        {
            var request = new CreateMotoRequest
            {
                Placa = $"TST{i:D4}",
                Modelo = $"Modelo {i}",
                Fabricante = "Honda",
                Ano = 2024
            };
            await _client.PostAsJsonAsync("/api/v1/motos", request);
        }

        // Act - Primeira página
        var response1 = await _client.GetAsync("/api/v1/motos?page=1&pageSize=10");
        Assert.True(response1.IsSuccessStatusCode);
        var result1 = await response1.Content
            .ReadFromJsonAsync<ApiResponse<PagedResponse<MotoDto>>>();
        Assert.Equal(10, result1.Data.Items.Count);
        Assert.Equal(15, result1.Data.TotalItems);

        // Act - Segunda página
        var response2 = await _client.GetAsync("/api/v1/motos?page=2&pageSize=10");
        Assert.True(response2.IsSuccessStatusCode);
        var result2 = await response2.Content
            .ReadFromJsonAsync<ApiResponse<PagedResponse<MotoDto>>>();
        Assert.Equal(5, result2.Data.Items.Count);
    }

    [Fact]
    public async Task Motos_GetByStatus_DeveFiltrarCorretamente()
    {
        // Arrange
        var token = await ObterTokenAutenticacao();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar motos com diferentes status
        await _client.PostAsJsonAsync("/api/v1/motos", new CreateMotoRequest
        {
            Placa = "DSP0001",
            Modelo = "CB 500",
            Fabricante = "Honda",
            Ano = 2024
        });

        var moto2Response = await _client.PostAsJsonAsync("/api/v1/motos", new CreateMotoRequest
        {
            Placa = "MNT0001",
            Modelo = "MT-07",
            Fabricante = "Yamaha",
            Ano = 2024
        });
        var moto2 = await moto2Response.Content.ReadFromJsonAsync<ApiResponse<MotoDto>>();
        
        // Alterar status da segunda moto
        await _client.PutAsJsonAsync($"/api/v1/motos/{moto2.Data.Id}", new UpdateMotoRequest
        {
            Status = "Em Manutenção"
        });

        // Act
        var response = await _client.GetAsync("/api/v1/motos/status/Disponível");
        
        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<MotoDto>>>();
        Assert.NotEmpty(result.Data);
        Assert.All(result.Data, m => Assert.Equal("Disponível", m.Status));
    }

    [Fact]
    public async Task Localizacoes_CriarEConsultar_DeveSerBemSucedido()
    {
        // Arrange
        var token = await ObterTokenAutenticacao();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Criar uma moto primeiro
        var motoResponse = await _client.PostAsJsonAsync("/api/v1/motos", new CreateMotoRequest
        {
            Placa = "LOC0001",
            Modelo = "CB 500",
            Fabricante = "Honda",
            Ano = 2024
        });
        var moto = await motoResponse.Content.ReadFromJsonAsync<ApiResponse<MotoDto>>();

        // Act - Criar localização
        var locRequest = new CreateLocalizacaoRequest
        {
            MotoId = moto.Data.Id,
            Latitude = -23.550520,
            Longitude = -46.633308
        };
        var locResponse = await _client.PostAsJsonAsync("/api/v1/localizacoes", locRequest);

        // Assert
        Assert.True(locResponse.IsSuccessStatusCode);
        var locResult = await locResponse.Content.ReadFromJsonAsync<ApiResponse<LocalizacaoDto>>();
        Assert.Equal(moto.Data.Id, locResult.Data.MotoId);

        // Act - Consultar última localização
        var ultimaLocResponse = await _client.GetAsync($"/api/v1/localizacoes/ultima/{moto.Data.Id}");
        Assert.True(ultimaLocResponse.IsSuccessStatusCode);
        var ultimaLoc = await ultimaLocResponse.Content
            .ReadFromJsonAsync<ApiResponse<LocalizacaoDto>>();
        Assert.Equal(-23.550520, ultimaLoc.Data.Latitude, 5);
    }

    [Fact]
    public async Task Predicao_DeveRetornarPrevisaoManutencao()
    {
        // Arrange
        var token = await ObterTokenAutenticacao();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var predicaoRequest = new PredicaoManutencaoRequest
        {
            MotoId = 1,
            Quilometragem = 5000,
            NivelBateria = 85,
            DiasDesdeUltimaManutencao = 90
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/predicao", predicaoRequest);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<PredicaoManutencaoResponse>>();
        Assert.NotNull(result.Data);
        Assert.InRange(result.Data.ProbabilidadeManutencao, 0, 1);
        Assert.NotNull(result.Data.Recomendacao);
    }

    [Fact]
    public async Task Auth_LoginComCredenciaisInvalidas_DeveRetornarUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "usuarioinexistente",
            Password = "senhaerrada"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Motos_AcessoSemAutenticacao_DeveRetornarUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/motos");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Motos_CriarComPlacaDuplicada_DeveRetornarBadRequest()
    {
        // Arrange
        var token = await ObterTokenAutenticacao();
        _client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new CreateMotoRequest
        {
            Placa = "DUP1234",
            Modelo = "CB 500",
            Fabricante = "Honda",
            Ano = 2024
        };

        // Act - Primeira criação
        var response1 = await _client.PostAsJsonAsync("/api/v1/motos", request);
        Assert.True(response1.IsSuccessStatusCode);

        // Act - Segunda criação com mesma placa
        var response2 = await _client.PostAsJsonAsync("/api/v1/motos", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
    }

    private async Task<string> ObterTokenAutenticacao()
    {
        var registerRequest = new RegisterRequest
        {
            Username = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            Password = "Test@123456",
            Nome = "Test User",
            Role = "Admin"
        };

        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Username = registerRequest.Username,
            Password = registerRequest.Password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        
        return loginResult.Data.Token;
    }
}
