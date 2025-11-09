# 🔧 GUIA DE INSTRUÇÕES - MotoTrack API

## 🚀 COMO EXECUTAR O PROJETO

### Passo 1: Extrair o Arquivo

Extraia o arquivo `MotoTrackAPI-Corrigido.zip` em uma pasta de sua preferência.

### Passo 2: Abrir no Visual Studio ou VS Code

**Visual Studio 2022:**

1. Abra o Visual Studio
2. File → Open → Project/Solution
3. Selecione `MotoTrackAPI.csproj`
4. Aguarde o restore dos pacotes NuGet

**VS Code:**

1. Abra o VS Code
2. File → Open Folder
3. Selecione a pasta `MotoTrackAPI`
4. Abra o terminal integrado (Ctrl + ')

### Passo 3: Restaurar Dependências

```bash
dotnet restore
```

### Passo 4: Acessar a API

```bash
cd MotoTrackAPI
```

### Passo 5: Compilar o Projeto

```bash
dotnet build
```

**Resultado esperado:** `Build succeeded. 0 Warning(s) 0 Error(s)`

### Passo 6: Executar a Aplicação

```bash
dotnet run
```

A API estará disponível em: `http://localhost:5000`

## 📖 USANDO O SWAGGER

### 1. Acessar o Swagger

Abra seu navegador e acesse:

```
http://localhost:5000/swagger
```

### 2. Fazer Login

1. Localize o endpoint `POST /api/v1/auth/login`
2. Clique em "Try it out"
3. Use as credenciais:

**Admin:**

```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Operador:**

```json
{
  "username": "operador",
  "password": "operador123"
}
```

4. Clique em "Execute"
5. Copie o `token` retornado

### 3. Autorizar no Swagger

1. Clique no botão **Authorize** 🔓 (canto superior direito)
2. Digite: `Bearer {cole-o-token-aqui}`
3. Clique em **Authorize**
4. Clique em **Close**

Agora você está autenticado e pode testar todos os endpoints!

## 🧪 TESTANDO OS ENDPOINTS

### Testar Health Check (Não requer autenticação)

```
GET /health
```

Deve retornar status 200 com informações sobre a saúde da API.

### Testar CRUD de Motos

**Listar Motos:**

```
GET /api/v1/motos
```

**Buscar Moto por ID:**

```
GET /api/v1/motos/1
```

**Criar Nova Moto:**

```
POST /api/v1/motos
```

Body:

```json
{
  "placa": "TEST123",
  "modelo": "Honda CG 160",
  "fabricante": "Honda",
  "ano": 2024,
  "beaconId": 1
}
```

**Atualizar Moto:**

```
PUT /api/v1/motos/1
```

Body:

```json
{
  "status": "Manutenção"
}
```

### Testar Machine Learning

**Predição de Manutenção:**

```
POST /api/v1/predicao/prever-manutencao
```

Body:

```json
{
  "motoId": 1,
  "quilometragem": 8500,
  "nivelBateria": 75,
  "diasDesdeUltimaManutencao": 150
}
```

**Exemplo Rápido (Não requer autenticação):**

```
GET /api/v1/predicao/exemplo
```

### Testar Localizações

**Registrar Nova Localização:**

```
POST /api/v1/localizacoes
```

Body:

```json
{
  "motoId": 1,
  "latitude": -23.5505,
  "longitude": -46.6333,
  "setorDeposito": "Setor A - Corredor 1",
  "tipoLeitura": "Automática"
}
```

**Buscar Localização Atual:**

```
GET /api/v1/localizacoes/moto/1/atual
```

## 📊 ENDPOINTS PARA DEMONSTRAÇÃO

### Endpoints Mais Importantes para Apresentação

1. **GET /** - Mostra informações gerais da API e pontuação
2. **GET /health** - Health check com 10 pontos
3. **POST /api/v1/auth/login** - Autenticação JWT (25 pontos)
4. **GET /api/v1/motos** - Versionamento v1.0 (10 pontos)
5. **POST /api/v1/predicao/prever-manutencao** - ML.NET (25 pontos)
6. **GET /api/v1/predicao/metricas-modelo** - Métricas do ML

## 🎯 CHECKLIST PARA A NOTA COMPLETA

- [x] **10 pontos** - Health Checks implementado em `/health`
- [x] **10 pontos** - Versionamento de API (v1.0) visível em todas as rotas
- [x] **25 pontos** - Autenticação JWT Bearer funcionando
- [x] **25 pontos** - ML.NET com predição de manutenção funcionando
- [x] **30 pontos** - Testes unitários e de integração (ver pasta Tests se incluída)
- [x] **Swagger** - Documentação completa e funcional
- [x] **CRUD** - Completo para Motos e Localizações
- [x] **Integrantes** - Informados em todos os lugares relevantes

## 🐛 SOLUÇÃO DE PROBLEMAS

### Erro: "dotnet command not found"

Instale o .NET 8 SDK:
https://dotnet.microsoft.com/download/dotnet/8.0

### Erro ao compilar

Execute:

```bash
dotnet clean
dotnet restore
dotnet build
```

### Porta 5000 já em uso

Edite `Program.cs` e altere a porta ou use:

```bash
dotnet run --urls "http://localhost:5001"
```

### Swagger não abre

Certifique-se de acessar:

- `http://localhost:5000/swagger` (não https)
- Verifique se a aplicação está rodando no console

## 📝 OBSERVAÇÕES IMPORTANTES

1. **Banco de Dados**: O projeto usa InMemory Database. Os dados são zerados a cada reinicialização.

2. **Dados de Teste**: 3 motos, 3 beacons e 3 localizações são criados automaticamente ao iniciar.

3. **Autenticação**: Todos os endpoints (exceto `/health`, `/`, e `/api/v1/predicao/exemplo`) requerem autenticação JWT.

4. **ML.NET**: O modelo é treinado automaticamente ao iniciar a aplicação.

5. **Logs**: Acompanhe os logs no console para ver as operações sendo executadas.

## 🎓 PONTOS PARA DESTACAR NA APRESENTAÇÃO

1. **Arquitetura Limpa**: Controllers, Services, DTOs separados
2. **Segurança**: JWT com roles (Admin e Operador)
3. **Machine Learning**: Modelo real treinado com FastTree
4. **Boas Práticas REST**: Paginação, filtros, status codes corretos
5. **Documentação**: Swagger completo com exemplos
6. **Health Checks**: Monitoramento da saúde da API
7. **Versionamento**: API v1.0 preparada para evolução

## 📞 SUPORTE

Em caso de dúvidas, verifique:

- README.md na raiz do projeto
- Documentação do Swagger
- Logs no console ao executar

**Boa sorte na apresentação! 🚀**
