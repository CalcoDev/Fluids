# Calcopod - DebugDraw2D Visualization System

Once again, after the [Input System](https://github.com/CalcoDev/TheColloseum/tree/main/src/input) from [The Colloseum](https://github.com/CalcoDev/TheColloseum), I have a system that I am decently proud of ish. This time we actually have some documentation INSIDE the actual code as well (lmfao), but still, I am writing this as well to comemorate a cool README.

A small debug drawing system for Godot, made in C#. Draw debug geometry, text, and custom shapes that persist per-frame or for timed durations, with support for world and screen coordinate spaces, layering, and "frame-based" animation.

## ACKNOWLEDGEMENTS

The basis of this system, and quite a few ideas, were taken from [Sebastian Lague](https://www.youtube.com/@SebastianLague)'s [SebVis assembly definition](https://github.com/SebLague/Smoke-Simulation/tree/main/Assets/Seb/SebVis) from his video [Coding Adventure: Simulating Smoke](https://youtu.be/Q78wvrQ9xsU).

## Table of Contents

- [Calcopod - DebugDraw2D Visualization System](#calcopod---debugdraw2d-visualization-system)
  - [ACKNOWLEDGEMENTS](#acknowledgements)
  - [Table of Contents](#table-of-contents)
  - [Description](#description)
    - [Key Features](#key-features)
    - [Core Concepts](#core-concepts)
      - [DebugDrawComponent](#debugdrawcomponent)
      - [DebugDrawRegistry](#debugdrawregistry)
      - [Draw (Static Facade)](#draw-static-facade)
      - [DrawCommand](#drawcommand)
      - [WorldSurface \& ScreenSurface](#worldsurface--screensurface)
  - [How to Use](#how-to-use)
    - [Setup](#setup)
      - [Option 1: Manual (Recommanded)](#option-1-manual-recommanded)
      - [Option 2: Autoload](#option-2-autoload)
    - [Basic Drawing](#basic-drawing)
    - [Coordinate Spaces](#coordinate-spaces)
      - [World Space](#world-space)
      - [Screen Space](#screen-space)
    - [Lifetimes and Animation](#lifetimes-and-animation)
      - [Animation Progress (`animT`)](#animation-progress-animt)
    - [Layering and Ordering](#layering-and-ordering)
      - [DISCLAIMER](#disclaimer)
    - [Scoped Targeting](#scoped-targeting)
      - [Target Resolution Rules](#target-resolution-rules)
    - [Custom Commands](#custom-commands)
  - [Cleanup](#cleanup)

## Description

The DebugDraw2D system provides an easy, immediate-mode-like API for debugging visualization without the overhead of creating temporary nodes. Commands are buffered, rendered every frame via `_Draw()`, and automatically cleaned up based on their lifetime settings.

The system is designed to be:

- **Component-based**: Spawn a `DebugDrawComponent` anywhere to enable debug visualization in that context.
- **Non-invasive**: Draw calls are no-ops if no component is active.
- **Flexible**: Supports multiple independent debug draw instances (main viewport, subviewports, nested scenes), as well as completely custom Draw functionality for more advanced scenes.

Much more to be added later, **especially in the department of optimisation**, as we do not yet actually use the fact we are batching anything. Eventually we should also migrate away from Godot's `_Draw()` functionality, and use some proper primitives, similar to [Godot Debug 3D](https://github.com/DmitriySalnikov/godot_debug_draw_3d).

### Key Features

- **Buffered rendering** - Commands are recorded, not immediate-mode; rendered via `QueueRedraw()` + `_Draw()`
- **Multiple coordinate spaces** - World (camera-aware) and Screen (viewport UI space)
- **Layer and insertion ordering** - Commands render in layer order, with strict insertion order within layers
- **Per-frame and timed lifetimes** - Specify duration in seconds for timed commands
- **Time-driven animation** - `animDuration` automatically drives animation progress (`animT`)
- **Static facade API** - Simple `Draw.*` calls with automatic target resolution
- **Scope override** - Use `Draw.Use(component)` to route calls to a specific component

### Core Concepts

Again, supposedly documentation will exist in the actual code, but for ease of use:

#### DebugDrawComponent

- The main user-facing node. Owns two render surfaces (world and screen).
- Tracks command buffers, fonts, and component state.
- Automatically registers with the global registry.

#### DebugDrawRegistry

- Static singleton tracking all active components.
- Determines "current active" based on `Active` flag and `Priority`.
- Enables multi-instance support (main + subviewports).

#### Draw (Static Facade)

- Public API for all drawing operations.
- Resolves target automatically: stack → active → default → no-op.
- Supports scope override via `Draw.Use(component)`.

#### DrawCommand

- Base class for all drawing commands.
- Subclasses implement custom draw logic via `Draw(CanvasItem, DebugDrawComponent)`.
- Stores space, layer, color, thickness, lifetime, and animation state.

#### WorldSurface & ScreenSurface

- Node2D and Control nodes that render commands.
- Grouped by layer, rendered in insertion order within layer.
- Automatically created by `DebugDrawComponent` if missing.

## How to Use

### Setup

#### Option 1: Manual (Recommanded)

Create a `DebugDrawComponent` node in your scene and set `Active = true`. That's it.

#### Option 2: Autoload

Why use this 💀. Literally just add a node to your scene tree and click one button, it's more customizable and just better. Still, because this is how this originally was made, there is an autoload which sets stuff up.

Add `DebugDrawBootstrap` as an autoload in **Project Settings → Autoload**:

1. Create a scene with `DebugDrawBootstrap` node
2. Add it as an autoload named `DebugDrawBootstrap`
3. A default `DebugDrawComponent` will be created automatically
4. You can now call `Draw.*` from anywhere

### Basic Drawing

```csharp
// Primitives
Draw.Line(new Vector2(0, 0), new Vector2(100, 100), Colors.Red, thickness: 2f);
Draw.Circle(new Vector2(50, 50), 25f, Colors.Blue, filled: true);
Draw.Rect(new Rect2(10, 10, 100, 50), Colors.Green, filled: false);
var points = new List<Vector2> { Vector2.Zero, new Vector2(50, 50), new Vector2(100, 0) };
Draw.Polyline(points, Colors.Yellow);

// Text
Draw.Text(new Vector2(10, 10), "Hello, Debug!", Colors.White, font: DrawFontKind.Mono, size: 14f);

// Helper shapes
Draw.Arrow(from, to, Colors.Purple, headSize: 10f);
Draw.Cross(center, size: 20f, Colors.Cyan);
Draw.GridLines(new Rect2(0, 0, 200, 200), new Vector2(10, 10), Colors.Gray, thickness: 0.5f);
Draw.GridSquares(new Rect2(0, 0, 200, 200), new Vector2(10, 10), Colors.Orange, filled: false);
```

### Coordinate Spaces

```csharp
// World space (default) - camera-aware, world transform applies
Draw.Line(from, to, Colors.Red, space: DrawSpace.World);

// Screen space - rendered in viewport UI coordinates, ignores camera
Draw.Circle(screenPos, radius, Colors.Blue, space: DrawSpace.Screen);
```

#### World Space

- Natural world transform and camera rules apply
- When component lives under a SubViewport, world space is that subviewport's world
- Rendered by `WorldSurface` (Node2D)

#### Screen Space

- Viewport UI coordinates
- `ScreenSurface` (Control) is layered on top via CanvasLayer
- Ignores camera and world transforms

### Lifetimes and Animation

By default, commands last a single frame. Specify duration for persistence:

```csharp
// Frame-only (default, duration = 0)
Draw.Line(from, to, Colors.Red);

// Persist for 2 seconds
Draw.Line(from, to, Colors.Red, duration: 2f);

// Animate over 1 second while persisting for 2 seconds
Draw.Line(from, to, Colors.Red, duration: 2f, animDuration: 1f);
```

#### Animation Progress (`animT`)

- Automatically computed at draw time: `animT = clamp(1 - TimeRemaining / AnimDuration, 0, 1)`
- Returns 1 if `AnimDuration <= 0` (no animation)
- Each command type interprets `animT` differently:
  - **Line**: endpoint lerped by `animT`
  - **Polyline**: draws prefix by arc-length fraction
  - **Rect/Circle**: size scaled by `animT`
  - **Arc**: sweep angle fraction
  - **Text**: character count animated
  - **Arrow**: shaft length animated; head appears at end

### Layering and Ordering

Commands render in ascending layer order. Within a layer, insertion order is preserved:

```csharp
// Layer 0 (background)
Draw.Circle(pos1, 25f, Colors.Blue, layer: 0);
Draw.Rect(rect1, Colors.Green, layer: 0);

// Layer 1 (foreground)
Draw.Line(a, b, Colors.Red, layer: 1);
Draw.Text(pos, "Text", Colors.White, layer: 1);
```

#### DISCLAIMER

This WILL be changed later to have an API that resembles a more traditional layout

```csharp
// PSEUDOCODE, DOESN'T EXIST YET

Draw.BeginLayer(1);
Draw.EndLayer();

// Or
int layer = Draw.BeginLayer();
Draw.EndLayer(layer);

// Or
using (var layer = ...) {

}
// ... you get the point lmao
```

### Scoped Targeting

Route draw calls to a specific component using `Draw.Use()`:

```csharp
// Default component
Draw.Circle(pos1, 25f, Colors.Red);

// Scoped override - this goes to mySubviewportDebugDraw
using (Draw.Use(mySubviewportDebugDraw))
{
    Draw.Circle(pos2, 25f, Colors.Blue);
    Draw.Line(a, b, Colors.Green);
    // When scope exits, target reverts to default
}

// Back to default
Draw.Text(pos3, "Back to default", Colors.White);
```

#### Target Resolution Rules

1. If a scope override is active → use top of stack
2. Else if a component has `Active = true` → use highest priority
3. Else if a default component exists → use default
4. Else → no-op

### Custom Commands

Extend `DrawCommand` to create custom drawing logic:

```csharp
public class WonkyCommand : DrawCommand
{
    public Vector2 CenterPos { get; set; }
    public float Wobble { get; set; }

    public override void Draw(CanvasItem canvas, DebugDrawComponent component)
    {
        float t = GetAnimT();
        float offset = Mathf.Sin(t * Mathf.Tau) * Wobble;

        canvas.DrawCircle(
            CenterPos + new Vector2(offset, 0),
            10f + 5f * t,
            Color
        );
    }
}

// Enqueue it
var cmd = new WonkyCommand
{
    CenterPos = new Vector2(100, 100),
    Wobble = 20f,
    Color = Colors.Purple,
    Layer = 1,
    AnimDuration = 2f,
    TimeRemaining = 2f
};
Draw.Enqueue(cmd);
```

## Cleanup

Commands are automatically cleaned up:

- **Frame commands**: Cleared at the start of each `_Process()` call
- **Timed commands**: Automatically removed when `TimeRemaining <= 0`

For manual control:

```csharp
// Clear frame-only commands
Draw.ClearFrame();

// Clear timed commands
Draw.ClearTimed();

// Clear everything
Draw.ClearAll();
```
