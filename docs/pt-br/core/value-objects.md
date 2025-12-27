# Value Objects

Value Objects são objetos imutáveis que representam conceitos de domínio através de seus atributos em vez de identidade. O módulo Mvp24Hours.Core fornece Value Objects prontos para uso em cenários comuns.

## Value Objects Disponíveis

| Value Object | Propósito |
|-------------|-----------|
| `Email` | Endereços de email com validação |
| `Cpf` | CPF brasileiro |
| `Cnpj` | CNPJ brasileiro |
| `Money` | Valores monetários com moeda |
| `Address` | Endereços físicos |
| `DateRange` | Intervalos de data/hora |
| `Percentage` | Valores percentuais |
| `PhoneNumber` | Números de telefone internacionais |

---

## Email

Representa um endereço de email validado.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Criar com validação
var email = Email.Create("usuario@exemplo.com");

// Propriedades
Console.WriteLine(email.Value);     // usuario@exemplo.com
Console.WriteLine(email.LocalPart); // usuario
Console.WriteLine(email.Domain);    // exemplo.com

// Parse seguro
if (Email.TryParse("usuario@exemplo.com", out var resultado))
{
    Console.WriteLine($"Válido: {resultado.Value}");
}

// Apenas validação
bool valido = Email.IsValid("usuario@exemplo.com");

// Conversão implícita para string
string emailStr = email;

// Conversão explícita de string
Email email2 = (Email)"outro@exemplo.com";
```

---

## Cpf

Representa um CPF brasileiro (Cadastro de Pessoa Física) com validação.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Criar com validação (aceita formatado ou não formatado)
var cpf = Cpf.Create("123.456.789-09");
var cpf2 = Cpf.Create("12345678909");

// Propriedades
Console.WriteLine(cpf.Value);       // 12345678909 (não formatado)
Console.WriteLine(cpf.Formatted);   // 123.456.789-09
Console.WriteLine(cpf.Unformatted); // 12345678909

// Parse seguro
if (Cpf.TryParse("123.456.789-09", out var resultado))
{
    Console.WriteLine($"CPF válido: {resultado.Formatted}");
}

// Apenas validação
bool valido = Cpf.IsValid("123.456.789-09");
```

**Nota**: A validação inclui verificação dos dígitos verificadores usando o algoritmo oficial do CPF.

---

## Cnpj

Representa um CNPJ brasileiro (Cadastro Nacional da Pessoa Jurídica) com validação.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Criar com validação
var cnpj = Cnpj.Create("12.345.678/0001-95");
var cnpj2 = Cnpj.Create("12345678000195");

// Propriedades
Console.WriteLine(cnpj.Value);       // 12345678000195
Console.WriteLine(cnpj.Formatted);   // 12.345.678/0001-95
Console.WriteLine(cnpj.Unformatted); // 12345678000195

// Parse seguro
if (Cnpj.TryParse("12.345.678/0001-95", out var resultado))
{
    Console.WriteLine($"CNPJ válido: {resultado.Formatted}");
}

// Apenas validação
bool valido = Cnpj.IsValid("12.345.678/0001-95");
```

---

## Money

Representa valores monetários com suporte a moeda.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Criar com valor e moeda
var preco = Money.Create(99.99m, "BRL");
var precoEmDolar = Money.Create(19.99m, "USD");

// Propriedades
Console.WriteLine(preco.Amount);   // 99.99
Console.WriteLine(preco.Currency); // BRL

// Operações aritméticas
var dobrado = preco * 2;           // R$ 199,98
var comDesconto = preco - 10m;     // R$ 89,99
var total = preco + preco;         // R$ 199,98

// Comparação
bool maisCaro = preco > Money.Create(50m, "BRL"); // true

// Formatação
Console.WriteLine(preco.ToString()); // BRL 99.99

// Criar valor zero
var zero = Money.Zero("BRL");
```

**Importante**: Operações aritméticas requerem a mesma moeda:

```csharp
var brl = Money.Create(100m, "BRL");
var usd = Money.Create(100m, "USD");

// Isso lança InvalidOperationException:
// var invalido = brl + usd;
```

---

## Address

Representa um endereço físico.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Criar um endereço completo
var endereco = Address.Create(
    street: "Rua das Flores",
    number: "123",
    complement: "Apto 45",
    neighborhood: "Centro",
    city: "São Paulo",
    state: "SP",
    zipCode: "01310-100",
    country: "Brasil"
);

