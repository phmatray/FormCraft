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
/// <b>Why <c>.</c> → <c>-</c> stays injective.</b> Every <c>FieldName</c> this library itself produces
/// (<c>MemberPathResolver.GetDottedPath</c>, or a bare <c>MemberExpression.Member.Name</c>) is built
/// from C# member names joined by <c>.</c>, and a C# identifier can never itself contain a literal
/// <c>-</c> — it is not a permitted identifier character. So along every path this library builds a
/// <c>FieldName</c>, replacing every <c>.</c> with a <c>-</c> cannot make two different dotted paths
/// collide: <c>"a.b"</c> and <c>"a-b"</c> can never both occur as field names produced this way,
/// because the second is not a legal dotted member path to begin with. (<c>FieldName</c> is a plain
/// settable <see langword="string"/> on the underlying configuration types, so this is a guarantee
/// about how the library constructs it, not one the type system enforces against a hand-built
/// <c>IFieldConfiguration</c> that assigns something else entirely.)
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
