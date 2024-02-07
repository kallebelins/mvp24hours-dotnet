# How to implement entities?
An entity can be any class that has few properties with a unique ID.

## Basic Configuration
```csharp
public class MyEntity : IEntityBase
{
    public object EntityKey => this.MyId; // returns class identifier (ID)
}
```

It is worth remembering that the entity loaded in the context of the Entity Framework must have some characteristics:
```csharp
public class MyEntity : IEntityBase
{
    [JsonIgnore] // prevents duplicating the key if serialized
    [IgnoreDataMember] // prevents sending column to the database and generating exceptions
    public object EntityKey => this.MyId;

    [Key] // indicates that this property represents the entity identifier
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // automatically generates value for the column at the time of persistence
    public int Id { get; set; }

    [...]
}
```

You can create a base class or use the one from the architecture:
```csharp
public class MyEntity : EntityBase<int> // ID column data type
{
    [...]
}
```

## Configuration with Log
We have two dynamic implementations for entity update logging through context. To reference record update date and time, use:
```csharp
public class MyEntityWithLog : EntityBase<int>, IEntityDateLog
{
    [Required]
    public DateTime Created { get; set; }

    public DateTime? Modified { get; set; }

    public DateTime? Removed { get; set; }
}
```

If you need to update the record of the user who performed the action:
```csharp
public class MyEntityWithLog : EntityBase<int>, IEntityLog<int?> // user id column data type
{
    public DateTime Created { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? Modified { get; set; }
    public int ModifiedBy { get; set; }
    public DateTime? Removed { get; set; }
    public int RemovedBy { get; set; }
}
```

You can create a base class or use the one from the architecture:
```csharp
public class MyEntityWithLog : EntityBaseLog<int, int?> // column data type id and user id
{
    [...]
}
```

## Common Question
But how will I use these columns with already defined names? Simple. You can configure the column name in the database via Fluent or Data Annotation.