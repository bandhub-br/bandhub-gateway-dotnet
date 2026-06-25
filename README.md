# BandHub.Gateway

API Gateway do ecossistema BandHub. Ponto de entrada único que roteia requisições para os microsserviços corretos usando **YARP (Yet Another Reverse Proxy)**.

---

## Sobre o Serviço

O Gateway é o único endereço que o cliente (frontend, app mobile, etc.) precisa conhecer. Ele recebe a requisição, identifica o serviço destino pela rota e faz o proxy transparente, encaminhando headers — incluindo o JWT — sem modificação.

---

## Roteamento

| Prefixo de rota       | Serviço destino          | Porta destino |
|-----------------------|--------------------------|---------------|
| `/auth/**`            | BandHub.AuthService      | 5290          |
| `/accounts/**`        | BandHub.UserService      | 5293          |
| `/bands/**`           | BandHub.BandService      | 5081          |
| `/bff/**`             | BandHub.Bff              | 5223          |

### Exemplos

```
POST http://localhost:5000/auth/login           → AuthService
POST http://localhost:5000/auth/refresh-token  → AuthService
POST http://localhost:5000/accounts/register   → UserService
GET  http://localhost:5000/accounts            → UserService
POST http://localhost:5000/bands               → BandService
GET  http://localhost:5000/bands               → BandService
POST http://localhost:5000/bff/accounts/register-band → BFF
```

---

## Tecnologias

| Tecnologia              | Versão  |
|-------------------------|---------|
| .NET                    | 8.0     |
| YARP (ReverseProxy)     | 2.2.0   |

---

## Estrutura

```
BandHub.Gateway/
├── BandHub.Gateway/
│   ├── Program.cs                     # Configuração do YARP
│   ├── appsettings.json               # Rotas e clusters YARP
│   ├── appsettings.Development.json
│   └── Properties/
│       └── launchSettings.json        # Porta 5000
├── tests/
│   └── BandHub.Gateway.UnitTests/
│       └── RoutingConfigTests.cs      # Testes de mapeamento de rotas
├── BandHub.Gateway.sln
├── global.json
└── README.md
```

---

## Porta

| Protocolo | Porta |
|-----------|-------|
| HTTP      | 5000  |

---

## Como executar

```bash
dotnet restore
dotnet build
dotnet run --project BandHub.Gateway
```

### Testes

```bash
dotnet test
```

---

## Configuração das rotas (appsettings.json)

As rotas e clusters são configurados diretamente no `appsettings.json` via seção `ReverseProxy`. Para alterar o endereço de um serviço (ex.: em produção), basta atualizar a propriedade `Address` do cluster correspondente.

```json
"ReverseProxy": {
  "Clusters": {
    "auth-cluster": {
      "Destinations": {
        "destination1": { "Address": "http://localhost:5290" }
      }
    }
  }
}
```

---

## Arquitetura

```
Cliente
  │
  ▼
BandHub.Gateway (porta 5000)  ← único ponto de entrada
  ├──► /auth/**       → BandHub.AuthService  (5290)
  ├──► /accounts/**   → BandHub.UserService  (5293)
  ├──► /bands/**      → BandHub.BandService  (5081)
  └──► /bff/**        → BandHub.Bff          (5223)
```

---

## Observações

- A autenticação JWT é validada pelos próprios microsserviços. O Gateway encaminha o header `Authorization` sem alteração.
- Para adicionar uma nova rota, basta adicionar uma entrada em `Routes` e `Clusters` no `appsettings.json`, sem necessidade de alterar código.
