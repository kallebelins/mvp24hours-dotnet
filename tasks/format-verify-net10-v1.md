# `dotnet format --verify-no-changes` — .NET 10 (v1)

> Gerado em 2026-07-17  
> Referência: [tasks-net10-v1.md](./tasks-net10-v1.md) §10.2 · ADO Task [#87311](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87311)  
> Workflow: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) job `code-quality`, step `🎨 Check code formatting`  
> Config: [`.editorconfig`](../.editorconfig) (tarefa 3.2) · Solução: [`src/Mvp24Hours.sln`](../src/Mvp24Hours.sln)

## Resumo

Primeira passada real de formatação da solução sob o `.editorconfig` criado na tarefa 3.2. O `dotnet format` foi **escopado apenas à formatação** (espaço em branco + ordenação de `using`) via `--severity error`; os *code fixes* de analisadores em nível de aviso (CS8625/CS0618/CA*) ficam **fora do escopo** e são adiados para a v2, coerente com o residual de avisos aceito na tarefa 10.1 e o gate `TreatWarningsAsErrors` adiado na 10.3.

**Resultado final:** `dotnet format src/Mvp24Hours.sln --severity error --verify-no-changes` → **exit 0** (`0 de 1723 arquivos formatados`). Build `Debug` da solução após a formatação: **0 erro(s)** / 942 aviso(s) (residual nullable aceito na 10.1).

## Baseline (antes)

```text
dotnet format src/Mvp24Hours.sln --severity error --verify-no-changes --verbosity diagnostic
→ exit 2 (mudanças necessárias) · 1107 de 1723 arquivos precisavam de formatação
```

| Categoria | Ocorrências | Significado |
|---|---|---|
| WHITESPACE | 1135 | Indentação (4 espaços), Allman braces, espaçamento de operadores/vírgulas |
| IMPORTS | 983 | Ordenação de `using` (`dotnet_sort_system_directives_first = true`) |
| FINALNEWLINE | 4 | `insert_final_newline = true` |
| Analisadores (CS8625/CS0618/CA*) | 0 | **Excluídos** por `--severity error` |

## Correção aplicada

```powershell
dotnet format src/Mvp24Hours.sln --severity error --verbosity minimal   # exit 0
```

- **1107** arquivos `.cs` reformatados (`3408 insertions(+), 3406 deletions(-)` — quase equilibrado, típico de reindentação + reordenação de `using`).
- **0** adições de `[Obsolete]` e **0** alterações de nulidade (`?`) — confirmado por `git diff`. A passada é puramente mecânica/formatação; nenhuma mudança semântica.
- Remoção de BOM UTF-8 em arquivos que ainda o tinham (`charset = utf-8`).

## Verificação (depois)

```text
dotnet format src/Mvp24Hours.sln --severity error --verify-no-changes --verbosity diagnostic
→ exit 0 · 0 de 1723 arquivos formatados
```

## Por que `--severity error` (escopo formatação)

O `dotnet format` completo (sem `--severity`) roda também o *fixer* de analisadores em nível de **aviso** e tenta:

- **CS0618** → adicionar `[Obsolete]` a métodos de teste que exercitam APIs obsoletas (muda intenção do teste);
- **CS8625** → `CSharpDeclareAsNullableCodeFixProvider` (várias correções **falharam** com *"O nó não faz parte da árvore"*, impossíveis de aplicar automaticamente → o gate nunca ficaria verde sozinho).

Ambos conflitam com a decisão documentada na **10.1** (aceitar 969 avisos residuais; zero-warnings adiado para v2) e na **10.3** (gate `TreatWarningsAsErrors` removido do CI temporariamente). Portanto, o gate de formatação do CI passa a ser **só formatação** (`--severity error`), enquanto a elevação das regras de estilo/analisador para `warning`/`error` fica para a higiene v2 (as regras de estilo no `.editorconfig` seguem em `suggestion`).

## Ajuste no CI

Step `🎨 Check code formatting` de [`ci.yml`](../.github/workflows/ci.yml):

- **Antes:** `dotnet format --verify-no-changes --verbosity diagnostic` — falhava por dois motivos: (1) rodava na raiz do repo, onde **não há `.sln`** (`dotnet format` não encontra projeto/solução); (2) escopo completo incluía *fixers* de analisador que não podem ficar verdes hoje.
- **Depois:** `dotnet format src/Mvp24Hours.sln --severity error --verify-no-changes --verbosity diagnostic` — aponta para a solução e limita ao escopo de formatação.

## Revalidação

```powershell
dotnet format src/Mvp24Hours.sln --severity error --verify-no-changes --verbosity diagnostic
```

Critério de sucesso: **exit 0** (`0 ... arquivos formatados`).
