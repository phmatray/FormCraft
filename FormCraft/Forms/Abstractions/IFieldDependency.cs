namespace FormCraft;

/// <summary>
/// Defines the contract for field dependencies that allow one field to react to changes in another field.
/// </summary>
/// <typeparam name="TModel">The model type that contains the dependent fields.</typeparam>
/// <example>
/// <code>
/// // Example: Show/hide a field based on another field's value
/// .AddField(x => x.HasSpouse)
///     .WithLabel("Are you married?");
/// 
/// .AddField(x => x.SpouseName)
///     .WithLabel("Spouse Name")
///     .DependsOn(x => x.HasSpouse, (model, hasSpouse) => 
///     {
///         // This field is only visible when HasSpouse is true
///         model.SpouseName = hasSpouse ? model.SpouseName : null;
///     })
///     .VisibleWhen(x => x.HasSpouse);
/// </code>
/// </example>
public interface IFieldDependency<TModel>
{
    /// <summary>
    /// Gets the name of the field that this dependency depends on.
    /// </summary>
    string DependentFieldName { get; }

    /// <summary>
    /// Called when the dependent field's value changes, allowing this field to react accordingly.
    /// </summary>
    /// <param name="model">The complete model instance containing both fields.</param>
    void OnDependencyChanged(TModel model);
}