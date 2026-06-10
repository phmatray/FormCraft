namespace FormCraft;

/// <summary>
/// Excludes a property from automatic form generation performed by
/// <c>AddFieldsAuto()</c>. Properties decorated with this attribute are
/// skipped even when they would otherwise produce a field.
/// </summary>
/// <example>
/// <code>
/// public class CustomerModel
/// {
///     public string Name { get; set; } = string.Empty;
///
///     [ExcludeField]
///     public string InternalReference { get; set; } = string.Empty;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ExcludeFieldAttribute : Attribute;
