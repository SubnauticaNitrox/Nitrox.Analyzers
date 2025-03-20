namespace Nitrox.Analyzers.Configuration;

/// <summary>
///     Keep in sync with Nitrox.Analyzers.props
/// </summary>
internal record struct SourceGeneratorConfiguration
(
    // You can disable generating of the interceptor attribute by setting this to false
    // You might need it if you already have this attribute in your project
    bool GenerateInterceptorAttribute
);
