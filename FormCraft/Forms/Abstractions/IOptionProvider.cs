namespace FormCraft;

/// <summary>
/// Provides async search-based options for autocomplete and lookup fields.
/// </summary>
/// <typeparam name="TModel">The model type that the form binds to.</typeparam>
/// <typeparam name="TValue">The type of the option value.</typeparam>
public interface IOptionProvider<in TModel, TValue>
{
    /// <summary>
    /// Searches for options matching the given text.
    /// </summary>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="model">The current model instance for context-aware searching.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A collection of matching options.</returns>
    Task<IEnumerable<SelectOption<TValue>>> SearchAsync(
        string searchText,
        TModel model,
        CancellationToken cancellationToken = default);
}
