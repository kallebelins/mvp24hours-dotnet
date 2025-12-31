//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics.CodeAnalysis;

namespace Mvp24Hours.Core.Serialization.SourceGeneration;

/// <summary>
/// Provides AOT (Ahead-of-Time) compatibility helpers and documentation.
/// </summary>
/// <remarks>
/// <para>
/// Native AOT compilation provides faster startup, lower memory usage, and smaller deployment size.
/// However, it requires eliminating or annotating reflection usage.
/// </para>
/// <para>
/// <strong>Key considerations for AOT compatibility:</strong>
/// <list type="bullet">
/// <item><description>Use source-generated JSON serialization via <see cref="Mvp24HoursJsonSerializerContext"/></description></item>
/// <item><description>Use <c>[LoggerMessage]</c> attributes for logging instead of string interpolation</description></item>
/// <item><description>Avoid <c>Activator.CreateInstance</c> and <c>Type.MakeGenericType</c></description></item>
/// <item><description>Mark reflection-dependent code with <c>[RequiresUnreferencedCode]</c></description></item>
/// <item><description>Use <c>[DynamicallyAccessedMembers]</c> to preserve required type info</description></item>
/// </list>
/// </para>
/// </remarks>
public static class AotCompatibility
{
    /// <summary>
    /// Indicates whether the application is running in Native AOT mode.
    /// </summary>
    /// <remarks>
    /// This property uses the <c>NATIVE_AOT</c> compilation symbol that is automatically
    /// defined when publishing with Native AOT.
    /// </remarks>
    public static bool IsNativeAot =>
#if NATIVE_AOT
        true;
#else
        false;
#endif

    /// <summary>
    /// Warning message for methods that use reflection and may not work in Native AOT.
    /// </summary>
    public const string ReflectionWarning =
        "This method uses reflection and may not be compatible with Native AOT. " +
        "Consider using a source-generated alternative.";

    /// <summary>
    /// Warning message for JSON serialization without source generation.
    /// </summary>
    public const string JsonReflectionWarning =
        "JSON serialization without source generation uses reflection. " +
        "For AOT compatibility, use Mvp24HoursJsonSerializerContext or a custom JsonSerializerContext.";

    /// <summary>
    /// Warning message for dynamic type creation.
    /// </summary>
    public const string DynamicTypeCreationWarning =
        "Dynamic type creation via Activator.CreateInstance or MakeGenericType is not AOT-compatible. " +
        "Consider using a factory pattern or dependency injection.";
}

/// <summary>
/// Attribute to mark assemblies, types, or methods as AOT-compatible.
/// </summary>
/// <remarks>
/// This attribute serves as documentation and can be used by analyzers to verify AOT compatibility.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct |
    AttributeTargets.Method | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
public sealed class AotCompatibleAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AotCompatibleAttribute"/> class.
    /// </summary>
    public AotCompatibleAttribute() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AotCompatibleAttribute"/> class with notes.
    /// </summary>
    /// <param name="notes">Notes about AOT compatibility considerations.</param>
    public AotCompatibleAttribute(string notes)
    {
        Notes = notes;
    }

    /// <summary>
    /// Gets the notes about AOT compatibility.
    /// </summary>
    public string? Notes { get; }
}

/// <summary>
/// Attribute to mark types or methods that require reflection and are not fully AOT-compatible.
/// </summary>
/// <remarks>
/// This complements <see cref="RequiresUnreferencedCodeAttribute"/> with Mvp24Hours-specific guidance.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor,
    AllowMultiple = false,
    Inherited = false)]
public sealed class RequiresReflectionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresReflectionAttribute"/> class.
    /// </summary>
    /// <param name="reason">The reason why reflection is required.</param>
    public RequiresReflectionAttribute(string reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets the reason why reflection is required.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Gets or sets the recommended AOT-compatible alternative.
    /// </summary>
    public string? Alternative { get; set; }
}