// Propriedades
Console.WriteLine(endereco.Street);       // Rua das Flores
Console.WriteLine(endereco.Number);       // 123
Console.WriteLine(endereco.City);         // São Paulo
Console.WriteLine(endereco.State);        // SP
Console.WriteLine(endereco.ZipCode);      // 01310-100
Console.WriteLine(endereco.Country);      // Brasil
Console.WriteLine(endereco.FullAddress);  // Endereço completo formatado
```

---

## DateRange

Representa um intervalo entre duas datas.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Criar um intervalo de datas
var intervalo = DateRange.Create(
    new DateTime(2024, 1, 1),
    new DateTime(2024, 12, 31)
);

// Propriedades
Console.WriteLine(intervalo.Start);    // 2024-01-01
Console.WriteLine(intervalo.End);      // 2024-12-31
Console.WriteLine(intervalo.Duration); // TimeSpan

// Verificar se uma data está no intervalo
var dataTest = new DateTime(2024, 6, 15);
bool contem = intervalo.Contains(dataTest); // true

// Verificar se intervalos se sobrepõem
var outro = DateRange.Create(
    new DateTime(2024, 6, 1),
    new DateTime(2025, 1, 31)
);
bool sobrepoe = intervalo.Overlaps(outro); // true

// Criar a partir de duração
var semana = DateRange.FromDuration(DateTime.Today, TimeSpan.FromDays(7));
```

---

## Percentage

Representa valores percentuais com suporte a conversão.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Criar a partir de porcentagem (0-100)
var desconto = Percentage.FromPercent(25); // 25%

// Criar a partir de decimal (0-1)
var taxa = Percentage.FromDecimal(0.25m); // 25%

// Propriedades
Console.WriteLine(desconto.Value);     // 0.25 (representação decimal)
Console.WriteLine(desconto.AsPercent); // 25 (representação percentual)

// Aplicar a um valor
decimal preco = 100m;
decimal valorDesconto = desconto.Apply(preco); // 25

// Aritmética
var total = desconto + Percentage.FromPercent(10); // 35%
var metade = desconto / 2; // 12.5%

// Validação
bool valido = Percentage.IsValid(150); // false (> 100%)
```

---

## PhoneNumber

Representa números de telefone internacionais.

```csharp
using Mvp24Hours.Core.ValueObjects;

// Criar com validação
var telefone = PhoneNumber.Create("+55", "11", "999887766");

// Propriedades
Console.WriteLine(telefone.CountryCode); // +55
Console.WriteLine(telefone.AreaCode);    // 11
Console.WriteLine(telefone.Number);      // 999887766
Console.WriteLine(telefone.E164Format);  // +5511999887766

// Formato americano
var americano = PhoneNumber.Create("+1", "555", "1234567");
Console.WriteLine(americano.E164Format); // +15551234567
```

---

## Criando Value Objects Customizados

Estenda `BaseVO` para criar seus próprios Value Objects:

```csharp
using Mvp24Hours.Core.ValueObjects;

public sealed class CodigoProduto : BaseVO, IEquatable<CodigoProduto>
{
    public string Categoria { get; }
    public int Sequencia { get; }
    public string Value => $"{Categoria}-{Sequencia:D4}";
    
    private CodigoProduto(string categoria, int sequencia)
    {
        Categoria = categoria;
        Sequencia = sequencia;
    }
    
    public static CodigoProduto Create(string categoria, int sequencia)
    {
        Guard.Against.NullOrEmpty(categoria, nameof(categoria));
        Guard.Against.NegativeOrZero(sequencia, nameof(sequencia));
        Guard.Against.LengthOutOfRange(categoria, 2, 2, nameof(categoria));
        
        return new CodigoProduto(categoria.ToUpper(), sequencia);
    }
    
    public static bool TryParse(string valor, out CodigoProduto resultado)
    {
        resultado = null!;
        if (string.IsNullOrEmpty(valor)) return false;
        
        var partes = valor.Split('-');
        if (partes.Length != 2) return false;
        if (!int.TryParse(partes[1], out var sequencia)) return false;
        
        try
        {
            resultado = Create(partes[0], sequencia);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Categoria;
        yield return Sequencia;
    }
    
    public bool Equals(CodigoProduto? other) => base.Equals(other);
    
    public override string ToString() => Value;
    
    public static implicit operator string(CodigoProduto codigo) => codigo.Value;
}
```

Uso:

```csharp
var codigo = CodigoProduto.Create("AB", 123);
Console.WriteLine(codigo.Value); // AB-0123

if (CodigoProduto.TryParse("XY-0456", out var parsed))
{
    Console.WriteLine(parsed.Categoria); // XY
}
```

---

## Boas Práticas

1. **Use Value Objects para conceitos de domínio** - Email, Money, CPF são conceitos de domínio, não apenas strings/decimals
2. **Valide na criação** - Toda validação acontece no `Create` ou construtor
3. **Torne-os imutáveis** - Value Objects nunca devem mudar após a criação
4. **Implemente igualdade** - Dois Value Objects com mesmos valores devem ser iguais
5. **Forneça `TryParse`** - Sempre ofereça parse seguro para entrada do usuário

