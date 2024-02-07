# Como implementar entidades?
Uma entidade pode ser qualquer classe que possua poucas propriedades com um ID exclusivo.

## Configuração Básica
```csharp
public class MyEntity : IEntityBase
{
    public object EntityKey => this.MyId; // retorna identificador da classe (ID)
}
```

Vale lembrar que a entidade carregada no contexto do Entity Framework deverá possuir algumas caracaterísticas:
```csharp
public class MyEntity : IEntityBase
{
    [JsonIgnore] // previne de duplicar a chave caso serializada
    [IgnoreDataMember] // previne de enviar coluna para o banco e gerar exceções
    public object EntityKey => this.MyId;

    [Key] // indica que essa propriedade representa o identificador da entidade
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // gera valor automáticamente para a coluna no momento da persistência
    public int Id { get; set; }

    [...]
}
```

Você pode criar uma classe base ou usar a da arquitetura:
```csharp
public class MyEntity : EntityBase<int> // tipo dado da coluna ID
{
    [...]
}
```

## Configuração Com Log
Temos duas implementações dinâmicas para log de atualização de entidades através do contexto. Para usar referência de data e hora de atualização de registro, use:
```csharp
public class MyEntityWithLog : EntityBase<int>, IEntityDateLog
{
    [Required]
    public DateTime Created { get; set; }

    public DateTime? Modified { get; set; }

    public DateTime? Removed { get; set; }
}
```

Caso tenha a necessidade de atualizar o registro do usuário que realizou a ação:
```csharp
public class MyEntityWithLog : EntityBase<int>, IEntityLog<int?> // tipo de dado da coluna de usuário id
{
    public DateTime Created { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? Modified { get; set; }
    public int ModifiedBy { get; set; }
    public DateTime? Removed { get; set; }
    public int RemovedBy { get; set; }
}
```

Você pode criar uma classe base ou usar a da arquitetura:
```csharp
public class MyEntityWithLog : EntityBaseLog<int, int?> // tipo de dado da coluna id e de usuário id
{
    [...]
}
```

## Dúvida Comum
Mas como vou utilizar essas colunas com nome já definido? Simples. Você pode configurar o nome da coluna no banco de dados via Fluent ou Data Annotation.