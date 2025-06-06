using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DynamicFormBlazor.Models;

public abstract class FieldDefinition
{
    public string Key { get; set; }
    public string Label { get; set; }
    public abstract object? DefaultValue { get; }
    public abstract RenderFragment RenderFragment(Dictionary<string, object> model);
}

public class FieldDefinition<T> : FieldDefinition
{
    public T Value { get; set; }
    public override object? DefaultValue => Value;

    public override RenderFragment RenderFragment(Dictionary<string, object> model)
    {
        return builder =>
        {
            var current = (T)model[Key]!;

            // Text
            if (typeof(T) == typeof(string))
            {
                builder.OpenComponent<MudTextField<string>>(0);
                builder.AddAttribute(1, "Label", Label);
                builder.AddAttribute(2, "Value", current as string);
                builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<string>(this, val => model[Key] = val));
                builder.AddAttribute(4, "Immediate", true);
                builder.CloseComponent();
            }
            // Numeric: int, double, decimal
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(double) || typeof(T) == typeof(decimal))
            {
                var compType = typeof(MudNumericField<>).MakeGenericType(typeof(T));
                builder.OpenComponent(0, compType);
                builder.AddAttribute(1, "Label", Label);
                builder.AddAttribute(2, "Value", current);
                builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<T>(this, val => model[Key] = val));
                builder.CloseComponent();
            }
            // Checkbox
            else if (typeof(T) == typeof(bool))
            {
                builder.OpenComponent<MudCheckBox<bool>>(0);
                builder.AddAttribute(1, "Label", Label);
                builder.AddAttribute(2, "Checked", (bool)(object)current);
                builder.AddAttribute(3, "CheckedChanged", EventCallback.Factory.Create<bool>(this, val => model[Key] = val));
                builder.CloseComponent();
            }
            // Date
            else if (typeof(T) == typeof(DateTime?))
            {
                builder.OpenComponent<MudDatePicker>(0);
                builder.AddAttribute(1, "Label", Label);
                builder.AddAttribute(2, "Date", (DateTime?)(object)current);
                builder.AddAttribute(3, "DateChanged", EventCallback.Factory.Create<DateTime?>(this, val => model[Key] = val));
                builder.CloseComponent();
            }
            else
            {
                builder.AddContent(0, $"No renderer for type {typeof(T).Name}");
            }
        };
    }
}

public class SelectFieldDefinition<T> : FieldDefinition<T>
{
    public IEnumerable<FieldOption<T>> Options { get; set; } = [];

    public override RenderFragment RenderFragment(Dictionary<string, object> model)
    {
        return builder =>
        {
            var current = (T)model[Key]!;
            var selectType = typeof(MudSelect<>).MakeGenericType(typeof(T));
            var itemType   = typeof(MudSelectItem<>).MakeGenericType(typeof(T));

            builder.OpenComponent(0, selectType);
            builder.AddAttribute(1, "Label", Label);
            builder.AddAttribute(2, "Value", current);
            builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<T>(this, val => model[Key] = val));
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(childBuilder =>
            {
                foreach (var opt in Options)
                {
                    childBuilder.OpenComponent(0, itemType);
                    childBuilder.AddAttribute(1, "Value", opt.Value);
                    childBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(__builder => __builder.AddContent(0, opt.Label)));
                    childBuilder.CloseComponent();
                }
            }));
            builder.CloseComponent();
        };
    }
}

public class FieldOption<T>
{
    public T Value { get; set; }
    public string Label { get; set; }
}