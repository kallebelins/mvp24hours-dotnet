# Política de Segurança

## 🛡️ Versões Suportadas

Nós fornecemos atualizações de segurança para as seguintes versões do Mvp24Hours:

| Versão | Suportada          | Suporte até       |
| ------ | ------------------ | ----------------- |
| 9.1.x  | ✅ Sim             | Atual             |
| 9.0.x  | ✅ Sim             | Jun 2027          |
| 8.3.x  | ⚠️ Limitado        | Dez 2026          |
| 8.2.x  | ❌ Não             | EOL               |
| < 8.2  | ❌ Não             | EOL               |

**Recomendação:** Sempre use a versão mais recente para garantir que você tenha as correções de segurança mais atualizadas.

## 🔒 Reportando uma Vulnerabilidade

A segurança do Mvp24Hours é uma prioridade. Se você descobrir uma vulnerabilidade de segurança, por favor **NÃO** abra uma issue pública.

### Processo de Reporte

1. **📧 Envie um email para:** [kallebe.santos@outlook.com]
   
   Inclua as seguintes informações:
   - Descrição detalhada da vulnerabilidade
   - Passos para reproduzir o problema
   - Versões afetadas
   - Potencial impacto
   - Sugestões de correção (se houver)

2. **⏱️ Tempo de Resposta:**
   - Confirmaremos o recebimento em até 48 horas
   - Avaliaremos a vulnerabilidade em até 7 dias
   - Manteremos você informado sobre o progresso

3. **🔍 Avaliação:**
   - Verificaremos e validaremos o relatório
   - Determinaremos a severidade (Critical, High, Medium, Low)
   - Desenvolveremos uma correção

4. **🚀 Divulgação:**
   - Lançaremos uma correção
   - Publicaremos um aviso de segurança
   - Creditaremos você (se desejar) na descoberta

### Severidade das Vulnerabilidades

