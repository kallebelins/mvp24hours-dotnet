# Smart Enums (Padrão Enumeration)

Smart Enums estendem o conceito de enums tradicionais do C# permitindo comportamento associado, lógica de domínio rica e melhor suporte a ORM.

## Por Que Smart Enums?

Enums tradicionais têm limitações:

```csharp
// Enum tradicional - limitado
public enum StatusPedido
{
    Pendente = 1,
    Processando = 2,
    Enviado = 3,
    Entregue = 4,
    Cancelado = 5
}

// Como adicionar comportamento? Não dá para fazer isso:
// StatusPedido.Pendente.PodeCancelar(); // Não é possível
```

Com Smart Enums:

```csharp
// Smart Enum - comportamento rico
public class StatusPedido : Enumeration<StatusPedido>
{
    public static readonly StatusPedido Pendente = new(1, "Pendente");
    public static readonly StatusPedido Processando = new(2, "Processando");
    public static readonly StatusPedido Enviado = new(3, "Enviado");
    
    private StatusPedido(int value, string name) : base(value, name) { }
    
    public virtual bool PodeCancelar => this == Pendente || this == Processando;
}

// Agora você pode:
if (pedido.Status.PodeCancelar)
{
    pedido.Cancelar();
}
```

## Criando Smart Enums

### Estrutura Básica

```csharp
using Mvp24Hours.Core.Domain.Enumerations;

public class MetodoPagamento : Enumeration<MetodoPagamento>
{
    public static readonly MetodoPagamento CartaoCredito = new(1, "CartaoCredito");
    public static readonly MetodoPagamento CartaoDebito = new(2, "CartaoDebito");
    public static readonly MetodoPagamento Transferencia = new(3, "Transferencia");
    public static readonly MetodoPagamento Pix = new(4, "Pix");
    public static readonly MetodoPagamento Dinheiro = new(5, "Dinheiro");
    
    private MetodoPagamento(int value, string name) : base(value, name) { }
}
```

### Adicionando Comportamento

```csharp
public class MetodoPagamento : Enumeration<MetodoPagamento>
{
    public static readonly MetodoPagamento CartaoCredito = new(1, "CartaoCredito", true, 0.03m);
    public static readonly MetodoPagamento CartaoDebito = new(2, "CartaoDebito", true, 0.01m);
    public static readonly MetodoPagamento Transferencia = new(3, "Transferencia", false, 0m);
    public static readonly MetodoPagamento Pix = new(4, "Pix", true, 0m);
    public static readonly MetodoPagamento Dinheiro = new(5, "Dinheiro", false, 0m);
    
    public bool Instantaneo { get; }
    public decimal PercentualTaxa { get; }
    
    private MetodoPagamento(int value, string name, bool instantaneo, decimal taxa) 
        : base(value, name)
    {
        Instantaneo = instantaneo;
        PercentualTaxa = taxa;
    }
    
    public decimal CalcularTaxa(decimal valor) => valor * PercentualTaxa;
}

// Uso
var metodo = MetodoPagamento.CartaoCredito;
var taxa = metodo.CalcularTaxa(100m); // 3.00
Console.WriteLine(metodo.Instantaneo); // true
```

### Padrão State Machine

```csharp
public class StatusPedido : Enumeration<StatusPedido>
{
    public static readonly StatusPedido Rascunho = new(1, "Rascunho");
    public static readonly StatusPedido Pendente = new(2, "Pendente");
    public static readonly StatusPedido Confirmado = new(3, "Confirmado");
    public static readonly StatusPedido Processando = new(4, "Processando");
    public static readonly StatusPedido Enviado = new(5, "Enviado");
    public static readonly StatusPedido Entregue = new(6, "Entregue");
    public static readonly StatusPedido Cancelado = new(7, "Cancelado");
    
    private StatusPedido(int value, string name) : base(value, name) { }
    
    public virtual bool PodeTransicionarPara(StatusPedido novoStatus)
    {
        return this switch
        {
            _ when this == Rascunho => novoStatus == Pendente || novoStatus == Cancelado,
            _ when this == Pendente => novoStatus == Confirmado || novoStatus == Cancelado,
            _ when this == Confirmado => novoStatus == Processando || novoStatus == Cancelado,
            _ when this == Processando => novoStatus == Enviado,
            _ when this == Enviado => novoStatus == Entregue,
            _ => false
        };
    }
    
    public virtual bool PodeCancelar => this == Rascunho || this == Pendente || this == Confirmado;
    public virtual bool Terminal => this == Entregue || this == Cancelado;
}

// Uso
var atual = StatusPedido.Pendente;
var proximo = StatusPedido.Confirmado;

if (atual.PodeTransicionarPara(proximo))
{
    pedido.Status = proximo;
}
```

