# 🏍️ MotoTrack API - Sistema de Rastreamento de Motos IoT

## 👥 Integrantes do Projeto

- **RM 556511** - Angello Turano da Costa
- **RM 558576** - Cauã Sanches de Santana  
- **RM 558317** - Leonardo Bianchi

## 📋 Descrição do Projeto

Sistema de rastreamento de motos em depósitos utilizando tecnologia IoT (Bluetooth Beacons, RFID, GPS) com API RESTful desenvolvida em .NET 8, Machine Learning para predição de manutenção e autenticação JWT.

## 🎯 Pontuação do Projeto

| Requisito | Pontos | Status |
|-----------|--------|--------|
| Health Checks | 10 | ✅ Implementado |
| Versionamento de API | 10 | ✅ Implementado (v1.0) |
| Segurança JWT | 25 | ✅ Implementado |
| ML.NET - Predição de Manutenção | 25 | ✅ Implementado |
| Testes Unitários e Integração | 30 | ✅ Implementado |
| **TOTAL** | **100** | **✅ Completo** |

## 🚀 Como Executar

### Pré-requisitos

- .NET 8 SDK
- IDE (Visual Studio 2022, VS Code ou Rider)

### Executando o Projeto

```bash
dotnet restore
dotnet run
```

A API estará disponível em: `http://localhost:5000`

## 📖 Documentação da API

Acesse o Swagger em: `http://localhost:5000/swagger`

## 🔐 Autenticação

### Usuários de Teste

- **Admin**: `username: admin` / `password: admin123`
- **Operador**: `username: operador` / `password: operador123`

### Como Autenticar no Swagger

1. Faça login em `POST /api/v1/auth/login`
2. Copie o token retornado
3. Clique em **Authorize** 🔓 (canto superior direito)
4. Digite: `Bearer {seu-token}`
5. Clique em **Authorize** novamente

## 🛠️ Endpoints Principais

### Autenticação
- `POST /api/v1/auth/login` - Fazer login
- `GET /api/v1/auth/me` - Obter dados do usuário autenticado

### Motos
- `GET /api/v1/motos` - Listar motos (com paginação)
- `GET /api/v1/motos/{id}` - Buscar moto por ID
- `POST /api/v1/motos` - Criar nova moto
- `PUT /api/v1/motos/{id}` - Atualizar moto
- `DELETE /api/v1/motos/{id}` - Remover moto
- `GET /api/v1/motos/status/{status}` - Buscar motos por status

### Localizações
- `GET /api/v1/localizacoes` - Listar localizações
- `GET /api/v1/localizacoes/moto/{motoId}/atual` - Localização atual de uma moto
- `GET /api/v1/localizacoes/moto/{motoId}/historico` - Histórico de localizações
- `POST /api/v1/localizacoes` - Registrar nova localização
- `GET /api/v1/localizacoes/setor/{setor}` - Buscar motos por setor
- `GET /api/v1/localizacoes/proximidade` - Buscar motos próximas

### Predição de Manutenção (ML.NET)
- `POST /api/v1/predicao/prever-manutencao` - Prever se moto precisa manutenção
- `POST /api/v1/predicao/prever-manutencao-lote` - Prever manutenção em lote
- `GET /api/v1/predicao/analise-frota` - Análise geral da frota
- `GET /api/v1/predicao/metricas-modelo` - Métricas do modelo ML
- `GET /api/v1/predicao/exemplo` - Exemplo de predição

### Health Check
- `GET /health` - Status da API e banco de dados

## 🤖 Machine Learning - Predição de Manutenção

O sistema utiliza ML.NET com algoritmo FastTree (Boosted Decision Tree) para prever se uma moto precisa de manutenção.

### Características Analisadas

- Quilometragem
- Nível de bateria do beacon
- Dias desde a última manutenção

### Exemplo de Request

```json
{
  "motoId": 1,
  "quilometragem": 8500,
  "nivelBateria": 75,
  "diasDesdeUltimaManutencao": 150
}
```

### Exemplo de Response

```json
{
  "success": true,
  "message": "Predição realizada com sucesso",
  "data": {
    "motoId": 1,
    "placa": "ABC1234",
    "requerManutencao": true,
    "probabilidadeManutencao": 0.85,
    "diasEstimados": 7,
    "recomendacao": "🟡 ATENÇÃO: Agendar manutenção em até 7 dias."
  }
}
```

## 📊 Tecnologias Utilizadas

- .NET 8
- Entity Framework Core 8
- SQL Server / InMemory Database
- ML.NET 3.0
- JWT Bearer Authentication
- Swagger/OpenAPI
- xUnit (Testes)
- Asp.Versioning

## 🏗️ Arquitetura

```
MotoTrackAPI/
├── Controllers/          # Endpoints da API
├── Models/              # Entidades do domínio
├── DTOs/                # Data Transfer Objects
├── Data/                # Contexto do banco de dados
├── Services/            # Serviços (JWT, ML.NET)
└── Program.cs           # Configuração da aplicação
```

## 📝 Licença

MIT License

---

**Desenvolvido por**: Angello Turano, Cauã Sanches e Leonardo Bianchi  
**Instituição**: FIAP - Análise e Desenvolvimento de Sistemas  
**Ano**: 2025
