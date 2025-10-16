# SafeScribe API — Autenticação e Autorização com JWT (ASP.NET Core)

API Web construída para a CP5: autenticação com **JWT**, autorização por **Roles** (Leitor, Editor, Admin), **Notas** protegidas, e **Logout com blacklist** de tokens.

---

## 👥 Integrantes

- **Pedro Henrique Martins Dos Reis** — RM555306  
- **Adonay Rodrigues da Rocha** — RM558782  
- **Thamires Ribeiro Cruz** — RM558128

---

## ✅ Tecnologias
- **.NET 8** (ASP.NET Core Web API)
- **JWT Bearer** (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **Entity Framework Core InMemory**
- **BCrypt** para hash de senha

---

## 📦 Requisitos
- SDK **.NET 8.x** instalado (`dotnet --info`)

---

## ▶️ Como executar
```bash
# na pasta do projeto (SafeScribe.Api)
dotnet restore
dotnet build
dotnet run
```
Abra o Swagger em: **`/swagger`** (URL exibida no console).

> O banco é **InMemory**: ao reiniciar a API, os dados são resetados.

---

## ⚙️ Configuração do JWT
Arquivo `appsettings.json`:
```json
{
  "Jwt": {
    "Issuer": "SafeScribe",
    "Audience": "SafeScribe.Api",
    "Secret": "troque-por-uma-chave-bem-grande-32+caracteres"
  }
}
```

> **TokenValidationParameters** (no `Program.cs`):  
> - `ValidateIssuer` / `ValidIssuer` — verifica o emissor esperado (claim `iss`).  
> - `ValidateAudience` / `ValidAudience` — verifica a audiência esperada (claim `aud`).  
> - `ValidateIssuerSigningKey` / `IssuerSigningKey` — valida a **assinatura** do token (integridade/autenticidade).  
> - `ValidateLifetime` — rejeita tokens expirados (claim `exp`) e antes do tempo (claim `nbf`).  
> - `ClockSkew` — tolerância para diferenças de relógio.

---

## 🔐 Perfis (Roles) e Regras
- **Leitor**: pode **ler apenas as próprias notas** (na prática não cria, então não possui notas).
- **Editor**: pode **criar e editar as próprias notas**.
- **Admin**: pode **criar/ler/editar/deletar** qualquer nota.

---

## 🧭 Endpoints

### Auth
- **POST** `/api/v1/auth/registrar` — cria usuário (público)  
  **Body (exemplos):**
  ```json
  { "username": "admin",   "password": "123456", "role": "Admin" }
  ```
  ```json
  { "username": "editorA", "password": "123456", "role": "Editor" }
  ```
  ```json
  { "username": "leitor",  "password": "123456", "role": "Leitor" }
  ```

- **POST** `/api/v1/auth/login` — retorna **JWT** + expiração (público)  
  **Body:**
  ```json
  { "username": "admin", "password": "123456" }
  ```
  **Resposta (exemplo):**
  ```json
  {
    "token": "<JWT>",
    "expiresAt": "2025-10-16T15:59:00Z",
    "userId": "GUID",
    "username": "admin",
    "role": "Admin",
    "jti": "GUID"
  }
  ```

- **POST** `/api/v1/auth/logout` — **revoga o token atual** (autenticado)  
  O `jti` do token é registrado numa **blacklist** em memória até a expiração.  
  Requisições posteriores com o mesmo token retornam **401 Token revogado (logout).**

### Notas (todas **protegidas** por JWT)
> No Swagger, clique em **Authorize**, cole **apenas o token** (sem `Bearer`).

- **POST** `/api/v1/notas` — cria nota (Editor/Admin)  
  ```json
  { "title": "Minha nota", "content": "Conteúdo" }
  ```

- **GET** `/api/v1/notas/{id}` — lê nota:  
  - **Leitor/Editor**: somente a **própria**  
  - **Admin**: qualquer

- **PUT** `/api/v1/notas/{id}` — atualiza nota:  
  - **Editor**: somente a **própria**  
  - **Admin**: qualquer  
  ```json
  { "title": "Atualizado", "content": "Conteúdo atualizado" }
  ```

- **DELETE** `/api/v1/notas/{id}` — apenas **Admin**

---

## 🧪 Roteiro rápido de testes (Swagger)

1. **Registrar** usuários (ex.: `admin`, `editorA`, `leitor`).  
2. **Login** (ex.: `admin`) → copie o `token`.  
3. Clique **Authorize** → cole o token.  
4. **POST /api/v1/notas** (criar) → **201**  
5. **GET /api/v1/notas/{id}** (ver) → **200**  
6. **PUT /api/v1/notas/{id}** (editar) → **204**  
7. **DELETE /api/v1/notas/{id}** (apagar) → **204** (somente Admin)  
8. **POST /api/v1/auth/logout** → retorna `jti` + `expiresAt`  
9. Tente acessar qualquer rota protegida **com o mesmo token** → **401 Token revogado**  
10. Faça login de novo (novo token) para voltar a usar.

---

## 🗂️ Estrutura (resumo)
```
SafeScribe.Api/
  Auth/
    Roles.cs
  Controllers/
    AuthController.cs
    NotasController.cs
  Data/
    AppDbContext.cs
  Dtos/
    LoginRequestDto.cs
    LoginResponseDto.cs
    NoteCreateDto.cs
    NoteUpdateDto.cs
    UserRegisterDto.cs
  Middlewares/
    JwtBlacklistMiddleware.cs
  Models/
    Note.cs
    User.cs
  Services/
    ITokenService.cs
    TokenService.cs
    ITokenBlacklistService.cs
    InMemoryTokenBlacklistService.cs
  Program.cs
  appsettings.json
```

---

## 🛡️ Observações de segurança
- **Nunca** armazene senhas em texto puro — usamos **BCrypt** (`PasswordHash`).  
- Mantenha a **chave JWT** em segredo e com tamanho suficiente (32+ chars em UTF‑8).  
- Em produção, use um **banco persistente** em vez de InMemory e **HTTPS** habilitado.

---

## 📄 Licença
Uso acadêmico — CP5.
