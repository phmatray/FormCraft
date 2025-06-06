namespace DynamicFormBlazor.Forms.Core;

public interface IFormConfiguration<TModel>
{
    List<IFieldConfiguration<TModel, object>> Fields { get; }
    Dictionary<string, List<IFieldDependency<TModel>>> FieldDependencies { get; }
    FormLayout Layout { get; set; }
    string? CssClass { get; set; }
    bool ShowValidationSummary { get; set; }
    bool ShowRequiredIndicator { get; set; }
    string RequiredIndicator { get; set; }
}

public enum FormLayout
{
    Vertical,
    Horizontal,
    Inline,
    Grid
}

public class FormConfiguration<TModel> : IFormConfiguration<TModel>
{
    public List<IFieldConfiguration<TModel, object>> Fields { get; } = new();
    public Dictionary<string, List<IFieldDependency<TModel>>> FieldDependencies { get; } = new();
    public FormLayout Layout { get; set; } = FormLayout.Vertical;
    public string? CssClass { get; set; }
    public bool ShowValidationSummary { get; set; } = true;
    public bool ShowRequiredIndicator { get; set; } = true;
    public string RequiredIndicator { get; set; } = "*";
}