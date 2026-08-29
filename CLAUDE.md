# FaceId — Convenções do Projeto

Plataforma de controle de ponto com reconhecimento facial, anti-fraude e
conformidade com a Portaria MTP 671/2021 (REP-P).

---

## Skills obrigatórias

**Estas skills não são opcionais.** Devem ser invocadas *antes* de escrever
código da stack correspondente, não depois para validar.

### Ao trabalhar com Flutter/Dart

| Skill | Quando |
|-------|--------|
| `flutter-tester` | Qualquer criação, correção ou revisão de teste — unit, widget, integration, Riverpod, Mockito |
| `owasp-mobile-security-checker` | Armazenamento de token, storage local, criptografia, rede, e toda auditoria de segurança |

### Ao trabalhar com .NET/C#

Usar as skills do plugin **`dotnet-claude-kit`**, escolhendo a específica da
tarefa:

| Skill | Quando |
|-------|--------|
| `vertical-slice` | Estrutura de feature, organização de slice |
| `minimal-api` | Endpoints, roteamento, OpenAPI |
| `ef-core` | DbContext, migrations, queries, interceptors |
| `dependency-injection` | Registro de serviços, lifetimes |
| `modern-csharp` | Baseline para todo código C# novo |
| `testing` | xUnit v3, WebApplicationFactory, Testcontainers |
| `error-handling` | Result pattern, ProblemDetails |
| `messaging` | Wolverine, RabbitMQ, outbox |
| `authentication` | JWT, autorização, políticas |
| `security-scan` | Auditoria de segurança |

---

## Arquitetura

```
backend/src/
├── FaceId.Api/             endpoints Minimal API, composição, middleware
├── FaceId.Features/        slices verticais (command, query, handler, endpoint)
├── FaceId.Domain/          entidades, value objects, regras, portas
├── FaceId.Infrastructure/  EF Core, MinIO, RabbitMQ, ONNX, assinatura digital
└── FaceId.Shared/          primitivos sem dependência de domínio
```

- **Vertical Slice Architecture** + CQRS. Cada slice é autocontido; slices não
  referenciam outros slices.
- `Domain` não referencia nenhum projeto. `Infrastructure` implementa as
  interfaces declaradas em `Domain`.
- Endpoints são finos: sem regra de negócio, apenas rota, autorização e delegação.

### Stack

.NET 10 · ASP.NET Core Minimal API · **WolverineFX** (CQRS + RabbitMQ + outbox) ·
EF Core 10 + Npgsql · PostgreSQL 17 · MinIO · RabbitMQ · ONNX Runtime ·
OpenCvSharp4 · Serilog · OpenTelemetry · Flutter + Riverpod + Material 3

---

## Regras invioláveis

### Mapeamento manual

Proibido AutoMapper ou qualquer mapeador automático. Todo mapeamento é explícito.

```csharp
var response = new EmployeeResponse
{
    Id = employee.Id,
    Name = employee.Name,
    Registration = employee.Registration
};
```

Métodos de extensão são permitidos para organizar, desde que o mapeamento
continue explícito.

### Bibliotecas proibidas

| Biblioteca | Motivo | Substituta |
|------------|--------|-----------|
| AutoMapper | Comercial; mapeamento é manual | — |
| MediatR v13+ | Comercial | Wolverine |
| MassTransit v9 | Comercial | Wolverine |
| FluentAssertions v8+ | Comercial | Shouldly |
| SixLabors.ImageSharp v3+ | Split License paga | OpenCvSharp4 |
| InsightFace / ArcFace / SCRFD | Pesos *non-commercial research only* | YuNet + SFace (Apache-2.0) |

Toda nova dependência passa por verificação de licença antes de entrar em
`Directory.Packages.props`.

### Dados biométricos

- Embeddings **cifrados em repouso** (AES-256-GCM)
- **Nunca** expor embedding, `recognitionScore`, `livenessScore` ou threshold em
  resposta HTTP, WebSocket, log, evento de mensageria ou arquivo exportado
- Frames processados em memória e descartados; nunca em disco
- Nenhuma decisão biométrica no cliente — ele captura, envia e exibe o que o
  servidor mandar

### Registro de ponto

**Falha biométrica nunca impede o registro do ponto.** A Portaria 671/2021,
Art. 78 §1º, veda ao REP mecanismos que restrinjam a marcação. A biometria
classifica a *confiança* do registro (`Verified`, `PendingReview`, `Disputed`,
`ManualEntry`), nunca a sua existência.

`AttendanceRecord` e `AuditLog` são **append-only**: trigger no PostgreSQL
rejeita `UPDATE` e `DELETE`.

### Isolamento multiempresa

Toda entidade de negócio implementa `ITenantOwned`. Isolamento aplicado em duas
camadas: filtro global do EF Core **e** verificação explícita no handler. Recurso
de outra empresa retorna `404`, nunca `403`.

`CompanyId` vem sempre do token via `ICurrentUser` — nunca de corpo, query string
ou header.

---

## Qualidade

Clean Code · SOLID · alta coesão · baixo acoplamento · DI · responsabilidade
única · métodos pequenos · nomes claros.

**Sem complexidade artificial.** Não criar interface, factory ou padrão sem
necessidade real. Abstração para uma única implementação precisa de justificativa
concreta. Proibidos `GenericRepository`, `GenericService`, `BaseHandler` genérico.

Async correto: nunca `.Result`, `.Wait()` ou `.GetAwaiter().GetResult()`.
`CancellationToken` propagado em toda operação assíncrona.

---

## Idioma

| Contexto | Idioma |
|----------|--------|
| Código, identificadores, tabelas, endpoints | Inglês |
| Comentários no código | Inglês |
| Interface e mensagens ao usuário | Português (pt-BR) |
| README e documentação | Português |
| Commits, branches, PRs | Inglês |

Comentários explicam **o código**: algoritmo não óbvio, razão de uma decisão
técnica local, ou referência a norma quando o código implementa regra legal.

---

## Git

### Branches semânticas

```
main                             produção
develop                          integração
feature/<escopo>-<descricao>     nova funcionalidade
fix/<escopo>-<descricao>         correção de defeito
refactor/<escopo>-<descricao>    refatoração sem mudança de comportamento
test/<escopo>-<descricao>        apenas testes
chore/<escopo>-<descricao>       build, dependências, tooling
docs/<escopo>-<descricao>        documentação
perf/<escopo>-<descricao>        performance
```

Escopos: `auth`, `company`, `employee`, `schedule`, `biometrics`, `attendance`,
`compliance`, `security`, `audit`, `export`, `api`, `web`, `app`, `infra`, `db`.

### Commits — Conventional Commits, em inglês

```
<tipo>(<escopo>): <descrição imperativa curta>
```

Exemplo: `feat(attendance): register attendance with biometric validation`

O **tipo do commit** segue Conventional Commits (`feat`), enquanto a **branch**
usa o prefixo por extenso (`feature/`). São convenções distintas e ambas valem.

### Nunca versionar

Certificados (`.pfx`, `.p12`, `.pem`), `.env`, modelos ONNX, dataset biométrico
de teste, arquivos AFD gerados.
