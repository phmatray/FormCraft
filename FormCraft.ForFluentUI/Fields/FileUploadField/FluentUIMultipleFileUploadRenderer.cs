using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI renderer for multiple-file fields.
/// </summary>
/// <remarks>
/// Matches <c>IReadOnlyList&lt;IBrowserFile&gt;</c> - the type core's
/// <c>.AsMultipleFileUpload(...)</c> is declared on, and the one the MudBlazor renderer matches - as
/// well as a concrete <c>List&lt;IBrowserFile&gt;</c>, which a model may reasonably declare and
/// which this renderer's own tests use. Matching only the concrete list, as an earlier draft did,
/// left the canonical builder-produced field matching neither upload renderer: it fell through to
/// the service's "Unsupported field type" placeholder, silently.
/// </remarks>
public class FluentUIMultipleFileUploadRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUIMultipleFileUploadComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        if (fieldType == typeof(IReadOnlyList<IBrowserFile>))
        {
            return true;
        }

        return fieldType.IsGenericType &&
               fieldType.GetGenericTypeDefinition() == typeof(List<>) &&
               typeof(IBrowserFile).IsAssignableFrom(fieldType.GetGenericArguments()[0]);
    }
}
