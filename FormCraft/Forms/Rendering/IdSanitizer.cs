namespace FormCraft;

/// <summary>
/// Makes a field-name segment safe to embed in an HTML <c>id</c> that a consumer might also target
/// from plain CSS (#449).
/// </summary>
/// <remarks>
/// <para>
/// #437/#448 made a field's <c>FieldName</c> the full dotted member path for a nested binding
/// (<c>"Billing.Amount"</c> instead of <c>"Amount"</c>). A <c>.</c> is a valid HTML id character, so
/// nothing inside FormCraft broke — but it is also the CSS class-selector delimiter, so a consumer
/// targeting <c>#formcraft-help-Billing.Amount</c> from a stylesheet gets "id
/// <c>formcraft-help-Billing</c> AND class <c>Amount</c>", not the single element they typed it for.
/// </para>
/// <para>
/// <b>Why <c>.</c> → <c>-</c> stays injective.</b> Every character <see cref="ToCssSafeId"/> sees
/// other than a <c>.</c> separator comes from a C# member name, and a C# identifier can never itself
/// contain a literal <c>-</c> — it is not a permitted identifier character. So no real
/// <c>FieldName</c> can already contain a <c>-</c>, and replacing every <c>.</c> with one cannot make
/// two different dotted paths collide: <c>"a.b"</c> and <c>"a-b"</c> can never both occur as actual
/// field names, because the second is not a legal dotted member path to begin with.
/// </para>
/// </remarks>
public static class IdSanitizer
{
    /// <summary>
    /// Returns <paramref name="fieldName"/> with every <c>.</c> replaced by <c>-</c>, so it is safe
    /// to interpolate into an HTML id without becoming a CSS class-selector delimiter. A single
    /// segment (no nesting) is returned unchanged — there is nothing to sanitize.
    /// </summary>
    public static string ToCssSafeId(string fieldName) => fieldName.Replace('.', '-');
}
