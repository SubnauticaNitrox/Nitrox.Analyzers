#if DEBUG
using System;

namespace Nitrox.Analyzers.Boilerplate;

/// <summary>
///     Used for debugging that the generator pipeline properly caches results; isn't redundantly regenerating.
/// </summary>
public static class GeneratedTimeBoilerplate
{
    public static string Code => $"// Generated on {DateTime.Now.ToLongTimeString()}";
}
#endif
