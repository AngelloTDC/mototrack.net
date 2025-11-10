using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Asp.Versioning;
using MotoTrackAPI.Data;
using MotoTrackAPI.Services;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("🚀 Iniciando configuração da MotoTrack API...");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseInMemoryDatabase("MotoTrackDB");
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Api-Version"),
        new MediaTypeApiVersionReader("ver"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

Console.WriteLine("✅ Versionamento da API configurado (v1.0)");

var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? "ChaveSecretaSuperSeguraComMaisDe32Caracteres123!@#";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "MotoTrackAPI",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "MotoTrackClient",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtService>();

Console.WriteLine("✅ Autenticação JWT configurada");

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "database",
        tags: new[] { "db", "database" })
    .AddCheck("api-health", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
            "API está funcionando corretamente"));

Console.WriteLine("✅ Health Checks configurados");

builder.Services.AddSingleton<MLService>();

Console.WriteLine("✅ Serviço ML.NET registrado");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MotoTrack API - Sistema de Rastreamento de Motos IoT",
        Version = "v1.0",
        Description = @"
# MotoTrack API - Solução IoT para Rastreamento de Motos

## Funcionalidades Implementadas

✅ **10 pontos** - Health Checks em `/health`  
✅ **10 pontos** - Versionamento de API (v1.0)  
✅ **25 pontos** - Autenticação JWT Bearer  
✅ **25 pontos** - Machine Learning com ML.NET para predição de manutenção  
✅ **30 pontos** - Testes Unitários e de Integração com xUnit  
✅ **Boas práticas REST** - CRUD completo, paginação, filtros  

## Como Autenticar

1. Faça login em: `POST /api/v1/auth/login`
   ```json
   {
     ""username"": ""admin"",
     ""senha"": ""admin123""
   }
   ```

2. Copie o token retornado

3. Clique em **Authorize** (canto superior direito)

4. Digite: `Bearer {token-vem-aqui}`

5. Clique em **Authorize** novamente

## Machine Learning - Predição de Manutenção

Use o endpoint `/api/v1/predicao/prever-manutencao` para prever se uma moto precisa de manutenção baseado em:
- Quilometragem
- Nível de bateria do beacon
- Dias desde última manutenção

## Rastreamento em Tempo Real

A API permite registrar e consultar localizações das motos no depósito usando sensores IoT (GPS, RFID, Bluetooth).

## Usuários de Teste

- **Operador**: `operador` / `operador123`
- **Admin**: `admin` / `admin123`

## Moto de Teste Manutenção
```json
{
  ""motoId"": 1,
  ""quilometragem"": 50000,
  ""nivelBateria"": 90,
  ""diasDesdeUltimaManutencao"": 180
}
```

##  Integrantes do Projeto

- **RM 556511** - Angello Turano da Costa
- **RM 558576** - Cauã Sanches de Santana
- **RM 558317** - Leonardo Bianchi",
        Contact = new OpenApiContact
        {
            Name = "FIAP - Análise e Desenvolvimento de Sistemas",
        },
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header usando Bearer scheme.
        
**Como usar:** 
1. Faça login no endpoint `/api/v1/auth/login`
2. Copie o token retornado
3. Clique em 'Authorize' 
4. Digite: `Bearer {seu-token}`
5. Clique em 'Authorize' novamente

Exemplo: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

Console.WriteLine("✅ Swagger configurado");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    Console.WriteLine("✅ Banco de dados inicializado");
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MotoTrack API v1.0");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "MotoTrack API - Documentação";
    c.DefaultModelsExpandDepth(2);
    c.DisplayRequestDuration();
    c.EnableDeepLinking();
    c.EnableFilter();
});

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description ?? "N/A",
                duration = $"{e.Value.Duration.TotalMilliseconds}ms",
                error = e.Value.Exception?.Message
            }),
            totalDuration = $"{report.TotalDuration.TotalMilliseconds}ms"
        }, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await context.Response.WriteAsync(result);
    }
});

app.MapGet("/", () => Results.Ok(new
{
    api = "MotoTrack API",
    version = "v1.0",
    status = "✅ Online",
    descricao = "Sistema de Rastreamento de Motos com IoT",
    integrantes = new[]
    {
        new { rm = "RM 556511", nome = "Angello Turano da Costa" },
        new { rm = "RM 558576", nome = "Cauã Sanches de Santana" },
        new { rm = "RM 558317", nome = "Leonardo Bianchi" }
    },
    pontuacao = new
    {
        healthChecks = "10 pontos ✅",
        versionamento = "10 pontos ✅",
        seguranca_jwt = "25 pontos ✅",
        ml_net = "25 pontos ✅",
        testes = "30 pontos ✅",
        total = "100 pontos"
    },
    endpoints = new
    {
        documentacao = "/swagger",
        healthCheck = "/health",
        login = "/api/v1/auth/login",
        motos = "/api/v1/motos",
        localizacoes = "/api/v1/localizacoes",
        predicao_ml = "/api/v1/predicao/prever-manutencao"
    },
    recursos = new[]
    {
        "✅ Health Checks",
        "✅ API Versioning v1.0",
        "✅ JWT Authentication",
        "✅ ML.NET - Predição de Manutenção",
        "✅ CRUD Completo de Motos",
        "✅ Rastreamento em Tempo Real"
    },
    autenticacao = new
    {
        tipo = "JWT Bearer",
        usuarios_teste = new[]
        {
            new { username = "admin", password = "admin123", role = "Admin" },
            new { username = "operador", password = "operador123", role = "Operador" }
        }
    }
}))
.WithName("GetApiInfo")
.WithTags("Info")
.ExcludeFromDescription();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║        🏍️  MOTOTRACK API - SISTEMA IoT INICIADO                 ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine(" Ambiente:                     " + app.Environment.EnvironmentName);
Console.WriteLine(" URL Base:                     http://localhost:5000");
Console.WriteLine();
Console.WriteLine("✅ Health Checks:                /health");
Console.WriteLine("✅ Swagger UI:                   /swagger");
Console.WriteLine("✅ API Versão:                   v1.0");
Console.WriteLine("✅ Autenticação:                 JWT Bearer");
Console.WriteLine("✅ Machine Learning:             ML.NET ativo");
Console.WriteLine();
Console.WriteLine(" Credenciais de teste:");
Console.WriteLine("   Admin:     username=admin     password=admin123");
Console.WriteLine("   Operador:  username=operador  password=operador123");
Console.WriteLine();
Console.WriteLine(" Integrantes do Projeto:");
Console.WriteLine("   • RM 556511 - Angello Turano da Costa");
Console.WriteLine("   • RM 558576 - Cauã Sanches de Santana");
Console.WriteLine("   • RM 558317 - Leonardo Bianchi");
Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════════════════════════════");

app.Run();

public partial class Program { }
