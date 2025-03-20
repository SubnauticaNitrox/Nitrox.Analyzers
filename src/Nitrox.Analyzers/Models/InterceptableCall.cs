using Microsoft.CodeAnalysis;
using Nitrox.Analyzers.Helpers;

namespace Nitrox.Analyzers.Models;

/// <summary>
///     Models an interceptable call with all the information needed to define an interceptor for said call.
/// </summary>
internal record InterceptableCall(CallLocation Location, string OwnerTypeName, EquatableArray<MethodParameterInfo> Parameters, string ReturnTypeName, bool IsAsync, bool IsStatic, string Name, string OwnerNamespace, Accessibility Accessibility);
