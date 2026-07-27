# Contribuindo para Mvp24Hours

Primeiramente, obrigado por considerar contribuir com o Mvp24Hours! 🎉

É graças a pessoas como você que o Mvp24Hours continua sendo uma biblioteca útil e de qualidade para a comunidade .NET.

## 📋 Índice

- [Código de Conduta](#código-de-conduta)
- [Como Posso Contribuir?](#como-posso-contribuir)
- [Primeiros Passos](#primeiros-passos)
- [Processo de Desenvolvimento](#processo-de-desenvolvimento)
- [Padrões de Código](#padrões-de-código)
- [Commits e Pull Requests](#commits-e-pull-requests)
- [Reportando Bugs](#reportando-bugs)
- [Sugerindo Melhorias](#sugerindo-melhorias)
- [Documentação](#documentação)
- [Testes](#testes)
- [Comunidade](#comunidade)

## 📜 Código de Conduta

Este projeto e todos os participantes são regidos por um código de conduta. Ao participar, você concorda em manter este código. Por favor, reporte comportamentos inaceitáveis para [kallebe.santos@outlook.com].

### Nossos Padrões

**Comportamentos que contribuem para criar um ambiente positivo incluem:**

- ✅ Usar linguagem acolhedora e inclusiva
- ✅ Respeitar pontos de vista e experiências diferentes
- ✅ Aceitar críticas construtivas graciosamente
- ✅ Focar no que é melhor para a comunidade
- ✅ Mostrar empatia com outros membros da comunidade

**Comportamentos inaceitáveis incluem:**

- ❌ Uso de linguagem ou imagens sexualizadas
- ❌ Trolling, comentários insultuosos/depreciativos e ataques pessoais ou políticos
- ❌ Assédio público ou privado
- ❌ Publicar informações privadas de outros sem permissão explícita
- ❌ Outras condutas que possam ser razoavelmente consideradas inapropriadas

## 🤝 Como Posso Contribuir?

Existem várias maneiras de contribuir com o Mvp24Hours:

### 1. Reportar Bugs 🐛
Encontrou um bug? [Abra uma issue](https://github.com/kallebelins/mvp24hours-dotnet/issues/new?template=bug_report.md)

### 2. Sugerir Melhorias 💡
Tem uma ideia? [Sugira uma melhoria](https://github.com/kallebelins/mvp24hours-dotnet/issues/new?template=feature_request.md)

### 3. Melhorar Documentação 📖
A documentação nunca é perfeita. Correções e melhorias são sempre bem-vindas!

### 4. Escrever Código 💻
- Implementar novas funcionalidades
- Corrigir bugs existentes
- Melhorar performance
- Adicionar testes

### 5. Revisar Pull Requests 👀
Ajude revisando PRs de outros contribuidores

### 6. Compartilhar 📢
Compartilhe o projeto nas redes sociais, blogs, eventos, etc.

## 🚀 Primeiros Passos

### Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) ou superior
- [Git](https://git-scm.com/)
- IDE recomendada: [Visual Studio 2022](https://visualstudio.microsoft.com/) ou [Visual Studio Code](https://code.visualstudio.com/)
- [Docker](https://www.docker.com/) (opcional, para testes de integração)

### Configurando o Ambiente

1. **Fork o repositório**
   
   Clique no botão "Fork" no canto superior direito da página do GitHub.

2. **Clone seu fork**
   ```bash
   git clone https://github.com/seu-usuario/mvp24hours-dotnet.git
   cd mvp24hours-dotnet
   ```

3. **Adicione o repositório original como upstream**
   ```bash
   git remote add upstream https://github.com/kallebelins/mvp24hours-dotnet.git
   ```

4. **Restaure as dependências**
   ```bash
   dotnet restore
   ```

5. **Compile o projeto**
   ```bash
   dotnet build
   ```

6. **Execute os testes**
   ```bash
   dotnet test
   ```

### Estrutura do Projeto

```
mvp24hours-dotnet/
├── src/
│   ├── Mvp24Hours.Core/                    # Contratos e interfaces base
│   ├── Mvp24Hours.Application/             # Services e lógica de aplicação
│   ├── Mvp24Hours.Infrastructure/          # Utilitários e helpers
│   ├── Mvp24Hours.Infrastructure.Data.EFCore/     # Entity Framework Core
│   ├── Mvp24Hours.Infrastructure.Data.MongoDb/    # MongoDB
│   ├── Mvp24Hours.Infrastructure.Caching/         # Cache base
│   ├── Mvp24Hours.Infrastructure.Caching.Redis/   # Redis
│   ├── Mvp24Hours.Infrastructure.RabbitMQ/        # RabbitMQ
│   ├── Mvp24Hours.Infrastructure.Pipe/            # Pipeline
│   ├── Mvp24Hours.Infrastructure.CronJob/         # CronJob
│   ├── Mvp24Hours.WebAPI/                         # Extensões Web API
│   └── Tests/                              # Testes unitários e integração
├── docs/                                   # Documentação
├── CHANGELOG.md                            # Histórico de mudanças
├── CONTRIBUTING.md                         # Este arquivo
└── README.md                               # Readme principal
```

## 🔨 Processo de Desenvolvimento

### 1. Escolha uma Issue

Navegue pelas [issues abertas](https://github.com/kallebelins/mvp24hours-dotnet/issues) e escolha uma que interesse você.

Issues marcadas com `good first issue` são ideais para iniciantes.

### 2. Crie uma Branch

```bash
# Atualize seu fork
git checkout main
git pull upstream main

# Crie uma nova branch
git checkout -b feature/minha-nova-funcionalidade
# ou
git checkout -b fix/correcao-do-bug
```

**Convenção de nomenclatura de branches:**
- `feature/` - Para novas funcionalidades
- `fix/` - Para correções de bugs
- `docs/` - Para mudanças na documentação
- `refactor/` - Para refatorações
- `test/` - Para adição/correção de testes
- `perf/` - Para melhorias de performance

### 3. Faça suas Alterações

- Escreva código limpo e bem documentado
- Siga os [padrões de código](#padrões-de-código)
- Adicione testes para novas funcionalidades
- Atualize a documentação se necessário

### 4. Teste suas Alterações

```bash
# Execute todos os testes
dotnet test

# Execute testes de um projeto específico
dotnet test src/Tests/Mvp24Hours.Core.Test/

# Execute com cobertura de código
dotnet test /p:CollectCoverage=true
```

### 5. Commit suas Alterações

Veja [Commits e Pull Requests](#commits-e-pull-requests) para convenções.

### 6. Push para seu Fork

```bash
git push origin feature/minha-nova-funcionalidade
```

### 7. Abra um Pull Request

- Vá para o repositório original no GitHub
- Clique em "Pull Request"
- Selecione sua branch
- Preencha o template de PR com detalhes
- Aguarde a revisão

## 📝 Padrões de Código

### C# Style Guide

Seguimos as [convenções de código do C#](https://docs.microsoft.com/pt-br/dotnet/csharp/fundamentals/coding-style/coding-conventions) da Microsoft.

#### Principais Regras:

1. **Nomenclatura**
   ```csharp
   // Classes, métodos e propriedades: PascalCase
   public class CustomerService { }
   public string FirstName { get; set; }
   public void GetCustomer() { }
   
   // Variáveis locais e parâmetros: camelCase
   var customerId = 1;
   public void Add(Customer customer) { }
   
   // Interfaces: começam com 'I'
   public interface IRepository<T> { }
   
   // Constantes: PascalCase
   public const int MaxRetries = 3;
   
   // Campos privados: começam com '_'
   private readonly ILogger _logger;
   ```

2. **Indentação e Formatação**
   ```csharp
   // Use 4 espaços para indentação (não tabs)
   // Chaves em nova linha (Allman style)
   public void MyMethod()
   {
       if (condition)
       {
           // código
       }
   }
   ```

3. **XML Documentation**
   ```csharp
   /// <summary>
   /// Retrieves a customer by ID.
   /// </summary>
   /// <param name="id">The customer identifier.</param>
   /// <returns>The customer if found; otherwise, null.</returns>
   /// <exception cref="ArgumentException">Thrown when id is invalid.</exception>
   public Customer GetById(int id)
   {
       // implementação
   }
   ```

4. **Async/Await**
   ```csharp
   // Sempre use 'Async' no nome de métodos assíncronos
   public async Task<Customer> GetCustomerAsync(int id)
   {
       return await repository.GetByIdAsync(id);
   }
   
   // Use ConfigureAwait(false) em bibliotecas
   var result = await operation.ExecuteAsync().ConfigureAwait(false);
   ```

5. **Tratamento de Erros**
   ```csharp
   // Use exceções customizadas da hierarquia Mvp24Hours
   if (customer == null)
   {
       throw new DataException(
           "Customer not found",
           "CUSTOMER_NOT_FOUND",
           new Dictionary<string, object> { ["CustomerId"] = id }
       );
   }
   ```

6. **SOLID Principles**
   - **S**ingle Responsibility Principle
   - **O**pen/Closed Principle
   - **L**iskov Substitution Principle
   - **I**nterface Segregation Principle
   - **D**ependency Inversion Principle

### Análise Estática

Execute análise estática antes de commitar:

```bash
# Verificar problemas de código
dotnet format --verify-no-changes

# Formatar automaticamente
dotnet format
```

## 💬 Commits e Pull Requests

### Mensagens de Commit

Seguimos o padrão [Conventional Commits](https://www.conventionalcommits.org/pt-br/).

**Formato:**
```
<tipo>[escopo opcional]: <descrição>

[corpo opcional]

[rodapé opcional]
```

**Tipos:**
- `feat`: Nova funcionalidade
- `fix`: Correção de bug
- `docs`: Mudanças na documentação
- `style`: Formatação, ponto e vírgula, etc (sem mudança de código)
- `refactor`: Refatoração de código
- `perf`: Melhoria de performance
- `test`: Adição ou correção de testes
- `chore`: Mudanças no processo de build, ferramentas, etc

**Exemplos:**
```bash
# Feature simples
git commit -m "feat: add pagination support to repository"

# Fix com escopo
git commit -m "fix(efcore): resolve null reference in GetById method"

# Com corpo detalhado
git commit -m "feat(rabbitmq): implement dead letter queue

- Add configuration for DLQ
- Implement retry mechanism
- Add tests for DLQ flow

Closes #123"

# Breaking change
git commit -m "feat!: change IRepository signature

BREAKING CHANGE: GetById now returns Task<T> instead of T"
```

### Template de Pull Request

Ao abrir um PR, preencha o template com:

```markdown
## Descrição
[Descreva suas mudanças aqui]

## Tipo de Mudança
- [ ] 🐛 Bug fix (non-breaking change)
- [ ] ✨ Nova funcionalidade (non-breaking change)
- [ ] 💥 Breaking change (fix ou feature que causa mudanças incompatíveis)
- [ ] 📝 Documentação
- [ ] 🎨 Refatoração

## Checklist
- [ ] Meu código segue os padrões do projeto
- [ ] Realizei self-review do código
- [ ] Comentei áreas complexas do código
- [ ] Atualizei a documentação
- [ ] Não adicionei warnings
- [ ] Adicionei testes que provam que minha correção/funcionalidade funciona
- [ ] Testes unitários novos e existentes passam localmente
- [ ] Atualizei o CHANGELOG.md

## Como Testar?
[Descreva os passos para testar suas mudanças]

## Screenshots (se aplicável)
[Adicione screenshots se houver mudanças visuais]

## Issues Relacionadas
Closes #[número da issue]
```

## 🐛 Reportando Bugs

Antes de reportar um bug, verifique se já não existe uma issue aberta sobre o problema.

### Como Reportar um Bug

1. Vá para [Issues](https://github.com/kallebelins/mvp24hours-dotnet/issues/new?template=bug_report.md)
2. Use o template de bug report
3. Preencha todas as seções

**Informações Essenciais:**

- **Título claro e descritivo**
- **Descrição detalhada** do problema
- **Passos para reproduzir** o comportamento
- **Comportamento esperado** vs **comportamento atual**
- **Screenshots** (se aplicável)
- **Ambiente:**
  - Versão do Mvp24Hours
  - Versão do .NET
  - SO (Windows, Linux, macOS)
  - IDE e versão
- **Logs e stack traces** relevantes
- **Código de exemplo** que reproduz o problema

### Exemplo de Bug Report

```markdown
**Descrição do Bug**
GetById retorna null mesmo quando o registro existe no banco.

**Para Reproduzir**
1. Configure DbContext com SQL Server
2. Adicione um Customer
3. Chame `repository.GetById(1)`
4. Retorna null

**Comportamento Esperado**
Deveria retornar o customer com ID 1.

**Ambiente**
- Mvp24Hours: 9.1.x
- .NET: 9
- SO: Windows 11
- SQL Server: 2022

**Código para Reproduzir**
\```csharp
var customer = new Customer { Name = "Test" };
repository.Add(customer);
unitOfWork.SaveChanges();

var retrieved = repository.GetById(1); // Retorna null
\```
```

## 💡 Sugerindo Melhorias

Sugestões de melhorias são sempre bem-vindas!

### Como Sugerir uma Melhoria

1. Vá para [Issues](https://github.com/kallebelins/mvp24hours-dotnet/issues/new?template=feature_request.md)
2. Use o template de feature request
3. Descreva sua sugestão detalhadamente

**Informações Importantes:**

- **Problema que a feature resolve**
- **Solução proposta** detalhada
- **Alternativas consideradas**
- **Exemplos de uso**
- **Impacto em breaking changes**
- **Benefícios para a comunidade**

### Exemplo de Feature Request

```markdown
**Sua feature está relacionada a um problema?**
Sim, atualmente não há suporte para bulk operations eficientes.

**Descreva a solução que você gostaria**
Adicionar métodos BulkInsert, BulkUpdate e BulkDelete ao IRepository.

**Exemplo de Uso**
\```csharp
var customers = GetLargeCustomerList();
repository.BulkInsert(customers); // Insere milhares de registros eficientemente
unitOfWork.SaveChanges();
\```

**Alternativas Consideradas**
- Usar loop com Add() - muito lento para grandes volumes
- Usar SQL direto - perde abstração do Repository

**Benefícios**
- Melhora significativa de performance em cenários com muitos registros
- Mantém a abstração do Repository Pattern
- Facilita operações em batch
```

## 📚 Documentação

A documentação é tão importante quanto o código!

### Tipos de Documentação

1. **XML Comments** - Para IntelliSense
   ```csharp
   /// <summary>
   /// Retrieves entities with pagination.
   /// </summary>
   ```

2. **README** - Para visão geral de módulos

3. **Docs** - Guias detalhados em `docs/`
   - Tutoriais
   - Exemplos práticos
   - Arquitetura
   - Melhores práticas

4. **CHANGELOG** - Histórico de mudanças

### Como Contribuir com Documentação

1. Melhore documentação existente
2. Adicione exemplos práticos
3. Corrija erros de ortografia/gramática
4. Traduza documentação
5. Crie tutoriais em vídeo/blog

**Dica:** Documentação pode ser um ótimo primeiro PR!

## 🧪 Testes

Testes são obrigatórios para novas funcionalidades e correções de bugs.

### Estrutura de Testes

```
Tests/
├── Mvp24Hours.Core.Test/              # Testes do Core
├── Mvp24Hours.Application.Test/       # Testes da Application
├── Mvp24Hours.Application.SQLServer.Test/  # Testes de integração SQL
├── Mvp24Hours.Application.MongoDb.Test/    # Testes de integração Mongo
└── ...
```

### Escrevendo Testes

```csharp
using Xunit;

namespace Mvp24Hours.Core.Test
{
    public class RepositoryTests
    {
        [Fact]
        public void Add_ValidEntity_ShouldAddToContext()
        {
            // Arrange
            var repository = CreateRepository();
            var customer = new Customer { Name = "Test" };
            
            // Act
            repository.Add(customer);
            var result = repository.GetById(customer.Id);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
        }
        
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void GetById_InvalidId_ShouldReturnNull(int invalidId)
        {
            // Arrange
            var repository = CreateRepository();
            
            // Act
            var result = repository.GetById(invalidId);
            
            // Assert
            Assert.Null(result);
        }
    }
}
```

### Rodando Testes

```bash
# Todos os testes
dotnet test

# Apenas um projeto
dotnet test src/Tests/Mvp24Hours.Core.Test/

# Com cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Apenas uma categoria
dotnet test --filter Category=Integration
```

### Cobertura de Código

Buscamos manter **cobertura mínima de 80%** em novos códigos.

```bash
# Gerar relatório de cobertura
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./coverage/
```

## 🌍 Comunidade

### Onde Obter Ajuda

- 📖 [Documentação](https://kallebelins.github.io/mvp24hours-dotnet)
- 💬 [GitHub Discussions](https://github.com/kallebelins/mvp24hours-dotnet/discussions)
- 🐛 [GitHub Issues](https://github.com/kallebelins/mvp24hours-dotnet/issues)
- 💼 [LinkedIn - Kallebe Lins](https://www.linkedin.com/in/kallebelins/)

### Canais de Comunicação

- **GitHub Issues** - Para bugs e feature requests
- **GitHub Discussions** - Para perguntas e discussões gerais
- **Pull Requests** - Para contribuições de código

### Reconhecimento

Todos os contribuidores são reconhecidos:

- Lista de contribuidores no README
- Menção nos release notes
- Badge de contribuidor no GitHub

## 📜 Licença

Ao contribuir com o Mvp24Hours, você concorda que suas contribuições serão licenciadas sob a [Licença MIT](LICENSE).

## 🎯 Roadmap

Veja o [roadmap de tarefas](docs/tasks.md) para saber o que está planejado:

- 156 tarefas organizadas
- Categorizadas por prioridade
- Abrange código, testes e documentação

### Tarefas Prioritárias

Consulte [docs/tasks.md](docs/tasks.md) para a lista completa, mas algumas prioridades atuais:

1. ⚡ Implementar guard clauses consistentes
2. ⚡ Adicionar testes unitários para Extension Methods
3. ⚡ Configurar code coverage reporting
4. ⚡ Revisar e otimizar implementações async

## 🙏 Agradecimentos

Obrigado por tornar o Mvp24Hours melhor! Cada contribuição, por menor que seja, faz diferença.

Algumas formas de ajudar além de código:

- ⭐ Dê uma estrela no repositório
- 📢 Compartilhe o projeto
- 📝 Escreva sobre o projeto
- 🐛 Reporte bugs
- 💡 Sugira melhorias
- 👥 Ajude outros usuários
- 📖 Melhore a documentação

---

**Dúvidas?** Abra uma [Discussion](https://github.com/kallebelins/mvp24hours-dotnet/discussions) ou entre em contato via [LinkedIn](https://www.linkedin.com/in/kallebelins/).

**Pronto para contribuir?** Comece escolhendo uma [issue com label "good first issue"](https://github.com/kallebelins/mvp24hours-dotnet/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22)!

Desenvolvido com ❤️ por [Kallebe Lins](https://github.com/kallebelins).

**Seja o primeiro contribuidor!** 🎉

