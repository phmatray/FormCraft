using Microsoft.AspNetCore.Components;
using DynamicFormBlazor.Forms.Core;
using System.Linq.Expressions;

namespace DynamicFormBlazor.Forms.Rendering;

public interface IFieldRenderer
{
    bool CanRender(Type fieldType, IFieldConfiguration<object, object> field);
    RenderFragment Render<TModel>(IFieldRenderContext<TModel> context);
}

public interface IFieldRenderContext<TModel>
{
    TModel Model { get; }
    IFieldConfiguration<TModel, object> Field { get; }
    Type ActualFieldType { get; }
    object? CurrentValue { get; }
    EventCallback<object?> OnValueChanged { get; }
    EventCallback OnDependencyChanged { get; }
}

public class FieldRenderContext<TModel> : IFieldRenderContext<TModel>
{
    public TModel Model { get; init; } = default!;
    public IFieldConfiguration<TModel, object> Field { get; init; } = default!;
    public Type ActualFieldType { get; init; } = default!;
    public object? CurrentValue { get; init; }
    public EventCallback<object?> OnValueChanged { get; init; }
    public EventCallback OnDependencyChanged { get; init; }
}