Classificamos vulnerabilidades usando o [CVSS v3.1](https://www.first.org/cvss/):

- **🔴 Critical (9.0-10.0):** Exploração remota sem autenticação
- **🟠 High (7.0-8.9):** Comprometimento significativo de dados/sistema
- **🟡 Medium (4.0-6.9):** Acesso limitado a informações sensíveis
- **🟢 Low (0.1-3.9):** Impacto mínimo de segurança

## 🎯 Escopo de Segurança

### O Que Está No Escopo

Vulnerabilidades relacionadas a:

- ✅ Injeção (SQL, NoSQL, Command, etc)
- ✅ Quebra de autenticação e autorização
- ✅ Exposição de dados sensíveis
- ✅ XXE (XML External Entities)
- ✅ Controle de acesso quebrado
- ✅ Configuração de segurança incorreta
- ✅ XSS (Cross-Site Scripting)
- ✅ Deserialização insegura
- ✅ Componentes com vulnerabilidades conhecidas
- ✅ Logging e monitoramento insuficientes
- ✅ CSRF (Cross-Site Request Forgery)
- ✅ Path traversal
- ✅ Denial of Service (DoS)

### O Que NÃO Está No Escopo

- ❌ Problemas de usabilidade
- ❌ Bugs que não têm impacto de segurança
- ❌ Vulnerabilidades em dependências de terceiros (reporte diretamente aos mantenedores)
- ❌ Ataques de engenharia social
- ❌ Ataques físicos

## 🔐 Melhores Práticas de Segurança

### Para Usuários da Biblioteca

#### 1. Validação de Entrada
```csharp
// ✅ BOM: Use validação adequada
public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(150);
    }
}
```

#### 2. Parameterização de Queries
```csharp
// ✅ BOM: Use Repository pattern (automático no Mvp24Hours)
var customers = repository.GetBy(c => c.Name == userName);

// ❌ MAL: Concatenação de strings (evite!)
// var sql = $"SELECT * FROM Customers WHERE Name = '{userName}'";
```

#### 3. Tratamento de Exceções
```csharp
// ✅ BOM: Não exponha detalhes internos
try
{
    // operação
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed");
    return new MessageResult("An error occurred")
        .ToBusiness<Customer>();
}

// ❌ MAL: Expor stack trace ao cliente
// throw new Exception(ex.ToString());
```

#### 4. Configuração Segura
```csharp
// ✅ BOM: Use User Secrets em desenvolvimento
// dotnet user-secrets set "ConnectionStrings:Default" "..."

// ✅ BOM: Use variáveis de ambiente em produção
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

// ❌ MAL: Hard-coded secrets
// var connectionString = "Server=...;Password=secret123";
```

#### 5. HTTPS e Criptografia
```csharp
// ✅ BOM: Force HTTPS em produção
app.UseHttpsRedirection();
app.UseHsts();

// ✅ BOM: Criptografe dados sensíveis
// Use Data Protection API do ASP.NET Core
```

#### 6. Auditoria e Logging
```csharp
// ✅ BOM: Use recursos de auditoria do Mvp24Hours
public class Customer : EntityBaseLog<int, string>
{
    // Automatic Created, Modified, Removed tracking
}

// ✅ BOM: Log operações sensíveis
_logger.LogInformation(
    "User {UserId} accessed customer {CustomerId}",
    userId, customerId
);
```

### Para Contribuidores

1. **Nunca comite secrets:** Use .gitignore para excluir arquivos sensíveis
2. **Revise dependências:** Verifique vulnerabilidades conhecidas
3. **Valide entrada:** Sempre valide e sanitize entrada do usuário
4. **Use async seguramente:** Evite race conditions
5. **Teste segurança:** Inclua testes de segurança em PRs

## 📋 Checklist de Segurança

Antes de fazer deploy em produção:

- [ ] Todas as dependências estão atualizadas
- [ ] Secrets não estão no código ou configuração
- [ ] HTTPS está habilitado
- [ ] Validação de entrada está implementada
- [ ] Logs não contêm informações sensíveis
- [ ] Tratamento de erros não expõe detalhes internos
- [ ] Autenticação e autorização estão configuradas
- [ ] Auditoria está habilitada
- [ ] Backups estão configurados
- [ ] Monitoramento está ativo

## 🔄 Atualizações de Segurança

### Como Nos Mantemos Seguros

1. **Monitoramento:** Monitoramos continuamente vulnerabilidades
2. **Scans Automáticos:** GitHub Dependabot e CodeQL
3. **Revisão de Código:** Todos os PRs passam por revisão
4. **Testes:** Suite de testes automáticos
5. **Atualizações:** Patch releases regulares

### Como Manter-se Atualizado

- ⭐ **Watch** o repositório no GitHub
- 📧 Ative notificações de releases
- 📖 Leia o [CHANGELOG.md](CHANGELOG.md)
- 🔔 Siga [@kallebelins](https://linkedin.com/in/kallebelins) no LinkedIn

## 🏆 Hall da Fama de Segurança

Agradeceremos aos pesquisadores que reportarem vulnerabilidades responsavelmente.

<!-- 
Contribuidores de segurança serão listados aqui após a divulgação:

### 2025
- [Nome] - [Descrição da vulnerabilidade]
-->

**Nenhuma vulnerabilidade reportada ainda.** 

O projeto está mantido com práticas de segurança desde o início. Se você encontrar alguma vulnerabilidade, será o primeiro a ser reconhecido! 🎯

## 📚 Recursos Adicionais

### Segurança em .NET
- [Guia de Segurança ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [.NET Security Guidelines](https://docs.microsoft.com/dotnet/standard/security/)

### Segurança em Bancos de Dados
- [SQL Injection Prevention](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html)
- [MongoDB Security Checklist](https://docs.mongodb.com/manual/administration/security-checklist/)

### Ferramentas
- [Snyk](https://snyk.io/) - Vulnerability scanning
- [OWASP Dependency-Check](https://owasp.org/www-project-dependency-check/)
- [WhiteSource](https://www.whitesourcesoftware.com/)

## 📞 Contato

Para questões relacionadas a segurança:

- **Email:** [kallebe.santos@outlook.com]
- **LinkedIn:** [Kallebe Lins](https://linkedin.com/in/kallebelins)
- **PGP Key:** [Adicione sua chave PGP aqui se houver]

---

**Obrigado por ajudar a manter o Mvp24Hours seguro! 🛡️**

*Última atualização: Janeiro 2026*

