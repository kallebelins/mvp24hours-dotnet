# Validação end-to-end do gate `TreatWarningsAsErrors` — .NET 10 (v1)

> Gerado em 2026-07-17
> Referência: [tasks-net10-v1.md](./tasks-net10-v1.md) §10.3 · ADO Task [#87312](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87312)
> Workflow: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) job `code-quality`, step `🔍 Run static code analysis`
> Config: [`src/Directory.Build.props`](../src/Directory.Build.props) (`MvpResidualWarnings` / `WarningsNotAsErrors`)
> Relacionado: [`gate-treatwarningsaserrors-net10-v1.md`](./gate-treatwarningsaserrors-net10-v1.md) (§2.3, baseline do gate) · [`format-verify-net10-v1.md`](./format-verify-net10-v1.md) (§10.2)

## Decisão

Reativar o gate `TreatWarningsAsErrors=true` no CI **agora**, em modo **escopado**: qualquer categoria de aviso **nova** derruba o build (protege contra regressões de qualidade), enquanto o **residual aceito na tarefa 10.1** permanece como aviso (não erro) via `WarningsNotAsErrors`. A eliminação total desse residual (zero-warnings estrito) segue adiada para a higiene **v2**.

Alternativa descartada nesta rodada: manter o gate removido até residual ≈ 0 (bloqueava indefinidamente a proteção contra novos avisos).

## Como o gate ficou (CI)

Step `🔍 Run static code analysis` de [`ci.yml`](../.github/workflows/ci.yml):

```yaml
- name: 🔍 Run static code analysis
  run: dotnet build src/Mvp24Hours.sln --configuration Release --no-incremental /p:TreatWarningsAsErrors=true
```

Dois pontos essenciais:

1. **`src/Mvp24Hours.sln`** — a solução vive em `src/`; um `dotnet build` na raiz não encontra `.sln`.
2. **`--no-incremental`** — sem isso, um build incremental pula a compilação e **não re-emite** os avisos; o gate ficaria "verde" por engano (observado localmente: build incremental → `0 Aviso(s)` / `0 Erro(s)`). O CI faz checkout limpo, mas o flag garante o comportamento correto em qualquer runner com cache.

## Lista escopada (`src/Directory.Build.props`)

```xml
<MvpResidualWarnings>CS8600;CS8602;CS8603;CS8604;CS8618;CS8619;CS8620;CS8622;CS8625;CS8629;CS8631;CS8764;LOGGEN002;CS0618;ASPDEPR006;SYSLIB0057;CS0108;xUnit1031;NU1510</MvpResidualWarnings>
<WarningsNotAsErrors>$(WarningsNotAsErrors);$(MvpResidualWarnings)</WarningsNotAsErrors>
```

Distribuição do residual (build `Release` completo, sem gate — `948` avisos únicos no resumo MSBuild):

| Família | Códigos | Fase que zera (v2) |
|---|---|---|
| Nullable (NRT) | CS8600 CS8602 CS8603 CS8604 CS8618 CS8619 CS8620 CS8622 CS8625 CS8629 CS8631 CS8764 | Fase 4 (residual `Application`/testes) |
| Logging source-gen | LOGGEN002 | analisador de logging |
| API obsoleta | CS0618 · ASPDEPR006 · SYSLIB0057 | Fase 5 (residual fora do escopo) |
| Ocultação de membro | CS0108 | Fase 6 (residual em testes) |
| xUnit async | xUnit1031 | Fase 6 (residual em testes) |
| NuGet (intencional) | NU1510 | mantido pelo pin de segurança da 7.1 |

## Evidências (2026-07-17)

**1. Gate estrito, sem escopo (prova de que o gate morde):**

```text
dotnet build src/Mvp24Hours.sln -c Release --no-incremental /p:TreatWarningsAsErrors=true
(sem WarningsNotAsErrors) → FALHA · 174 Erro(s)  (avisos promovidos a erro)
```

**2. Gate escopado (estado final adotado):**

```text
dotnet build src/Mvp24Hours.sln -c Release --no-incremental /p:TreatWarningsAsErrors=true
(com WarningsNotAsErrors=MvpResidualWarnings) → ÊXITO · 948 Aviso(s) · 0 Erro(s) · exit 0
```

**3. Proteção contra categoria NOVA (teste reversível):** injeção de uma variável não usada (`CS0219`, fora da lista) em `Mvp24Hours.Core.Test/ClockAndGuidTest.cs`:

```text
dotnet build src/Tests/Mvp24Hours.Core.Test/... -c Release --no-incremental /p:TreatWarningsAsErrors=true
→ FALHA · error CS0219 · 1 Erro(s)   (residual segue como 37 Aviso(s))
```

Probe removido em seguida (`git diff` limpo); revalidação final da solução: **948 Aviso(s) / 0 Erro(s) / exit 0**.

Logs brutos locais (gitignored via `*.log`): `build-10.3-gate.log` (sem escopo), `build-10.3-gate-scoped.log` / `build-10.3-gate-final.log` (escopado).

## Comando de revalidação

```powershell
dotnet build src/Mvp24Hours.sln --configuration Release --no-incremental /p:TreatWarningsAsErrors=true
```

Critério de sucesso: **0 erro(s)** (o residual permanece como aviso). Qualquer código de aviso **não** listado em `MvpResidualWarnings` deve derrubar o build.

## Pendências para a v2

- Reduzir progressivamente `MvpResidualWarnings` (retirar códigos à medida que os avisos forem zerados) até chegar a `TreatWarningsAsErrors=true` **estrito** (sem exceções), fechando de vez a dívida da tarefa 10.1.
- Reavaliar o `NU1510` intencional quando o pin de segurança da 7.1 puder ser removido.