## Usando Smart Enums

### Busca por Valor

```csharp
// Obter por valor numérico
var status = StatusPedido.FromValue(1); // Rascunho

// Busca segura
if (StatusPedido.TryFromValue(99, out var resultado))
{
    // Não chega aqui - valor inválido
}
```

### Busca por Nome

```csharp
// Obter por nome (case-insensitive)
var status = StatusPedido.FromName("Pendente");
var status2 = StatusPedido.FromName("PENDENTE"); // Mesmo resultado

// Busca segura
if (StatusPedido.TryFromName("Desconhecido", out var resultado))
{
    // Não chega aqui
}
```

### Obter Todos os Valores

```csharp
// Obter todos os valores definidos
var todosStatus = StatusPedido.GetAll();

foreach (var status in todosStatus)
{
    Console.WriteLine($"{status.Value}: {status.Name}");
}

// Saída:
// 1: Rascunho
// 2: Pendente
// 3: Confirmado
// ...
```

### Verificar se Definido

```csharp
bool existe = StatusPedido.IsDefined(5);           // true (Enviado)
bool existePorNome = StatusPedido.IsDefined("Rascunho"); // true
```

### Comparações

```csharp
var pendente = StatusPedido.Pendente;
var confirmado = StatusPedido.Confirmado;

// Igualdade
bool igual = pendente == StatusPedido.Pendente; // true
bool diferente = pendente != confirmado;         // true

// Comparação (por Value)
bool menor = pendente < confirmado; // true (2 < 3)
```

### Conversões Implícitas

```csharp
var status = StatusPedido.Pendente;

// Para int
int valor = status; // 2

// Para string
string nome = status; // "Pendente"
```

## Integração com Entity Framework Core

### Configuração

```csharp
public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.Property(p => p.Status)
            .HasConversion(
                v => v.Value,                      // Para banco (int)
                v => StatusPedido.FromValue(v)    // Do banco
            );
    }
}
```

### Uso em Queries

```csharp
// Query por valor
var pedidosPendentes = await _context.Pedidos
    .Where(p => p.Status == StatusPedido.Pendente)
    .ToListAsync();

// Query por múltiplos status
var pedidosAtivos = await _context.Pedidos
    .Where(p => p.Status == StatusPedido.Pendente 
             || p.Status == StatusPedido.Processando)
    .ToListAsync();
```

## Serialização JSON

```csharp
// System.Text.Json
public class EnumerationJsonConverter<T> : JsonConverter<T> 
    where T : Enumeration<T>
{
    public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var valor = reader.GetInt32();
        return Enumeration<T>.FromValue(valor);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}
```

## Exemplos do Mundo Real

### Tipo de Documento

```csharp
public class TipoDocumento : Enumeration<TipoDocumento>
{
    public static readonly TipoDocumento Cpf = new(1, "CPF", 11, @"^\d{11}$");
    public static readonly TipoDocumento Cnpj = new(2, "CNPJ", 14, @"^\d{14}$");
    public static readonly TipoDocumento Rg = new(3, "RG", 9, @"^\d{9}$");
    
    public int Tamanho { get; }
    public string PatternValidacao { get; }
    
    private TipoDocumento(int value, string name, int tamanho, string pattern) 
        : base(value, name)
    {
        Tamanho = tamanho;
        PatternValidacao = pattern;
    }
    
    public bool Validar(string documento)
    {
        if (string.IsNullOrEmpty(documento)) return false;
        var limpo = Regex.Replace(documento, @"[^\d]", "");
        return Regex.IsMatch(limpo, PatternValidacao);
    }
}
```

### Canal de Notificação

```csharp
public class CanalNotificacao : Enumeration<CanalNotificacao>
{
    public static readonly CanalNotificacao Email = new(1, "Email", true);
    public static readonly CanalNotificacao Sms = new(2, "SMS", false);
    public static readonly CanalNotificacao Push = new(3, "Push", true);
    public static readonly CanalNotificacao WhatsApp = new(4, "WhatsApp", false);
    
    public bool SuportaConteudoRico { get; }
    
    private CanalNotificacao(int value, string name, bool suportaRico) 
        : base(value, name)
    {
        SuportaConteudoRico = suportaRico;
    }
}
```

## Boas Práticas

1. **Torne construtores privados** - Apenas os campos estáticos devem criar instâncias
2. **Use valores significativos** - A propriedade `Value` deve ser estável para armazenamento em banco
3. **Mantenha comportamento focado** - Não sobrecarregue com funcionalidades não relacionadas
4. **Considere serialização** - Planeje para armazenamento JSON/banco desde o início
5. **Use para conjuntos finitos e conhecidos** - Smart Enums funcionam melhor quando todos os valores são conhecidos em tempo de compilação

