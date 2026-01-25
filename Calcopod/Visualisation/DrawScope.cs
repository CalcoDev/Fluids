using System;

namespace Calcopod.Visualisation;

// TODO(calco): Potentially change this to actually be like using (... = ...)
// instead of general static Draw.() so we can easily reference past times?

/// <summary>
/// Disposable scope for overriding the Draw target.
/// Use with Draw.Use(component) to route draw calls to a specific component.
/// </summary>
/// <example>
/// using (Draw.Use(mySubviewDebugDraw))
/// {
///     Draw.Line(...); // routes into that component
/// }
/// </example>
public readonly struct DrawScope : IDisposable
{
    public void Dispose()
    {
        Draw.PopTarget();
    }
}
