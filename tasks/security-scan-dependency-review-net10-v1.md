# Security-scan e Dependency Review (tarefa 7.3)

> Data: 15/07/2026 · Task ADO [#87307](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87307)

## Objetivo

Após 7.1 (NU1903 / `Cryptography.Xml`) e 7.2 (NU1510), confirmar que não restam vulnerabilidades NuGet e que os gates de CI de dependência não bloqueiam PRs por falso positivo da migração .NET 10.

## Comando local (espelha o job `security-scan`)

```bash
dotnet list src/Mvp24Hours.sln package --vulnerable --include-transitive
```

### Resultado (15/07/2026)

| Métrica | Valor |
|---|---|
| Fonte de advisory | `https://api.nuget.org/v3/index.json` |
| Projetos da solução | **28** |
| Projetos com pacote vulnerável | **0** |
| Exit code | **0** |
| Residual restore | 1× NU1510 intencional (`System.Security.Cryptography.Xml` pin da 7.1) — **não** é vulnerabilidade |

Todos os projetos reportaram: *“não tem nenhum pacote vulnerável, considerando as fontes atuais.”*

## Revisão do job `security-scan` (`.github/workflows/ci.yml`)

### Achado

O step rodava `dotnet list package --vulnerable --include-transitive` na **raiz** do repositório. Não há `.sln`/`.csproj` na raiz → o comando **falha** localmente com *“Um arquivo de projeto ou de solução não pôde ser encontrado”*. Além disso, `dotnet list` **não** falha (exit ≠ 0) quando há advisories — só imprime a lista —, então o job nunca bloqueava o CI por vulnerabilidade real.

### Correção aplicada

1. Escopo explícito: `dotnet list src/Mvp24Hours.sln package --vulnerable --include-transitive`
2. `set -o pipefail` + `grep` EN/PT da frase de hit → `exit 1` se houver pacotes vulneráveis
3. Upload do `security-scan.log` mantido (`if: always()`)

## Revisão do `dependency-review.yml`

Configuração atual:

| Opção | Valor | Avaliação pós-7.1/7.2 |
|---|---|---|
| Trigger | `pull_request` | Adequado (review de delta de dependências no PR) |
| `fail-on-severity` | `moderate` | Adequado — GHSA da 7.1 era **High**; o pin `Cryptography.Xml` **10.0.10** remove o advisory da grafo atual |
| `deny-licenses` | `GPL-2.0, GPL-3.0` | Sem conflito conhecido com o CPM (`Directory.Packages.props`): pacotes MIT/Apache-2.0/BSD; **não** introduz falso positivo pela migração |
| Permissões | `contents: read` | Suficiente para falhar o check; comentário automático em PR exigiria `pull-requests: write` (opcional, fora do escopo) |

### Conclusão sobre falsos positivos

- **Vulnerabilidades:** o Delta Review só falha em advisories **introduzidos/alterados no PR**. Com a grafo já sanitizada (scan local zerado), PRs que não rebaixarem pacotes para versões vulneráveis não serão bloqueados pelo caso NU1903 antigo.
- **Licenças:** o deny GPL não afeta os pacotes da solução atual.
- **NU1510 / NU1900:** não são advisories do Dependency Review — irrelevantes para este gate.
- **Nenhuma alteração** necessária em `dependency-review.yml` nesta tarefa.

## Follow-ups (fora do escopo 7.3)

- Demais jobs do `ci.yml` (`build-and-test`, `code-quality`, `package`) ainda invocam `dotnet restore`/`build`/`test`/`pack` sem prefixo `src/` — possível falha na raiz; candidato a ajuste na Fase 10 ou hygiene de CI.
- Quando `System.ServiceModel.*` passar a puxar `Cryptography.Xml` ≥10.0.6, remover o pin NU1510 da 7.1.
