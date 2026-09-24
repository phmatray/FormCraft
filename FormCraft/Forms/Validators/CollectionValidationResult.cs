namespace FormCraft;

/// <summary>
/// Everything one validation pass over a collection produces, in both shapes its callers need.
/// </summary>
/// <param name="Messages">
/// The flat, human-formatted messages for the collection's own field identifier — item-count rules
/// first, then one line per item error.
/// </param>
/// <param name="ItemErrors">
/// The structured per-item errors, carrying the item index and field name so a caller can attach each
/// to its nested <c>Items[i].Field</c> identifier (#91).
/// </param>
/// <param name="EvaluatedItemFields">
/// Every <c>(ItemIndex, FieldName)</c> pair this traversal ran validators against, valid or not
/// (#447). <see cref="ItemErrors" /> alone cannot tell a caller "this cell was checked and is fine"
/// apart from "this cell was never checked" — a distinction <c>DynamicFormValidator</c> needs so it
/// can stamp a valid cell exactly as it already stamps a failing one, and a field-changed handler
/// parked on that cell since before the pass started cannot resurrect a stale error once it resumes.
/// A superset of <see cref="ItemErrors" />'s own identifiers.
/// </param>
/// <remarks>
/// This type exists so the two shapes come from <b>one</b> traversal. They used to be obtained by
/// calling <c>ValidateAsync</c> and <c>ValidateItemsAsync</c> in turn, and since the former already
/// awaits the latter, every item field's validators ran twice per pass — invisible in the output,
/// because the two results are attached to different identifiers, but plainly visible to any
/// validator with a side effect (#329).
/// <para>
/// It is deliberately non-generic: <c>DynamicFormValidator</c> constructs
/// <c>CollectionFieldValidator&lt;,&gt;</c> reflectively and cannot name the item type, so a generic
/// result would have to be unpacked reflectively too.
/// </para>
/// </remarks>
public sealed record CollectionValidationResult(
    IReadOnlyList<string> Messages,
    IReadOnlyList<CollectionItemError> ItemErrors,
    IReadOnlyList<(int ItemIndex, string FieldName)> EvaluatedItemFields);
