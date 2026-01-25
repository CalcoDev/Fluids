#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Calcopod.Visualisation;

/// <summary>
/// Static registry to track all DebugDrawComponents and determine the current active target.
/// </summary>
public static class DebugDrawRegistry
{
    private static readonly HashSet<DebugDrawComponent> _components = new();
    private static DebugDrawComponent? _cachedActive;
    private static bool _isDirty = true;

    /// <summary>
    /// The default component (typically set by autoload).
    /// </summary>
    public static DebugDrawComponent? Default { get; set; }

    /// <summary>
    /// The currently active component based on Active flag and Priority.
    /// </summary>
    public static DebugDrawComponent? Active
    {
        get
        {
            if (_isDirty)
            {
                RecomputeActive();
            }
            return _cachedActive;
        }
    }

    /// <summary>
    /// Register a component with the registry.
    /// </summary>
    public static void Register(DebugDrawComponent component)
    {
        if (_components.Add(component))
        {
            _isDirty = true;
        }
    }

    /// <summary>
    /// Unregister a component from the registry.
    /// </summary>
    public static void Unregister(DebugDrawComponent component)
    {
        if (_components.Remove(component))
        {
            _isDirty = true;

            if (Default == component)
            {
                Default = null;
            }
        }
    }

    /// <summary>
    /// Mark the registry as needing to recompute the active component.
    /// Call this when a component's Active or Priority changes.
    /// </summary>
    public static void MarkDirty()
    {
        _isDirty = true;
    }

    /// <summary>
    /// Get all registered components.
    /// </summary>
    public static IReadOnlyCollection<DebugDrawComponent> GetAll() => _components;

    private static void RecomputeActive()
    {
        _cachedActive = _components
            .Where(c => c.Active && Godot.GodotObject.IsInstanceValid(c))
            .OrderByDescending(c => c.Priority)
            .FirstOrDefault();

        _isDirty = false;
    }
}
