using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nitrox.Analyzers.Configuration;

/// <summary>
///     Keep in sync with Nitrox.Analyzers.props
/// </summary>
internal static class GeneratorOptions
{
    private const string CommonPrefix = $"build_property.{nameof(Nitrox)}{nameof(Analyzers)}";
    private const string GenerateInterceptorAttributeKey = $"{CommonPrefix}_{nameof(SourceGeneratorConfiguration.GenerateInterceptorAttribute)}";

    public static IncrementalValueProvider<SourceGeneratorConfiguration> Provide(IncrementalGeneratorInitializationContext context) =>
        context.AnalyzerConfigOptionsProvider.Select((options, _) => new SourceGeneratorConfiguration(
                                                         GetValue(options.GlobalOptions, GenerateInterceptorAttributeKey)
                                                     ));

    private static bool GetValue(AnalyzerConfigOptions options, string key, bool defaultValue = true) =>
        options.TryGetValue(key, out string? value) ? IsFeatureEnabled(value, defaultValue) : defaultValue;

    private static bool IsFeatureEnabled(string value, bool defaultValue) =>
        StringComparer.OrdinalIgnoreCase.Equals("true", value) ||
        StringComparer.OrdinalIgnoreCase.Equals("enable", value) ||
        StringComparer.OrdinalIgnoreCase.Equals("enabled", value) ||
        (bool.TryParse(value, out bool boolVal) ? boolVal : defaultValue);
}
