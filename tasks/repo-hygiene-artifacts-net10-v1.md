# Higiene de artefatos versionados (tarefa 8.2)

> Data: 16/07/2026 · Task ADO [#87309](https://bancorbras-ti.visualstudio.com/Bancorbrás-Agile/_workitems/edit/87309)

## Comando de auditoria

```powershell
git ls-files | Select-String -Pattern '\.(log|txt)$'
git ls-files | Select-String -Pattern '(^|/)(bin|obj)/|TestResults/|\.trx$|\.coverage$|\.nupkg$'
```

## Resultado — `.log` / `.txt` versionados

| Arquivo | Decisão |
|---|---|
| `mongo-cs8625.txt` (raiz) | **Removido** — dump ad-hoc de avisos CS8625 (~18KB), não documentação |
| `docs/llms_compact_en.txt` | Mantido — doc intencional para LLMs |
| `docs/llms_compact_pt.txt` | Mantido — idem |
| `docs/llms_complete_en.txt` | Mantido — idem |
| `docs/llms_complete_pt.txt` | Mantido — idem |
| `build-webapi-errors.txt` | Já tratado na 8.1 (`git rm` + `build-*.txt` no `.gitignore`) |

## Resultado — outros artefatos de build

- **0** arquivos em `bin/` / `obj/` versionados
- **0** `.trx`, `.coverage`, `.nupkg`, `.snupkg` versionados
- Logs locais (`build-*.log`, `build-baseline-*.log`, etc.) presentes no working tree mas **não** rastreados (`*.log` no `.gitignore`)
- Dumps locais em `tasks/` (`core-nullable-warnings.txt`, `core-nullable-remaining.txt`) **não** rastreados (`tasks/*` no `.gitignore`)

## Ajuste no `.gitignore`

Além do que a 8.1 já cobria (`*.log`, `build-*.txt`):

```gitignore
*-cs[0-9]*.txt
```

Cobre dumps no estilo `mongo-cs8625.txt` / `foo-cs8618.txt` sem afetar `docs/llms_*.txt`.

O padrão amplo `tasks/*` (notas locais) passou a ter exceções para evidências versionáveis:

```gitignore
tasks/*
!tasks/*.md
!tasks/*.json
```

Assim dumps (`*.txt`) em `tasks/` continuam ignorados, enquanto inventários/evidências `.md`/`.json` das fases anteriores e desta tarefa entram no controle de versão.