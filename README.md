# 🐾 Chateau Du Pet: app mobile

App de vagas de emprego do mundo pet, reescrito em **.NET MAUI** a partir do app original em Xamarin.Forms (TCC, 2021).

Este repositório traz o app e a **API** que ele consome. A API usa o mesmo banco PostgreSQL da [versão web em .NET 8](https://github.com/offryan/CHATEAU-DU-PET), então o que é feito no app aparece no portal e vice-versa.

## ✨ O que o app faz

| Perfil | Funcionalidades |
|---|---|
| **Visitante** | Lista e busca vagas (por cargo, empresa ou cidade), vê os detalhes e compartilha |
| **Profissional** | Tudo do visitante + se candidata e cancela candidaturas, acompanha as candidaturas e vê o próprio currículo (experiências e certificados) |
| **Empresa** | Vê as vagas publicadas e os candidatos de cada uma, com atalho para enviar e-mail |

Detalhes que fazem diferença no uso:

- Login salvo com segurança no aparelho (Keystore no Android, Keychain no iOS) até o token vencer
- Puxar para atualizar, estados de carregando/vazio/erro com "tentar de novo"
- Busca enquanto digita, sem chamar a API a cada letra
- Tema claro e escuro, seguindo o aparelho
- A API gratuita do Render "dorme" quando fica parada; o app espera até ela acordar, tenta de novo sozinho e avisa o usuário se demorar

### Contas de demonstração

Senha de todas: `demo1234` (os dados são fictícios e voltam ao original todo dia)

| Perfil | E-mail |
|---|---|
| Profissional | `jucelino@exemplo.com` |
| Empresa | `felipe@exemplo.com` |

## 🔄 Do Xamarin para o .NET MAUI

O app de 2021 era o template do Xamarin.Forms com dados fictícios fixos no código (`MockDataStore`) e não falava com o banco. A nova versão foi escrita do zero:

| | Versão 2021 | Versão atual |
|---|---|---|
| Framework | Xamarin.Forms 5 (suporte encerrado em maio/2024) | .NET MAUI 10 (.NET 10 LTS) |
| Projetos | Um projeto compartilhado + um por plataforma | Projeto único com `Platforms/` |
| Dados | Lista fixa em memória | API REST com dados reais do banco |
| MVVM | `INotifyPropertyChanged` escrito à mão | CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`) |
| Injeção de dependência | `DependencyService` | `Microsoft.Extensions.DependencyInjection` |
| Bindings | Resolvidos em tempo de execução | Compilados (`x:DataType`) e XAML gerado em C# na compilação |
| HTTP | — | `IHttpClientFactory` + Polly (novas tentativas e timeouts) |
| JSON | — | `System.Text.Json` com geração de código (sem reflexão, compatível com trimming) |
| Testes | Nenhum | 84 testes automatizados |
| Android alvo | API 30 | API 36 |

## 🧱 Arquitetura

```
┌──────────────────────┐      HTTPS + JWT      ┌─────────────────────┐      ┌──────────────┐
│  App .NET MAUI       │ ────────────────────▶ │  API ASP.NET Core   │ ───▶ │  PostgreSQL  │
│  (Android / iOS)     │ ◀──────────────────── │  (Minimal API)      │      │  (Supabase)  │
└──────────────────────┘        JSON           └─────────────────────┘      └──────────────┘
                                                                                   ▲
                                                 Portal web .NET 8 (outro repo) ───┘
```

```
src/
├── ChateauDuPet.Contracts   → records trocados entre API e app (um campo renomeado quebra a compilação dos dois lados)
├── ChateauDuPet.Api         → ASP.NET Core 10 Minimal API: JWT, Dapper + Npgsql, OpenAPI + Scalar
├── ChateauDuPet.App.Core    → ViewModels, cliente da API e sessão, sem dependência de MAUI (testável com xUnit comum)
└── ChateauDuPet.App         → .NET MAUI: telas XAML, tema, Shell e implementações de plataforma
tests/
├── ChateauDuPet.Api.Tests       → integração: API em memória contra PostgreSQL real
└── ChateauDuPet.App.Core.Tests  → ViewModels, sessão e cliente HTTP
database/                    → schema.sql e seed.sql (os mesmos do portal web)
```

A lógica do app fica em `App.Core`, que conversa com a plataforma por interfaces pequenas (`INavegacao`, `IPlataforma`, `IArmazenamentoSeguro`). O projeto MAUI só implementa essas interfaces e desenha as telas; os testes usam versões falsas delas.

## 🛠️ Tecnologias

| Camada | Tecnologia |
|---|---|
| App | .NET MAUI 10, CommunityToolkit.Maui, CommunityToolkit.Mvvm, Microsoft.Extensions.Http.Resilience |
| API | ASP.NET Core 10 (Minimal API), autenticação JWT, rate limiting, ProblemDetails, OpenAPI + Scalar |
| Dados | Dapper + Npgsql 10, PostgreSQL no Supabase |
| Testes | xUnit v3 (Microsoft.Testing.Platform), WebApplicationFactory |
| Infra | Docker no Render (`render.yaml`), GitHub Actions gerando o APK |

## 📡 API

Documentação interativa em `/scalar` (OpenAPI em `/openapi/v1.json`).

| Método | Rota | Acesso |
|---|---|---|
| `POST` | `/api/auth/login` | público (limitado a 10 tentativas/min por IP) |
| `GET` | `/api/auth/eu` | logado |
| `GET` | `/api/vagas?busca=` | público |
| `GET` | `/api/vagas/{id}` | público (para profissional, informa se já se candidatou) |
| `POST` / `DELETE` | `/api/vagas/{id}/candidatura` | profissional |
| `GET` | `/api/me/candidaturas` | profissional |
| `GET` | `/api/me/perfil` | profissional |
| `GET` | `/api/me/vagas` | empresa |
| `GET` | `/api/me/vagas/{id}/candidatos` | empresa (só das próprias vagas) |

## ▶️ Rodando localmente

Pré-requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download), PostgreSQL e, para o app, o workload do MAUI (`dotnet workload install maui-android`) com Android SDK ou o Visual Studio 2026.

**1. Banco**

```bash
createdb chateau
psql -d chateau -f database/schema.sql
psql -d chateau -f database/seed.sql
```

**2. API** (a string de conexão de desenvolvimento está em `appsettings.Development.json`)

```bash
dotnet run --project src/ChateauDuPet.Api
# http://localhost:5080/scalar
```

**3. App no emulador Android, apontando para a API local** (o emulador enxerga o computador em `10.0.2.2`)

```bash
dotnet build src/ChateauDuPet.App -t:Run -f net10.0-android -p:ChateauApiUrl=http://10.0.2.2:5080
```

Sem o `-p:ChateauApiUrl`, o app usa a API publicada (`https://chateau-du-pet-api.onrender.com`).

**4. Testes** (criam um banco `chateau_api_testes`; servidor configurável pela variável `CHATEAU_TEST_DB`)

```bash
dotnet test --project tests/ChateauDuPet.Api.Tests
dotnet test --project tests/ChateauDuPet.App.Core.Tests
```

## ☁️ Deploy da API (Render)

O `render.yaml` cria o serviço a partir do `Dockerfile`. Variáveis de ambiente:

| Variável | Valor |
|---|---|
| `DATABASE_URL` | String de conexão do Supabase (a mesma do portal web: usuário `chateau_app`, que só lê e grava nas tabelas do Chateau) |
| `Jwt__Chave` | Segredo de assinatura dos tokens, com 32+ caracteres (o Render gera um sozinho) |

A cada push na `main`, o GitHub Actions roda os testes e gera o APK (aba **Actions** → artefato `chateau-du-pet-apk`). Ao criar uma tag `v*`, o APK é anexado a uma release.

## 🔒 Segurança

- Senhas conferidas com **BCrypt**; login que falha não revela se o e-mail existe (nem pelo tempo de resposta)
- Token **JWT** assinado, com validade de 7 dias, guardado no armazenamento seguro do aparelho
- Rotas de profissional e empresa usam sempre o id do token, nunca um id vindo da URL: ninguém vê ou altera dados de outra conta
- SQL sempre parametrizado; curingas da busca (`%`, `_`) são escapados
- Limite de tentativas de login por IP
- No banco, o Row Level Security do Supabase continua bloqueando a API pública

---

Projeto original (2021): app desktop, portal Web Forms e o app Xamarin em [offryan/CHATEAU-DU-PET](https://github.com/offryan/CHATEAU-DU-PET).
