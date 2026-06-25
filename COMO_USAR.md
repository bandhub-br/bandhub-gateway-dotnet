# Como usar o BandHub.Gateway

O Gateway é o **único endereço** que o cliente precisa conhecer. Todas as requisições passam por ele na porta `5000`.

---

## Pré-requisitos

Todos os serviços devem estar rodando localmente:

| Serviço | Porta |
|---------|-------|
| BandHub.Gateway | 5000 |
| BandHub.AuthService | 5290 |
| BandHub.UserService | 5293 |
| BandHub.BandService | 5081 |
| BandHub.Bff | 5223 |

```bash
# Inicie cada serviço em um terminal separado
dotnet run --project BandHub.Gateway
dotnet run --project BandHub.AuthService
dotnet run --project BandHub.UserService
dotnet run --project BandHub.BandService
dotnet run --project BandHub.Bff
```

---

## Mapa de rotas

```
http://localhost:5000
  ├── /auth/**       → AuthService  (5290)
  ├── /accounts/**   → UserService  (5293)
  ├── /bands/**      → BandService  (5081)
  └── /bff/**        → BFF          (5223)
```

---

## Passo a passo: fluxo completo

### 1. Registrar uma conta

```http
POST http://localhost:5000/accounts/register
Content-Type: application/json

{
  "name": "João Silva",
  "email": "joao@email.com",
  "password": "Senha@123",
  "accountType": 1
}
```

> `accountType`: `1` = Usuário, `2` = Banda

**Resposta esperada (`201 Created`):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "João Silva",
  "email": "joao@email.com",
  "accountType": "User"
}
```

---

### 2. Fazer login e obter o token JWT

```http
POST http://localhost:5000/auth/login
Content-Type: application/json

{
  "email": "joao@email.com",
  "password": "Senha@123"
}
```

**Resposta esperada (`200 OK`):**
```json
{
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "João Silva",
  "email": "joao@email.com",
  "accountType": "User",
  "acessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "acessTokenExpiraEm": "2026-06-25T21:00:00Z",
  "refreshToken": "abc123...",
  "refreshTokenExpiraEm": "2026-07-02T15:00:00Z"
}
```

> Guarde o `acessToken` — ele será necessário para acessar os endpoints protegidos.

---

### 3. Usar o token nas requisições protegidas

Adicione o header `Authorization` com o valor `Bearer <acessToken>` em todas as requisições que exigem autenticação.

#### Criar uma banda

```http
POST http://localhost:5000/bands
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "name": "The Rolling Stones",
  "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

#### Listar bandas

```http
GET http://localhost:5000/bands
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Listar contas

```http
GET http://localhost:5000/accounts
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

### 4. Renovar o token (refresh)

Quando o `acessToken` expirar, use o `refreshToken` para obter um novo par de tokens sem precisar fazer login novamente.

```http
POST http://localhost:5000/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "abc123..."
}
```

**Resposta esperada (`200 OK`):**
```json
{
  "acessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "acessTokenExpiraEm": "2026-06-25T22:00:00Z",
  "refreshToken": "xyz789...",
  "refreshTokenExpiraEm": "2026-07-02T16:00:00Z"
}
```

---

### 5. Operações agregadas via BFF

Para operações que envolvem múltiplos serviços (como registrar uma banda e vincular a uma conta), use as rotas `/bff/**`:

#### Registrar conta do tipo Banda

```http
POST http://localhost:5000/bff/accounts/register-band
Content-Type: application/json

{
  "name": "The Beatles",
  "email": "beatles@email.com",
  "password": "Senha@123"
}
```

#### Registrar conta do tipo Usuário (via BFF)

```http
POST http://localhost:5000/bff/accounts/register-user
Content-Type: application/json

{
  "name": "Ringo Starr",
  "email": "ringo@email.com",
  "password": "Senha@123"
}
```

---

## Resumo de todos os endpoints via Gateway

| Método | Rota via Gateway | Serviço destino | Auth? |
|--------|------------------|-----------------|-------|
| `POST` | `/auth/login` | AuthService | Não |
| `POST` | `/auth/refresh-token` | AuthService | Não |
| `POST` | `/accounts/register` | UserService | Não |
| `GET` | `/accounts` | UserService | Sim |
| `POST` | `/bands` | BandService | Sim |
| `GET` | `/bands` | BandService | Sim |
| `POST` | `/bff/accounts/register-band` | BFF | Não |
| `POST` | `/bff/accounts/register-user` | BFF | Não |
| `POST` | `/bff/accounts/login` | BFF → AuthService | Não |

---

## Erros comuns

### `401 Unauthorized`
O token JWT está ausente, expirado ou inválido. Faça login novamente via `POST /auth/login` para obter um novo token.

### `429 Too Many Requests`
O endpoint `/auth/login` tem rate limiting de **5 requisições por minuto**. Aguarde antes de tentar novamente.

### `502 Bad Gateway`
O serviço destino não está no ar. Verifique se todos os microsserviços estão rodando nas portas corretas.

---

## Testando com .http (VS Code)

Crie um arquivo `gateway.http` com o conteúdo abaixo e use a extensão **REST Client** no VS Code:

```http
### Login
POST http://localhost:5000/auth/login
Content-Type: application/json

{
  "email": "joao@email.com",
  "password": "Senha@123"
}

### Listar bandas (substitua <TOKEN> pelo acessToken do login)
GET http://localhost:5000/bands
Authorization: Bearer <TOKEN>

### Criar banda
POST http://localhost:5000/bands
Authorization: Bearer <TOKEN>
Content-Type: application/json

{
  "name": "Metallica",
  "accountId": "<ACCOUNT_ID>"
}
```
