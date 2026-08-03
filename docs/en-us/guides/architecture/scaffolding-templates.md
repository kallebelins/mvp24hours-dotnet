# Scaffolding templates

Compilable architecture scaffolds live in the repository [`templates/`](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates) folder. Copy a template, rename `App` / `Item`, and implement your domain.

**Samples teach** full Customer API scenarios. **Templates bootstrap** a new solution with a placeholder `Item` resource.

## Blueprints

| Template | Docs guide |
| --- | --- |
| [complex-nlayers](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/blueprints/complex-nlayers) | [Complex N-Layers](structures/structure-complex-nlayers.md) |
| [clean-architecture](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/blueprints/clean-architecture) | [Clean Architecture](blueprints/template-clean-architecture.md) |
| [hexagonal](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/blueprints/hexagonal) | [Hexagonal](blueprints/template-hexagonal.md) |
| [cqrs](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/blueprints/cqrs) | [CQRS](blueprints/template-cqrs.md) |
| [ddd](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/blueprints/ddd) | [DDD](blueprints/template-ddd.md) |
| [event-driven](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/blueprints/event-driven) | [Event-Driven](blueprints/template-event-driven.md) |

## Hosts

| Template | Purpose |
| --- | --- |
| [api-complex-nlayers](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/hosts/api-complex-nlayers) | Points at the Complex N-Layers blueprint |
| [bff-complex-nlayers](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/hosts/bff-complex-nlayers) | BFF API without required DbContext |
| [function-minimal](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/hosts/function-minimal) / [simple](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/hosts/function-simple) / [complex](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/hosts/function-complex) | Azure Functions Isolated Worker |
| [worker-minimal](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/hosts/worker-minimal) / [simple](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/hosts/worker-simple) / [complex](https://github.com/kallebelins/mvp24hours-dotnet/tree/main/templates/hosts/worker-complex) | CronJob / background worker |

## Build

```bash
cd templates
dotnet build Mvp24Hours.Templates.slnx --configuration Release
```

See the [templates README](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/templates/README.md) for the rename checklist and NuGet vs project-reference modes.
