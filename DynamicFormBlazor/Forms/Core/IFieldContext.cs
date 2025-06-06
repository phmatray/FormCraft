using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace DynamicFormBlazor.Forms.Core;

public interface IFieldContext<TModel, TValue>
{
    TModel Model { get; }
    IFieldConfiguration<TModel, TValue> Configuration { get; }
    EditContext EditContext { get; }
    TValue Value { get; set; }
    EventCallback<TValue> ValueChanged { get; }
    FieldIdentifier FieldIdentifier { get; }
    IEnumerable<string> ValidationMessages { get; }
    bool IsValid { get; }
    string FieldCssClass { get; }
}