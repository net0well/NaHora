## O que muda

<!-- Descreva o comportamento que muda, não os arquivos que mudaram. -->

## Por quê

<!-- O problema que isto resolve. Se houver alternativa descartada, diga qual e por quê. -->

## Como testar

<!-- Passos para verificar manualmente, quando aplicável. -->

---

## Checklist

### Qualidade
- [ ] Mapeamento manual e explícito, sem AutoMapper
- [ ] Nenhuma abstração criada para uma única implementação sem justificativa
- [ ] `CancellationToken` propagado nas operações assíncronas
- [ ] Sem `.Result`, `.Wait()` ou `.GetAwaiter().GetResult()`
- [ ] Comentários explicam o código, e nada além do código

### Arquitetura
- [ ] Feature organizada como slice vertical, sem referência a outro slice
- [ ] `Domain` continua sem referenciar `Infrastructure`, `Features` ou `Api`
- [ ] Entidade nova implementa `ITenantOwned`
- [ ] Entidade nova foi avaliada para `[Audit]`

### Segurança e privacidade
- [ ] Nenhum embedding, `recognitionScore`, `livenessScore` ou threshold em resposta, log, evento ou exportação
- [ ] Nenhum segredo, certificado ou modelo adicionado ao repositório
- [ ] Isolamento por `CompanyId` verificado no handler, não só no filtro global
- [ ] Recurso de outra empresa retorna `404`, nunca `403`

### Conformidade
- [ ] Nenhuma alteração permite que uma falha biométrica **impeça** o registro de ponto
- [ ] `AttendanceRecord` e `AuditLog` seguem append-only

### Testes
- [ ] Cobertos os caminhos de sucesso e de falha
- [ ] Nenhum teste depende de ordem de execução ou de relógio real
- [ ] `dotnet test` e `flutter test` verdes localmente

### Dependências
- [ ] Licença de todo pacote novo verificada antes de entrar em `Directory.Packages.props`
- [ ] Versão concreta, sem `*`
