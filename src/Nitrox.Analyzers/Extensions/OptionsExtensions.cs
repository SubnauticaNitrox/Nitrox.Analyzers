using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Nitrox.Analyzers.Configuration;

namespace Nitrox.Analyzers.Extensions;

internal static class OptionsExtensions
{
    private const string CommonPrefix = $"build_property.{nameof(Nitrox)}{nameof(Analyzers)}";

    public static IncrementalValueProvider<SourceGeneratorConfiguration> GetOptionsProvider(this IncrementalGeneratorInitializationContext context) =>
        context.AnalyzerConfigOptionsProvider.Select((options, _) => new SourceGeneratorConfiguration(
                                                         GetValue(options.GlobalOptions, $"{CommonPrefix}_{nameof(SourceGeneratorConfiguration.GenerateInterceptorAttribute)}")
                                                     ));

    private static bool GetValue(AnalyzerConfigOptions options, string key, bool defaultValue = true) =>
        options.TryGetValue(key, out string? value) ? IsFeatureEnabled(value, defaultValue) : defaultValue;

    private static bool IsFeatureEnabled(string value, bool defaultValue) =>
        StringComparer.OrdinalIgnoreCase.Equals("true", value) ||
        StringComparer.OrdinalIgnoreCase.Equals("enable", value) ||
        StringComparer.OrdinalIgnoreCase.Equals("enabled", value) ||
        (bool.TryParse(value, out bool boolVal) ? boolVal : defaultValue);
}
