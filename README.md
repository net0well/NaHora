<div align="center">

# FaceId

**Controle de ponto com reconhecimento facial, anti-fraude e conformidade legal**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Flutter](https://img.shields.io/badge/Flutter-3.47-02569B)](https://flutter.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-4169E1)](https://www.postgresql.org/)
[![Portaria 671](https://img.shields.io/badge/Portaria%20671%2F2021-REP--P-006400)](https://www.gov.br/trabalho-e-emprego/pt-br)

</div>

---

## Sobre

O FaceId registra jornada de trabalho com identificação biométrica e prova de
vivacidade, produzindo registros que satisfazem a **Portaria MTP 671/2021**
(REP-P) — NSR sequencial, AFD assinado digitalmente e comprovante ao trabalhador.

Os mecanismos usuais de marcação de ponto — crachá, PIN, planilha, app com
geolocalização — são trivialmente fraudáveis: qualquer pessoa marca por qualquer
outra. O empregador não consegue provar quem registrou, e o trabalhador não
consegue contestar um registro que não fez.

### Duas decisões que definem o produto

**Reconhecimento facial não basta.** Uma foto no celular de um colega derruba
qualquer sistema que só compare rostos. Por isso a identificação é sempre
combinada com **liveness ativo** — um desafio sorteado pelo servidor, validado
pela sequência temporal dos landmarks — e com anti-spoofing passivo.

**Falha biométrica nunca impede o registro do ponto.** A Portaria 671, Art. 78
§1º, veda ao registrador mecanismos que restrinjam a marcação. A biometria aqui
classifica a **confiança** do registro, nunca a sua existência: toda marcação
recebe NSR e entra no AFD. Uma tentativa suspeita gera evidência e vai para
revisão humana — o fraudador consegue marcar, mas fica marcado, rastreável e
auditável. É o oposto de um sistema que apaga a prova de que alguém trabalhou.

---

## Arquitetura

```
┌─────────────────────┐
│  Flutter (Android)  │  captura, orienta o enquadramento,
└──────────┬──────────┘  nunca decide quem é
           │ HTTPS + SSE
           ▼
┌─────────────────────┐
│  ASP.NET Core 10    │  Minimal API · Vertical Slice · CQRS
│      Minimal API    │
└──────────┬──────────┘
           │
    ┌──────┼──────────────┬──────────────┐
    ▼      ▼              ▼              ▼
PostgreSQL  MinIO      RabbitMQ    ONNX Runtime
   17     evidências   eventos     YuNet + SFace
```

### Backend — Vertical Slice

```
backend/src/
├── FaceId.Api/             endpoints, composição, middleware
├── FaceId.Features/        slices verticais (command, handler, endpoint)
├── FaceId.Domain/          entidades, value objects, regras, portas
├── FaceId.Infrastructure/  EF Core, MinIO, RabbitMQ, ONNX, assinatura
└── FaceId.Shared/          primitivos sem dependência de domínio
```

`Domain` não referencia nenhum projeto — a regra é verificada por teste de
arquitetura que quebra o build. Cada slice é autocontido e não referencia outro
slice.

### Pipeline biométrico

```
Frame → Detecção → Qualidade → Alinhamento → Liveness → Embedding
                                                            ↓
                        Decisão ← Similaridade (cosseno) ────┘
                           ↓
        Verified  |  PendingReview  |  Disputed
```

Cada etapa é uma responsabilidade separada, testável isoladamente. Não existe um
método `ValidateFace()` que faça tudo.

---

## Tecnologias

| Camada | Escolha | Licença |
|--------|---------|---------|
| Runtime | .NET 10 | MIT |
| API | ASP.NET Core Minimal API | MIT |
| CQRS + Mensageria | **WolverineFX 6** | MIT |
| ORM | EF Core 10 + Npgsql | MIT |
| Banco | PostgreSQL 17 | PostgreSQL |
| Objetos | MinIO | AGPL-3.0 (serviço externo) |
| Broker | RabbitMQ | MPL-2.0 |
| Validação | FluentValidation | Apache-2.0 |
| Inferência | ONNX Runtime | MIT |
| Detecção facial | YuNet | Apache-2.0 |
| Embeddings | SFace | Apache-2.0 |
| Frontend | Flutter + Riverpod | BSD-3 / MIT |
| Testes | xUnit v3, Shouldly, NSubstitute, Testcontainers | Apache-2.0 / MIT |

### Por que não MediatR, MassTransit ou InsightFace

**MediatR v13+ e MassTransit v9 passaram a ser comerciais** (MassTransit em torno
de US$ 400/mês). O Wolverine é MIT e cobre mediator local, RabbitMQ e outbox
transacional num pacote só.

**Os pesos do InsightFace/ArcFace/SCRFD são _non-commercial research only_.** O
código é MIT, mas é o peso que faz o reconhecimento. Como o FaceId é comercial, a
stack usa YuNet e SFace, ambos Apache-2.0. Detalhes e riscos conhecidos em
[`models/README.md`](models/README.md).

Também estão fora: AutoMapper (comercial, e o projeto exige mapeamento manual),
FluentAssertions v8+ (comercial) e SixLabors.ImageSharp v3+ (licença paga acima
de US$ 1M de receita). Um teste de arquitetura falha o build se qualquer um deles
entrar no `Directory.Packages.props`.

---

## Requisitos

| Ferramenta | Versão | Observação |
|------------|--------|------------|
| .NET SDK | 10.0.400+ | |
| Flutter | 3.47+ | Canal stable |
| Android SDK | API 36 | Com *command-line tools* e licenças aceitas |
| JDK | 17 | Exigido pelo Gradle |
| Docker Desktop | 29+ | Para os testes de integração do backend |

As licenças do Android precisam ser aceitas uma vez:

```bash
flutter doctor --android-licenses
```

Sem elas o `flutter build apk` falha, e a mensagem não diz que o motivo é
licença.

---

## Como executar

### 1. Infraestrutura

```bash
cp .env.example .env
```

Ajuste o `.env` e suba os serviços:

```bash
docker compose up -d
```

| Serviço | Endereço | Credenciais |
|---------|----------|-------------|
| PostgreSQL | `localhost:5432` | conforme `.env` |
| MinIO (API) | `localhost:9000` | conforme `.env` |
| MinIO (console) | http://localhost:9001 | conforme `.env` |
| RabbitMQ | `localhost:5672` | conforme `.env` |
| RabbitMQ (management) | http://localhost:15672 | conforme `.env` |
| Mailpit (SMTP) | `localhost:1025` | — |
| Mailpit (webmail) | http://localhost:8025 | — |

O Mailpit captura todo e-mail enviado em desenvolvimento. Nada sai da máquina,
então um endereço digitado errado em teste não alcança uma pessoa real.

### 2. Backend

```bash
cd backend && dotnet run --project src/FaceId.Api
```

| Endpoint | Descrição |
|----------|-----------|
| `/health/live` | Processo vivo |
| `/health/ready` | PostgreSQL, MinIO, RabbitMQ, modelos, certificado |
| `/openapi/v1.json` | Especificação OpenAPI (desenvolvimento) |
| `/api/v1/system/info` | Informação de build e runtime |

### 3. Frontend

```bash
cd frontend/faceid && flutter run -d android
```

Com um aparelho conectado por USB (depuração USB ligada) ou um emulador aberto.
`flutter devices` lista o que está disponível.

---

## Testes

```bash
cd backend && dotnet test
```

| Projeto | Cobre |
|---------|-------|
| `FaceId.UnitTests` | Domínio, regras, cálculo de jornada, decisão biométrica |
| `FaceId.ArchitectureTests` | Direção de dependência, isolamento de slices, licenças |
| `FaceId.IntegrationTests` | API completa com PostgreSQL, MinIO e RabbitMQ reais |

Testes de integração usam **Testcontainers** com bancos reais. Provider em
memória não tem `timestamptz`, `jsonb`, trigger nem `FOR UPDATE` — testar contra
ele validaria um sistema que não existe.

`dotnet test` roda em modo Microsoft.Testing.Platform, configurado no
`global.json`.

---

## Segurança e privacidade

Dado biométrico é **dado pessoal sensível** (LGPD, Art. 5º II) e, ao contrário de
uma senha, **não pode ser trocado depois de vazar**.

- Embeddings cifrados em repouso com AES-256-GCM
- Embedding, score de similaridade, score de liveness e thresholds **nunca**
  aparecem em resposta HTTP, WebSocket, log, evento ou arquivo exportado
- Frames processados em memória e descartados; nunca gravados em disco
- Evidências em bucket privado, acessíveis só por URL pré-assinada, com todo
  acesso auditado
- Consentimento biométrico específico e destacado (Art. 11), revogável com efeito
  imediato
- Retenção configurável por empresa, com mínimos legais impostos pelo sistema
- `AttendanceRecord` e `AuditLog` são append-only, com trigger no banco
  rejeitando `UPDATE` e `DELETE`
- Isolamento multiempresa em duas camadas: filtro global do EF Core **e**
  verificação explícita no handler

---

## Conformidade — Portaria MTP 671/2021

| Exigência | Situação |
|-----------|----------|
| NSR sequencial sem lacunas | Contador transacional por empresa |
| Registro inalterável | Trigger no banco + hash encadeado |
| Vedação a restringir a marcação (Art. 78 §1º) | Toda marcação é registrada e classificada |
| Comprovante ao trabalhador (Art. 84) | PDF e texto |
| AFD no layout oficial | ISO-8859-1, CRC-16, assinatura CAdES |
| Assinatura ICP-Brasil (Art. 88) | Certificado e-CNPJ A1 |

> **Antes de produção:** o layout do Anexo I deve ser lido da fonte oficial e o
> AFD gerado validado com ferramenta oficial ou homologada. Registro do REP-P
> junto ao MTE, atestado técnico e DPIA são obrigações do controlador, fora do
> escopo do software.

---

## Roadmap

- [x] Fundação — solução, DI automática, CQRS, testes, Docker
- [ ] Banco e infraestrutura — EF Core, migrations, MinIO, RabbitMQ
- [ ] Autenticação — CPF + senha, JWT, refresh rotativo, permissões
- [ ] Design System e telas base
- [ ] Jornada de trabalho e cálculo de horas
- [ ] Câmera e moldura facial
- [ ] Biometria — detecção, embeddings, liveness, anti-spoofing
- [ ] Registro de ponto e histórico
- [ ] Eventos de segurança e revisão
- [ ] Auditoria automática
- [ ] Mensageria e eventos de domínio
- [ ] Exportação CSV e Excel
- [ ] AFD, AEJ e comprovante
- [ ] Calibração biométrica (FAR/FRR) e endurecimento

---

## Licença

Projeto privado. Licenças de terceiros nas tabelas acima e em
[`models/README.md`](models/README.md).
