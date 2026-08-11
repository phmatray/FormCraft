#if !NET9_0_OR_GREATER

// The compiler recognizes this attribute by its fully-qualified name, so declaring it in-source
// enables [OverloadResolutionPriority] on target frameworks whose BCL predates .NET 9 — the same
// pattern the BCL documents for IsExternalInit. FormCraft multi-targets net8.0, and without this
// the net8.0 leg would not compile the priority annotations in LovBuilder.
//
// Delete this file when net8.0 is dropped from <TargetFrameworks>.

namespace System.Runtime.CompilerServices;

/// <summary>
/// Specifies the priority of a member in overload resolution. When unspecified, the default
/// priority is 0.
/// </summary>
[AttributeUsage(
    AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
internal sealed class OverloadResolutionPriorityAttribute(int priority) : Attribute
{
    /// <summary>
    /// Gets the priority of the member. Higher values are preferred during overload resolution.
    /// </summary>
    public int Priority => priority;
}

#endif
