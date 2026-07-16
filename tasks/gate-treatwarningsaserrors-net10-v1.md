# Validação do gate `TreatWarningsAsErrors` — .NET 10 (v1)

> Gerado em 2026-07-15  
> Referência: [tasks-net10-v1.md](./tasks-net10-v1.md) §2.3 · ADO Task [#87280](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87280)  
> Workflow: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) job `code-quality`, step `🔍 Run static code analysis`  
> Baseline de avisos: [`baseline-net10-v1.md`](./baseline-net10-v1.md) (**4235** avisos / **0** erros sem o gate)

## Decisão (não alterar o CI nesta fase)

**Manter** `/p:TreatWarningsAsErrors=true`. O gate é o **objetivo final** da modernização (Fase 10 / tarefa 10.3), não um problema a remover na Fase 2. Removê-lo ou relaxá-lo mascararia a dívida de avisos e reabriria regressões de qualidade.

## O que o job `code-quality` faz hoje

| Step | Comando | Efeito esperado hoje |
|---|---|---|
| `🎨 Check code formatting` | `dotnet format --verify-no-changes` | Pode falhar se o código não estiver alinhado ao `.editorconfig` (ainda ausente — tarefa 3.2) |
| `🔍 Run static code analysis` | `dotnet build --configuration Release /p:TreatWarningsAsErrors=true` | **Falha** enquanto houver avisos (CS/NU/SYSLIB/CA/…) — ver evidência abaixo |

O job roda apenas em `pull_request` (`if: github.event_name == 'pull_request'`). Os jobs `build-and-test` / `package` **não** usam `TreatWarningsAsErrors`; eles podem passar com avisos.

## Evidência local (15/07/2026)

```text
dotnet build src/Mvp24Hours.sln --configuration Release /p:TreatWarningsAsErrors=true
→ FALHA da compilação
→ 0 Aviso(s) · 83 Erro(s)  (avisos promovidos a erro)
```

Log bruto (gitignored via `*.log`): `build-treatwarningsaserrors.log`.

### Códigos que derrubaram o gate nesta execução

A falha ocorreu **já no restore** (NuGet Audit / PackageReference). Compilação C# nem chegou a emitir os ~4000 CS* do baseline — os NU* bastam para vermelho.

| Código | Ocorrências no log | Origem / fase que resolve |
|---|---|---|
| NU1903 | 84 | Vulnerabilidade `System.Security.Cryptography.Xml` → **Fase 7.1** |
| NU1900 | 50 | Feed privado inacessível (Moblix) → **Fase 7** (mitigar/documentar residual) |
| NU1510 | 32 | `PackageReference` redundante → **Fase 7.2** |
| **Subtotal NU** | **~83** (resumo MSBuild) | Gate vermelho **antes** dos CS* |

### O que ainda falharia após o restore passar

Com o baseline de **4235** avisos ([§1.1](./baseline-net10-v1.md)), qualquer avisos restantes viram erro sob o mesmo flag. Dependências principais:

| Família | ~Qtd (baseline) | Fase de correção |
|---|---|---|
| CS8632 + NRT reais / override | ~3839 | **Fases 3–4** (`Directory.Build.props` + Nullable + correções) |
| CS0618 / SYSLIB0xxx / ASPDEPR006 | ~79 | **Fase 5** |
| Qualidade (CS0168, CA2022, xUnit1031, …) | ~16 | **Fase 6** |
| LOGGEN002 | 93 | incluir no trabalho de NRT/analyzer (Fases 3–4 / 6) |
| NU1903 / NU1900 / NU1510 | ~212 | **Fase 7** |

## Por que o CI só fica totalmente verde no fim da Fase 10

1. **Fase 2** (esta) só desbloqueia o SDK (.NET 10 nos workflows). O gate de qualidade permanece intocado e **continuará vermelho** em PRs.
2. **Fases 3–6** eliminam a maior parte dos avisos de linguagem/API/qualidade.
3. **Fase 7** é **obrigatória** para o gate: hoje o restore já falha só com NU*.
4. **Fases 8–9** são higiene e testes; não removem o gate.
5. **Fase 10** (tarefas 10.1–10.3) é a validação explícita de zero warnings + `TreatWarningsAsErrors=true` + `dotnet format --verify-no-changes`.

Texto sugerido para o PR da Fase 2:

> O job `code-quality` mantém deliberadamente `TreatWarningsAsErrors=true`. Validação local (tarefa 2.3) confirma falha imediata (~83 erros NuGet no restore; baseline total 4235 avisos). O CI de PR só ficará verde após as Fases 3–7 (aviso/CS/NU) e o fechamento na Fase 10 — **não** remover ou relaxar o gate nesta fase.

## Comando de revalidação (Fase 10 / tarefa 10.3)

```powershell
dotnet build src/Mvp24Hours.sln --configuration Release /p:TreatWarningsAsErrors=true
```

Critério de sucesso: **0 erro(s)** e **0 aviso(s)**.
