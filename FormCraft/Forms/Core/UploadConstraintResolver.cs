namespace FormCraft;

/// <summary>
/// The single implementation of how an upload component resolves its constraints (#340): the
/// <see cref="FileUploadConfiguration"/> that <c>.AsFileUpload(...)</c>/<c>.AsMultipleFileUpload(...)</c>
/// write, falling back to the raw per-key attributes for hand-written <c>.WithAttribute(...)</c>
/// callers — which is why the raw keys exist at all.
/// </summary>
/// <remarks>
/// <para>
/// Before this, each of the four upload components (MudBlazor single/multiple, Fluent
/// single/multiple) read its own subset of keys with its own CLR types, so each silently dropped a
/// <i>different</i> constraint: the MudBlazor single-file component never read
/// <see cref="FileUploadConfiguration"/> at all, and the Fluent adapter's raw-key fallback inferred
/// <c>long</c>, so a hand-written <c>.WithAttribute("MaxFileSize", 5_000_000)</c> — which boxes an
/// <c>int</c> — failed <c>is long</c> and silently reverted to a default.
/// </para>
/// <para>
/// One resolver, one set of rules, used by every upload component — the same shape as
/// <see cref="NativeRequired"/> for the same reason: two independent readers is how this drifted in
/// the first place.
/// </para>
/// </remarks>
public static class UploadConstraintResolver
{
    /// <summary>The attribute key <c>.AsFileUpload</c>/<c>.AsMultipleFileUpload</c> write.</summary>
    public const string ConfigurationAttributeName = "FileUploadConfiguration";

    /// <summary>The <see cref="FileUploadConfiguration"/> the builder wrote, if any.</summary>
    public static FileUploadConfiguration? GetConfiguration(IReadOnlyDictionary<string, object> attributes)
        => attributes.TryGetValue(ConfigurationAttributeName, out var value) ? value as FileUploadConfiguration : null;

    /// <summary>
    /// The accept filter: the configuration's file types joined with a comma, or the raw
    /// <c>"Accept"</c> attribute when no configuration was written.
    /// </summary>
    public static string? ResolveAccept(FileUploadConfiguration? configuration, IReadOnlyDictionary<string, object> attributes)
        => configuration is not null ? configuration.Accept : GetRaw<string>(attributes, "Accept");

    /// <summary>
    /// The maximum file size in bytes: the configuration's value, or the first matching raw key —
    /// tolerating a boxed <c>int</c> where a <c>long</c> is expected, since
    /// <see cref="FieldComponentBase{TModel,TValue}.GetAttribute{T}"/>'s exact-type match otherwise
    /// drops it.
    /// </summary>
    public static long? ResolveMaxFileSize(
        FileUploadConfiguration? configuration,
        IReadOnlyDictionary<string, object> attributes,
        params string[] rawKeys)
        => configuration is not null ? configuration.MaxFileSize : GetRawSize(attributes, rawKeys);

    /// <summary>The maximum file count: the configuration's value, or the first matching raw key.</summary>
    public static int? ResolveMaxFiles(
        FileUploadConfiguration? configuration,
        IReadOnlyDictionary<string, object> attributes,
        params string[] rawKeys)
    {
        if (configuration is not null)
        {
            return configuration.MaxFiles;
        }

        var size = GetRawSize(attributes, rawKeys);
        return size.HasValue ? (int)size.Value : null;
    }

    /// <summary>
    /// Whether to preview selected files: the configuration's value, or the raw
    /// <c>"ShowPreview"</c> attribute.
    /// </summary>
    public static bool? ResolveShowPreview(FileUploadConfiguration? configuration, IReadOnlyDictionary<string, object> attributes)
        => configuration is not null ? configuration.ShowPreview : GetRaw<bool?>(attributes, "ShowPreview");

    /// <summary>
    /// Whether drag-and-drop is enabled: the configuration's value, or the raw
    /// <c>"EnableDragDrop"</c> attribute.
    /// </summary>
    public static bool? ResolveEnableDragDrop(FileUploadConfiguration? configuration, IReadOnlyDictionary<string, object> attributes)
        => configuration is not null ? configuration.EnableDragDrop : GetRaw<bool?>(attributes, "EnableDragDrop");

    private static long? GetRawSize(IReadOnlyDictionary<string, object> attributes, string[] keys)
    {
        foreach (var key in keys)
        {
            if (attributes.TryGetValue(key, out var value))
            {
                switch (value)
                {
                    case long l:
                        return l;
                    case int i:
                        return i;
                }
            }
        }

        return null;
    }

    private static T? GetRaw<T>(IReadOnlyDictionary<string, object> attributes, string key)
        => attributes.TryGetValue(key, out var value) && value is T typed ? typed : default;
}
