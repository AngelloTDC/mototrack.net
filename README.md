# MotoTrack API — Sprint 3 (Advanced Business Development with .NET)

API RESTful em **.NET 8 (Web API)** para o domínio **MotoTrack** com 3 entidades principais (**Motos, Clientes, Ordens de Serviço**), seguindo boas práticas REST: **CRUD + paginação + HATEOAS + códigos HTTP adequados**, com **Swagger/OpenAPI** documentado.

---

## 🏗️ Arquitetura e justificativa

- **.NET 8 Web API**: robustez, integração nativa com Swagger e pipeline moderno.
- **EF Core + SQLite**: persistência leve e portável para avaliação (sem necessidade de servidor externo).
- **Camadas simples** (Controllers + Data + Models + DTOs/Utils) para foco em REST, sem over-engineering.
- **Boas práticas REST**: rotas /api/[recurso], status codes corretos, paginação (`?page=&size=`) e **HATEOAS** (links `self/next/prev`).

---

## ▶️ Como executar

```bash
# 1) Restaurar pacotes e compilar
dotnet restore
dotnet build

# 2) Rodar a API (SQLite será criado e populado automaticamente)
dotnet run --project MotoTrack.Api
```

A API sobe em `https://localhost:7063` (ou porta indicada no console).

### Swagger

Acesse: `/swagger`

- Modelos e schemas visíveis
- Exemplos de requisição/resposta via XML comments

---

## 📦 Banco de dados

- **SQLite** no arquivo `mototrack.db`.
- **Seed automático** com algumas motos, clientes e ordens (em `Data/Seed.cs`).

---

## 🔗 Endpoints (exemplos)

### Motos

- `GET /api/motos?page=1&size=5` → 200 OK (retorna `PagedResult<Moto>` com links `self/next/prev`)
- `GET /api/motos/1` → 200 OK | 404
- `POST /api/motos` (body: `{ "placa": "ABC1D23", "modelo": "Honda CG 160", "ano": 2021, "ativa": true }`) → 201 Created + Location
- `PUT /api/motos/1` → 204 No Content | 404
- `DELETE /api/motos/1` → 204 No Content | 404

### Clientes

- `GET /api/clientes?page=1&size=10`
- `GET /api/clientes/1`
- `POST /api/clientes`
- `PUT /api/clientes/1`
- `DELETE /api/clientes/1`

### Ordens de Serviço

- `GET /api/ordens?page=1&size=10` (com `Moto` e `Cliente` embutidos)
- `GET /api/ordens/1`
- `POST /api/ordens` (body: `{ "motoId": 1, "clienteId": 1, "descricao": "Troca de óleo" }`)
- `PATCH /api/ordens/1/status` (body: `"FECHADA"`)

---

## 🧪 Testes

Há um projeto de testes simples com **xUnit** e um teste de fumaça.  
Rodar:

```bash
dotnet test
```

---

## Equipe

- Angello Turano da Costa – RM 558576
- Cauã Sanches de Santana – RM 558317
- Leonardo Bianchi – RM 558576

---
