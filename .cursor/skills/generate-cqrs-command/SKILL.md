---
name: generate-cqrs-command
description: >-
  Genera la estructura completa de un Command CQRS con MediatR y FluentValidation.
  Usar al crear comandos CQRS, validators, handlers MediatR o flujos de mutación en
  Resto.Application.
disable-model-invocation: true
---

# Generate CQRS Command

## Estructura Base

Al ejecutar esta habilidad, toma el nombre del comando solicitado y genera en la capa de Application:

1. Un `public record NameCommand(...) : IRequest<Result<Guid>>;` con sus propiedades inmutables.
2. Una clase `public class NameCommandValidator : AbstractValidator<NameCommand>` asegurando reglas estrictas para los IDs y datos obligatorios del restaurante.
3. Una clase `public class NameCommandHandler : IRequestHandler<NameCommand, Result<Guid>>` que inyecte las interfaces del repositorio correspondiente (ej. `IPedidoRepository`) y `IUnitOfWork`.
4. El Handler debe validar las reglas del Agregado de Dominio, invocar el método semántico del agregado, guardar los cambios y emitir el evento respectivo para SignalR.

## Workflow

1. **Analizar el comando solicitado**
   - Identificar el agregado raíz (`Pedido`, `Mesa`, etc.).
   - Identificar el repositorio (`IPedidoRepository`, `IMesaRepository`, etc.).
   - Identificar el método semántico del dominio (ej. `ConfirmForKitchen()`, `MarkAsReady()`).
   - Identificar el evento de dominio que notificará a SignalR tras el commit.

2. **Leer código existente antes de generar**
   - Buscar comandos similares en `Resto.Application/` y replicar convenciones.
   - Leer el agregado en `Resto.Domain/` para usar propiedades y métodos reales.
   - Leer interfaces en `Resto.Application/Common/Interfaces/`.

3. **Crear la carpeta del comando**
   ```
   Resto.Application/{Feature}/{CommandName}/
   ├── {CommandName}Command.cs
   ├── {CommandName}CommandValidator.cs
   └── {CommandName}CommandHandler.cs
   ```

4. **Generar los tres archivos** siguiendo las plantillas de abajo.

5. **Verificar** con el checklist al final.

## Convenciones del proyecto

| Elemento | Convención |
|----------|------------|
| Namespace | `Resto.Application.{Feature}.{CommandName}` |
| Archivos | `{Name}Command.cs`, `{Name}CommandValidator.cs`, `{Name}CommandHandler.cs` |
| Nomenclatura técnica | Inglés (clases, métodos, propiedades) |
| Mensajes de error | Español |
| Retorno | `Result<Guid>` |
| Concurrencia | Incluir `byte[] RowVersion` en comandos de `Pedido` y `Mesa` |

### Ciclo de vida del pedido (no desviar)

```
Borrador → ConfirmadoEnCocina → Listo | Finalizado
```

- Solo el mozo crea/edita en `Borrador`.
- Solo el encargado confirma envío a cocina y marca cierre.
- Prohibido: estados intermedios de cocina (`Cooking`, `InPreparation`, etc.).

### Separación de responsabilidades

| Capa | Responsabilidad |
|------|-----------------|
| **Validator** | Forma del input: GUIDs no vacíos, campos obligatorios, `RowVersion` presente |
| **Aggregate** | Reglas de negocio: transiciones de estado, invariantes |
| **Handler** | Orquestación: cargar → invocar método semántico → persistir → retornar |
| **Infrastructure** | `SaveChangesAsync`, despacho de eventos de dominio, SignalR, `DbUpdateConcurrencyException` |

**El handler NO llama SignalR directamente.** Los eventos de dominio se despachan tras confirmar el `UnitOfWork`.

## Plantillas

### Command

```csharp
namespace Resto.Application.{Feature}.{CommandName};

public sealed record {CommandName}Command(
    Guid {Entity}Id,
    byte[] RowVersion  // obligatorio para Pedido/Mesa
) : IRequest<Result<Guid>>;
```

Ajustar propiedades según el caso. Usar `record` con propiedades posicionales inmutables.

### Validator

```csharp
using FluentValidation;

namespace Resto.Application.{Feature}.{CommandName};

public sealed class {CommandName}CommandValidator
    : AbstractValidator<{CommandName}Command>
{
    public {CommandName}CommandValidator()
    {
        RuleFor(x => x.{Entity}Id)
            .NotEmpty()
            .WithMessage("El identificador es obligatorio.");

        RuleFor(x => x.RowVersion)
            .NotNull()
            .NotEmpty()
            .WithMessage("La versión de concurrencia es obligatoria.");

        // Reglas adicionales según campos del comando
    }
}
```

Reglas estrictas para IDs (`NotEmpty()` en GUIDs) y datos obligatorios del restaurante.

### Handler

```csharp
using MediatR;
using Resto.Application.Common.Interfaces;
using Resto.Application.Common.Models;

namespace Resto.Application.{Feature}.{CommandName};

public sealed class {CommandName}CommandHandler
    : IRequestHandler<{CommandName}Command, Result<Guid>>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IUnitOfWork _unitOfWork;

    public {CommandName}CommandHandler(
        I{Entity}Repository {entity}Repository,
        IUnitOfWork unitOfWork)
    {
        _{entity}Repository = {entity}Repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        {CommandName}Command request,
        CancellationToken cancellationToken)
    {
        var {entity} = await _{entity}Repository
            .GetByIdAsync(request.{Entity}Id, cancellationToken);

        if ({entity} is null)
            return Result<Guid>.Failure("{Entidad} no encontrado.");

        // El agregado valida reglas de negocio y lanza DomainException si falla
        {entity}.{SemanticMethod}();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        // Eventos de dominio → IDomainEventDispatcher → SignalR (tras commit)

        return Result<Guid>.Success({entity}.Id);
    }
}
```

**Reglas del handler:**
- No mutar propiedades del agregado directamente; solo invocar métodos semánticos.
- No capturar `DbUpdateConcurrencyException` aquí (Infrastructure lo traduce).
- No inyectar `IHubContext` ni llamar SignalR.
- Retornar `Result<Guid>.Failure(...)` para errores de aplicación (entidad no encontrada).
- Dejar propagar `DomainException` del agregado o mapearla a `Result.Failure` según patrón existente.

## Mapeo agregado → repositorio

| Agregado | Repositorio | RowVersion |
|----------|-------------|------------|
| `Pedido` | `IPedidoRepository` | Sí |
| `Mesa` | `IMesaRepository` | Sí |
| Otros | `I{Entity}Repository` | Según entidad |

## Checklist

```
- [ ] Carpeta en Resto.Application/{Feature}/{CommandName}/
- [ ] Command: record inmutable, IRequest<Result<Guid>>
- [ ] Validator: IDs NotEmpty, campos obligatorios, mensajes en español
- [ ] Handler: inyecta repositorio + IUnitOfWork
- [ ] Handler: carga agregado, invoca método semántico (no setters)
- [ ] Handler: SaveChangesAsync antes de retornar
- [ ] RowVersion incluido si el agregado es Pedido o Mesa
- [ ] Sin llamadas directas a SignalR en el handler
- [ ] Namespace y nombres alineados con comandos existentes
```

## Ejemplos

Ver [examples.md](examples.md) para un comando completo de referencia.
