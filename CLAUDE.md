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

### Um tipo por arquivo

Cada classe, record, interface, enum ou struct vive no seu próprio arquivo,
nomeado como o tipo. Enums não moram dentro da entidade que os usa. Arquivos são
organizados por feature, não por categoria técnica.

### Design Patterns explícitos no nome

Quando um tipo **é** um pattern, o nome diz qual. Quem abre o arquivo não
precisa deduzir pela forma:

| Pattern | Sufixo | Exemplo no projeto |
|---------|--------|--------------------|
| Repository | `Repository` | `UserRepository` |
| Factory | `Factory` | `FaceIdDbContextFactory` |
| Strategy | `Strategy` | — |
| Adapter | `Adapter` | — (ver exceção abaixo) |
| Decorator | `Decorator` | — |
| Builder | `Builder` | — |
| Facade | `Facade` | — |
| Policy | `Policy` | `PasswordPolicy`, `SensitiveDataPolicy` |
| Handler | `Handler` | `LoginHandler` |
| Middleware | `Middleware` | `CorrelationIdMiddleware` |

**Duas exceções, e só estas duas.**

**Factory method estático em entidade** não vira `XFactory`. O nome é a ação do
domínio, que carrega mais informação que o nome do padrão:

```csharp
Employee.Hire(...)      // não EmployeeFactory.Create
Company.Register(...)   // não CompanyFactory.Create
Cpf.Create(...)         // aqui Create é a própria ação
```

**Adaptador de porta** é nomeado pela tecnologia que adapta, não por `Adapter`.
`Argon2PasswordHasher` diz o que muda ao trocar a implementação;
`PasswordHasherAdapter` não diria nada:

```csharp
Argon2PasswordHasher          : IPasswordHasher
JwtAccessTokenService         : IAccessTokenService
BrasilApiCompanyRegistryLookup: ICompanyRegistryLookup
```

E a regra que vale mais que a lista: **não usar um pattern para demonstrar que
o conhece**. Se um `Strategy` tem uma implementação só, é uma interface a mais e
uma indireção a menos de clareza. O sufixo só entra quando o padrão está mesmo
lá.

### Aggregate root explícito na declaração

A distinção fica visível na assinatura da classe, sem precisar abrir mais nada:

```csharp
public sealed class Employee : AggregateRoot   // carregado e salvo sozinho
public sealed class FaceEmbedding : Entity     // só alcançado pelo seu root
```

O que decorre de ser root:

- **Só root tem repositório.** Filho é carregado e salvo através do root.
- **Só root é referenciado por id de fora do agregado.** Referência a um filho
  vindo de outro agregado é erro de modelagem.
- O root garante as invariantes que atravessam seus filhos, e por isso o
  agregado inteiro commita junto.

Na dúvida, **não** é root. Promover errado quebra uma fronteira de consistência
que o domínio esperava manter.

Regra verificada por teste de arquitetura: todo repositório aponta para um
`AggregateRoot`, todo root é `sealed`, e nenhuma entidade expõe setter público.

### Notification pattern, não exceção

Regra de negócio **não lança exceção**. Retorna `Result` ou `Result<T>`, que
carrega *todos* os erros encontrados, não só o primeiro — um formulário que
rejeita um campo por vez é um formulário que o usuário abandona.

```csharp
public static Result<Employee> Hire(...)
{
    var errors = new List<Error>();
    if (companyId == Guid.Empty) errors.Add(EmployeeErrors.CompanyRequired());
    if (string.IsNullOrWhiteSpace(fullName)) errors.Add(EmployeeErrors.NameRequired());
    if (errors.Count > 0) return Result.Failure<Employee>(errors);
    return new Employee(...);
}
```

Exceção fica para o genuinamente excepcional: banco fora do ar, modelo ONNX que
não carrega, bug de referência nula. Essas sobem para o `GlobalExceptionHandler`.

**Na fronteira da API os dois convergem** em `application/problem+json`. O
cliente nunca vê stack trace nem mensagem de exceção — só um `code` estável e uma
mensagem segura.

### Catálogo de erros

Todo erro é declarado em `Domain/Errors/`, um arquivo por agregado
(`CompanyErrors`, `UserErrors`, `EmployeeErrors`, `ValueObjectErrors`). Nunca
criar `Error` inline no meio de uma regra.

```csharp
public static Error NameRequired() => Error.Validation(
    "USER_NAME_REQUIRED", "Full name is required.", "fullName");
```

O `Code` em SCREAMING_SNAKE_CASE é o contrato com o Flutter, que traduz por ele.
A mensagem pode ser reescrita à vontade; o código, não.

### Repositório e unit of work

Repositórios são **específicos por agregado**, nunca genéricos: cada método é uma
pergunta que o domínio realmente faz. Porta no domínio, implementação na
Infrastructure.

`IUnitOfWork` existe para quando atomicidade é regra de negócio — a alocação de
NSR é o caso claro, porque um número consumido e revertido deixaria lacuna, e a
Portaria 671 exige sequência sem nenhuma.

Leitura para listagem e relatório **não** passa por repositório: projeta direto
para DTO, porque carregar agregado inteiro para renderizar tabela é desperdício
que o banco paga.

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

**Método faz uma coisa.** Acima de ~40 linhas, quase sempre há mais de uma
responsabilidade ali dentro. Composições raiz (`AddXServices`) e mapeamentos
declarativos do EF são exceção legítima — são listas, não lógica.

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

### Granularidade — um commit por unidade de implementação

Uma branch tem **vários commits**, nunca um commit único com tudo. Cada commit
entrega uma peça coerente e revisável isoladamente: o value object, depois a
entidade, depois o mapeamento, depois a migration, depois os testes.

Um commit gigante torna a revisão impossível e o `git bisect` inútil.

O **tipo do commit** segue Conventional Commits (`feat`), enquanto a **branch**
usa o prefixo por extenso (`feature/`). São convenções distintas e ambas valem.

### Nunca versionar

Certificados (`.pfx`, `.p12`, `.pem`), `.env`, modelos ONNX, dataset biométrico
de teste, arquivos AFD gerados.
