using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Default implementation of the field renderer service that coordinates multiple field renderers.
/// </summary>
public class FieldRendererService : IFieldRendererService
{
    private readonly IEnumerable<IFieldRenderer> _renderers;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the FieldRendererService class.
    /// </summary>
    /// <param name="renderers">Collection of field renderers available for rendering different field types.</param>
    /// <param name="serviceProvider">Service provider for resolving custom renderers.</param>
    public FieldRendererService(IEnumerable<IFieldRenderer> renderers, IServiceProvider serviceProvider)
    {
        _renderers = renderers;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public RenderFragment RenderField<TModel>(TModel model, IFieldConfiguration<TModel, object> field,
        EventCallback<object?> onValueChanged, EventCallback onDependencyChanged)
    {
        var fieldType = GetActualFieldType(field);
        var currentValue = GetCurrentValue(model, field);

        var context = new FieldRenderContext<TModel>
        {
            Model = model,
            Field = field,
            ActualFieldType = fieldType,
            CurrentValue = currentValue,
            OnValueChanged = onValueChanged,
            OnDependencyChanged = onDependencyChanged,
        };

        // A renderer instance supplied via WithCustomRenderer(IFieldRenderer) takes
        // absolute precedence - the caller handed us the exact object to use.
        if (field.AdditionalAttributes.TryGetValue("CustomRendererInstance", out var rendererInstance))
        {
            if (rendererInstance is IFieldRenderer suppliedFieldRenderer)
            {
                return suppliedFieldRenderer.Render(context);
            }

            if (rendererInstance is ICustomFieldRenderer suppliedCustomRenderer &&
                IsValidForFieldType(suppliedCustomRenderer, fieldType))
            {
                return suppliedCustomRenderer.Render(context);
            }
        }

        // Check for custom renderer first
        if (field.CustomRendererType != null)
        {
            var customRenderer = TryResolveCustomRenderer(field.CustomRendererType, fieldType);
            if (customRenderer != null)
            {
                return customRenderer.Render(context);
            }
        }

        // Fall back to standard renderers. CanRender receives an object-typed
        // projection of the real configuration so renderers can dispatch on
        // metadata (InputType, IsRequired, ...) and not just AdditionalAttributes.
        var minimalFieldConfig = new MinimalFieldConfiguration
        {
            FieldName = field.FieldName,
            Label = field.Label,
            Placeholder = field.Placeholder,
            HelpText = field.HelpText,
            InputType = field.InputType,
            IsRequired = field.IsRequired,
            IsReadOnly = field.IsReadOnly,
            IsDisabled = field.IsDisabled,
            IsVisible = field.IsVisible,
            CssClass = field.CssClass,
            Order = field.Order,
            CustomRendererType = field.CustomRendererType,
            AdditionalAttributes = field.AdditionalAttributes
        };

        var renderer = _renderers.FirstOrDefault(r => r.CanRender(fieldType, minimalFieldConfig));
        if (renderer != null)
        {
            return renderer.Render(context);
        }

        return builder => builder.AddContent(0, $"Unsupported field type: {fieldType.Name} for field: {field.FieldName}");
    }

    private ICustomFieldRenderer? TryResolveCustomRenderer(Type rendererType, Type fieldType)
    {
        try
        {
            // First try to resolve from service provider
            var renderer = _serviceProvider.GetService(rendererType);
            if (renderer is ICustomFieldRenderer customRenderer && IsValidForFieldType(customRenderer, fieldType))
            {
                return customRenderer;
            }

            // If not registered, try to create an instance
            if (rendererType.GetConstructor(Type.EmptyTypes) != null)
            {
                var instance = Activator.CreateInstance(rendererType);
                if (instance is ICustomFieldRenderer createdRenderer && IsValidForFieldType(createdRenderer, fieldType))
                {
                    return createdRenderer;
                }
            }
        }
        catch
        {
            // Log error or handle appropriately
        }

        return null;
    }

    private static bool IsValidForFieldType(ICustomFieldRenderer renderer, Type fieldType)
    {
        // A renderer declared for a value type must also serve the nullable
        // variant of that type (int? fields with an int renderer).
        return renderer.ValueType.IsAssignableFrom(fieldType) ||
               (Nullable.GetUnderlyingType(fieldType) is { } underlyingType &&
                renderer.ValueType.IsAssignableFrom(underlyingType));
    }

    private static Type GetActualFieldType<TModel>(IFieldConfiguration<TModel, object> field)
    {
        var wrapperType = field.GetType();
        if (wrapperType.IsGenericType && wrapperType.GetGenericTypeDefinition().Name.Contains("FieldConfigurationWrapper"))
        {
            var getActualFieldTypeMethod = wrapperType.GetMethod("GetActualFieldType");
            if (getActualFieldTypeMethod != null)
            {
                return (Type)getActualFieldTypeMethod.Invoke(field, null)!;
            }

            var property = typeof(TModel).GetProperty(field.FieldName);
            return property?.PropertyType ?? typeof(object);
        }

        var expressionBody = field.ValueExpression.Body;

        return expressionBody switch
        {
            MemberExpression
            {
                Member: PropertyInfo propertyInfo
            } => propertyInfo.PropertyType,
            UnaryExpression
            {
                NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked,
                Operand: MemberExpression { Member: PropertyInfo unaryPropertyInfo }
            } => unaryPropertyInfo.PropertyType,
            _ => expressionBody.Type
        };
    }

    private static object GetCurrentValue<TModel>(TModel model, IFieldConfiguration<TModel, object> field)
    {
        var getter = ValueGetterCache<TModel>.GetOrCompile(field);
        return getter(model);
    }

    /// <summary>
    /// Caches the compiled value getter of each field configuration, so rendering a field emits IL
    /// for its expression at most once rather than once per render (#269).
    /// </summary>
    private static class ValueGetterCache<TModel>
    {
        /// <summary>
        /// Keyed by configuration <b>instance</b>, which buys two properties: two configurations over
        /// the same property never share an entry, and an entry lives no longer than the configuration
        /// it describes, so nothing is held alive artificially.
        /// </summary>
        /// <remarks>
        /// What is cached is the <b>getter</b>, never the value it returns. The delegate takes the
        /// model as its parameter, so every render still reads the model afresh — caching a value here
        /// would freeze each field at its first-rendered content.
        /// <para>
        /// The entry is never invalidated, so this assumes a configuration's <c>ValueExpression</c>
        /// keeps reading the same member for that configuration's lifetime. That is the fluent
        /// builder's immutable-after-<c>Build()</c> contract, and <see cref="FieldConfiguration{TModel, TValue}" />
        /// enforces it by assigning the expression in its constructor. A caller that hands
        /// <see cref="IFieldRendererService.RenderField" /> a configuration which re-targets its
        /// expression after first render would keep seeing the original member; use a separate
        /// configuration instance per binding instead.
        /// </para>
        /// </remarks>
        private static readonly ConditionalWeakTable<IFieldConfiguration<TModel, object>, Func<TModel, object>> Cache = new();

        internal static Func<TModel, object> GetOrCompile(IFieldConfiguration<TModel, object> field)
            => Cache.GetValue(field, static configuration => configuration.ValueExpression.Compile());
    }
}
