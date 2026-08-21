# Mvp24Hours DevKit — Instalação global

Scripts PowerShell para instalar o **Mvp24Hours MCP DevKit** como pacote global do usuário, disponível em qualquer projeto, sem copiar `.cursor/` ou `.github/` para cada repositório.

## Pré-requisitos

- [Cursor](https://cursor.com/) ou [VS Code 1.102+](https://code.visualstudio.com/) com suporte a MCP
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Clone do repositório [`mvp24hours-dotnet`](https://github.com/kallebelins/mvp24hours-dotnet)

## O que é instalado

| IDE | MCP (global) | Skills (global) |
| --- | --- | --- |
| **Cursor** | `%USERPROFILE%\.cursor\mcp.json` | `%USERPROFILE%\.cursor\skills\<name>\` (36 pastas) |
| **VS Code** | `%APPDATA%\Code\User\mcp.json` | `%USERPROFILE%\.copilot\skills\<name>\` (36 pastas) |

Os scripts **mesclam** a configuração MCP existente. Outros servidores (por exemplo `azure-devops`) não são removidos.

Cada skill do catálogo vira uma pasta com `SKILL.md` (o `name` do frontmatter). Assim você chama `@efcore-specialist` ou `@demand-architect` direto.

A pasta `skill-router` também inclui:

- `skill-catalog.md` e `mcp-scenarios.md` — índices de roteamento
- `catalog/` — cópia das 35 skills de domínio para o handoff do roteador

O caminho do repositório é gravado **resolvido** no `mcp.json`:

- `--project` — indica ao `dotnet run` qual `.csproj` executar
- `--no-build --configuration Release` — executa o binário já compilado, sem build no startup
- `env.MVP24HOURS_REPO_ROOT` — lido pelo servidor MCP em runtime (`Program.cs`) para indexar `docs/`, `samples/` e `src/`

Se você mover o clone, execute o install novamente com `-Force` para atualizar os paths.

## Build do servidor MCP

O install compila o projeto MCP uma vez em `Release` antes de gravar o `mcp.json`. A entrada gravada usa `dotnet run --no-build`, então **Cursor e VS Code podem rodar o servidor ao mesmo tempo**: sem build no startup, nenhum dos dois tenta sobrescrever os arquivos de `bin/` e `obj/` que o outro processo mantém abertos (o erro "o processo não pode acessar o arquivo porque está sendo usado por outro processo", MSB3021/MSB3027).

Consequências práticas:

- **Depois de um `git pull` no clone**, reexecute o install (ou rode `dotnet build mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj --configuration Release`). Sem isso os IDEs continuam executando o binário anterior.
- **Antes de compilar o repositório**, feche o Cursor e o VS Code. Um servidor em execução ainda bloqueia os arquivos de saída daquela configuração. O install detecta processos `Mvp24Hours.Mcp` rodando a partir de `bin/Release` e aborta com a lista de PIDs, em vez de falhar no meio do build.
- Use `-SkipBuild` se você já compilou em `Release` e quer apenas regravar o `mcp.json` e as skills.

## Instalação

Execute a partir da pasta `scripts/` do clone `mvp24hours-dotnet`:

### Cursor

```powershell
cd C:\Dev\Github\mvp24hours\mvp24hours-dotnet\scripts
.\Install-Mvp24HoursCursorDevKit.ps1
```

### VS Code

```powershell
cd C:\Dev\Github\mvp24hours\mvp24hours-dotnet\scripts
.\Install-Mvp24HoursVsCodeDevKit.ps1
```

### Parâmetros comuns

| Parâmetro | Descrição |
| --- | --- |
| `-RepoRoot` | Caminho do clone. Padrão: pasta pai de `scripts/` |
| `-Force` | Sobrescreve a skill se já existir |
| `-SkipSkill` | Instala somente MCP |
| `-SkipMcp` | Instala somente a skill |
| `-SkipBuild` | Não compila o projeto MCP (exige binários `Release` já existentes) |
| `-WhatIf` | Simula as alterações sem gravar arquivos |

Exemplo com caminho explícito:

```powershell
.\Install-Mvp24HoursCursorDevKit.ps1 `
    -RepoRoot "C:\Dev\Github\mvp24hours\mvp24hours-dotnet" `
    -Force
```

## Verificação

### Cursor

1. Reinicie o Cursor após a instalação.
2. Abra **Settings → Tools & MCP** e confirme que `mvp24hours` está listado e habilitado.
3. No Agent chat, pergunte: *"where do I start with Mvp24Hours?"*
4. O agente deve chamar `list_scenarios` e apresentar uma rota recomendada.

### VS Code

1. Recarregue o VS Code após a instalação.
2. Command Palette → **MCP: List Servers** → inicie `mvp24hours`.
3. Abra o Copilot Chat em **Agent** mode.
4. Use `@skill-router` ou pergunte: *"where do I start with Mvp24Hours?"*

## Desinstalação

### Cursor

```powershell
.\Uninstall-Mvp24HoursCursorDevKit.ps1
```

### VS Code

```powershell
.\Uninstall-Mvp24HoursVsCodeDevKit.ps1
```

Parâmetros adicionais: `-SkipMcp`, `-SkipSkill`, `-WhatIf`.

O uninstall também remove a variável legada `MVP24HOURS_MCP_REPO_ROOT` do usuário, se tiver sido criada por versões anteriores dos scripts.

## Arquivos do pacote

| Arquivo | Função |
| --- | --- |
| `Mvp24HoursDevKit.Common.ps1` | Funções compartilhadas (merge JSON, skill) |
| `Install-Mvp24HoursCursorDevKit.ps1` | Instalação global no Cursor |
| `Uninstall-Mvp24HoursCursorDevKit.ps1` | Remoção global no Cursor |
| `Install-Mvp24HoursVsCodeDevKit.ps1` | Instalação global no VS Code |
| `Uninstall-Mvp24HoursVsCodeDevKit.ps1` | Remoção global no VS Code |

## Troubleshooting

| Problema | Solução |
| --- | --- |
| Servidor não aparece | Reinicie o IDE após a instalação |
| Missing manifest | Confirme que `MVP24HOURS_REPO_ROOT` no `mcp.json` aponta para a raiz do clone (pasta com `docs/` e `samples/`) |
| Servidor falha ao iniciar | Verifique .NET 10 SDK e o caminho para `Mvp24Hours.Mcp.csproj` |
| Servidor não sobe após `-SkipBuild` | Faltam os binários `Release`: rode `dotnet build mcp/src/Mvp24Hours.Mcp/Mvp24Hours.Mcp.csproj --configuration Release` |
| Erro de arquivo bloqueado no build (MSB3021/MSB3027) | Um servidor MCP está em execução; feche Cursor e VS Code ou encerre os processos `Mvp24Hours.Mcp` |
| Servidor rodando código antigo | Reexecute o install (ou o `dotnet build` em Release) após atualizar o clone |
| Skill não aplicada (Cursor) | Confirme `%USERPROFILE%\.cursor\skills\<name>\SKILL.md` (ex.: `skill-router`, `efcore-specialist`) |
| Skill não listada (VS Code) | Confirme `%USERPROFILE%\.copilot\skills\<name>\SKILL.md` e use Agent mode |
| Outro MCP sumiu | Os scripts não removem outros servidores; verifique se o merge foi feito corretamente |

## Instalação por projeto (alternativa)

Copie [`skills/`](../skills/) para `.cursor/skills/` (Cursor) ou `.github/skills/` (VS Code Copilot). Configure MCP com os exemplos em [`docs/en-us/ai-resources/home.md`](../docs/en-us/ai-resources/home.md) e os JSON em [`mcp/templates/`](../mcp/templates/).

## Notas

- **VS Code Insiders** usa outro perfil (`Code - Insiders`). Suporte futuro pode ser adicionado via parâmetro.
