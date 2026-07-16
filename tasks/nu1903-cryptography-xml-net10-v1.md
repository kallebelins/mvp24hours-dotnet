# NU1903 — `System.Security.Cryptography.Xml` (tarefa 7.1)

> Data: 15/07/2026 · Task ADO [#87305](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87305)

## Advisories

| GHSA | CVE | Severidade | Afetado | Corrigido |
|---|---|---|---|---|
| [GHSA-37gx-xxp4-5rgx](https://github.com/advisories/GHSA-37gx-xxp4-5rgx) | CVE-2026-33116 | High (DoS) | `System.Security.Cryptography.Xml` ≥10.0.0 ≤10.0.5 | **10.0.6+** |
| [GHSA-w3x6-4m5h-cxqf](https://github.com/advisories/GHSA-w3x6-4m5h-cxqf) | (mesmo pacote / EncryptedXml DoS) | High | idem | **10.0.6+** |

## Árvore de dependência (causa raiz)

```
System.ServiceModel.Http / System.ServiceModel.Primitives 10.0.652802
  └── System.Security.Cryptography.Xml 10.0.0   ← vulnerável (NU1903)
```

- Origem direta: `Mvp24Hours.Infrastructure` referencia `System.ServiceModel.*`.
- `Infrastructure` tem `FrameworkReference` `Microsoft.AspNetCore.App`, então o NuGet de `Cryptography.Xml` era **podado** nesse projeto (não aparecia em `dotnet list package --include-transitive`).
- Consumidores **sem** shared framework AspNetCore (CronJob, Caching, Pipe, testes, etc.) resolviam o pacote NuGet **10.0.0** e emitiam NU1903 (≈84 avisos no baseline).

## Estratégia avaliada

| Opção | Resultado |
|---|---|
| Só `PackageVersion` no CPM (`CentralPackageTransitivePinningEnabled=false`) | **Não** sobrescreve transitivo → NU1903 permanece |
| `CentralPackageTransitivePinningEnabled=true` + `PackageVersion` 10.0.10 | NU1903 → 0, mas **NU1109** em `Application.RabbitMQ.Test` (`RabbitMQ.Client` 6.8.1 vs MassTransit ≥7.2.1) |
| `PackageReference` direto em `Infrastructure` + `PackageVersion` 10.0.10 | **NU1903 → 0**; 1× NU1510 esperado no Infrastructure (pacote já no AspNetCore.App) |

## Correção aplicada

1. `src/Directory.Packages.props`: `PackageVersion` `System.Security.Cryptography.Xml` **10.0.10** (patcheado; latest na linha 10.x).
2. `src/Mvp24Hours.Infrastructure/Mvp24Hours.Infrastructure.csproj`: `PackageReference` direto para forçar unificação na grafo dos consumidores.
3. `CentralPackageTransitivePinningEnabled` permanece **false** até bump de `RabbitMQ.Client` ≥7.2.1 (follow-up da 3.3 / futura higiene).

## Validação (15/07/2026)

- `dotnet restore src/Mvp24Hours.sln` → **0** NU1903, **0** NU1109, **0** erro(s).
- `dotnet list src/Mvp24Hours.sln package --vulnerable --include-transitive` → **nenhum** projeto com pacote vulnerável (fontes atuais).
- `dotnet build src/Mvp24Hours.sln -c Debug` → **0** erro(s).

## Residual / follow-ups

- **NU1510** em `Infrastructure` para `System.Security.Cryptography.Xml`: intencional; **não** remover na 7.2 sem alternativa (pin transitivo + bump RabbitMQ.Client).
- Remover o pin quando `System.ServiceModel.Primitives` passar a puxar `Cryptography.Xml` ≥10.0.6.
- 7.3: revalidar job `security-scan` do CI.
