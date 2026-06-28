---
name: generate-kitchen-card
description: Genera el marcado HTML con las clases semánticas de Tailwind para alertas de tiempo. Use al diseñar o modificar tarjetas de comanda del monitor de cocina en Angular 21.
disable-model-invocation: true
---

# Generate Kitchen Card

Genera el marcado HTML de tarjetas de comanda del monitor de cocina con clases Tailwind semánticas según minutos transcurridos (calculados en el cliente).

## Estructura Base

Al diseñar o modificar la vista del monitor de cocina en Angular 21, estructura el contenedor de la comanda con este estándar UX/UI de Tailwind:

- El contenedor principal debe evaluar dinámicamente una variable de minutos calculada en el cliente.
- Si minutos < 15: Usa fondo bg-slate-850 y borde sutil border-slate-700.
- Si minutos >= 15 && minutos < 20: Agrega borde de advertencia border-amber-500 e incorpora la animación animate-pulse para captar la atención de manera sutil.

Si minutos >= 20: El estado es crítico. Transforma el fondo a un tono de advertencia bg-rose-950/30, borde acentuado border-rose-600 e incluye un indicador visual o badge con la clase bg-rose-600 text-white para denotar urgencia máxima.

## Implementación Angular 21

1. **Standalone** — sin NgModule.
2. **Signals** — `elapsedMinutes` como `computed` a partir de `confirmedAt` y un `timeTick` local (interval cada 30 s).
3. **Sin interacción** — la tarjeta es pasiva (sin clics ni inputs).
4. **Clases dinámicas** — `[ngClass]` o `class` binding sobre el contenedor principal; no duplicar lógica en el template.

### Mapa de estados

| Condición | Fondo | Borde | Extra |
|-----------|-------|-------|-------|
| `minutos < 15` | `bg-slate-850` | `border-slate-700` | — |
| `15 <= minutos < 20` | `bg-slate-850` | `border-amber-500` | `animate-pulse` |
| `minutos >= 20` | `bg-rose-950/30` | `border-rose-600` | badge `bg-rose-600 text-white` |

### Helper TypeScript (patrón recomendado)

```typescript
type KitchenCardUrgency = 'normal' | 'warning' | 'critical';

function resolveKitchenCardUrgency(minutes: number): KitchenCardUrgency {
  if (minutes >= 20) return 'critical';
  if (minutes >= 15) return 'warning';
  return 'normal';
}

function kitchenCardClasses(urgency: KitchenCardUrgency): Record<string, boolean> {
  return {
    'bg-slate-850': urgency !== 'critical',
    'border-slate-700': urgency === 'normal',
    'border-amber-500 animate-pulse': urgency === 'warning',
    'bg-rose-950/30 border-rose-600': urgency === 'critical',
  };
}
```

## Plantilla mínima

```html
<article
  class="rounded-lg border p-4 transition-colors"
  [ngClass]="kitchenCardClasses(urgency())"
  [attr.aria-label]="'Comanda mesa ' + order().tableNumber + ', ' + elapsedMinutes() + ' minutos'"
>
  @if (urgency() === 'critical') {
    <span class="mb-2 inline-block rounded px-2 py-0.5 text-xs font-semibold bg-rose-600 text-white">
      URGENTE
    </span>
  }

  <header class="mb-2 flex items-center justify-between">
    <h2 class="text-lg font-semibold text-slate-100">Mesa {{ order().tableNumber }}</h2>
    <time class="text-sm text-slate-400">{{ elapsedMinutes() }} min</time>
  </header>

  <ul class="space-y-1 text-slate-200">
    @for (item of order().items; track item.id) {
      <li>{{ item.quantity }}× {{ item.name }}</li>
    }
  </ul>
</article>
```

## Checklist de entrega

- [ ] `minutos` proviene del cliente (`confirmedAt` + `timeTick`), no de polling al servidor
- [ ] Los tres umbrales (< 15, 15–19, ≥ 20) aplican las clases exactas del estándar
- [ ] Estado crítico incluye badge `bg-rose-600 text-white`
- [ ] Contenedor usa `border` base + clases de estado; transición suave con `transition-colors`
- [ ] Monitor pasivo: sin botones, inputs ni handlers de click

## Ejemplos completos

Ver [examples.md](examples.md) para componente standalone completo y variantes de layout.
