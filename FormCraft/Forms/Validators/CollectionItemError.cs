namespace FormCraft;

/// <summary>
/// Represents a validation error for a single field of a single item inside a collection field.
/// Carries enough structure to build a nested Blazor <c>FieldIdentifier</c> such as
/// <c>Items[0].ProductName</c> (collection field name + item index + item field name).
/// </summary>
/// <param name="ItemIndex">The zero-based index of the item within the collection.</param>
/// <param name="FieldName">The name of the field on the item that failed validation.</param>
/// <param name="Message">The validation error message.</param>
public sealed record CollectionItemError(int ItemIndex, string FieldName, string Message);
