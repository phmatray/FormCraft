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

    /// <summary>
    /// Returns a field configuration's actual value type, resolving it at most once per configuration
    /// instance (#314) — the same per-instance-cache technique <see cref="FieldValueGetterCache{TModel}"/>
    /// (#269, #312) uses for the compiled value getter.
    /// </summary>
    private static Type GetActualFieldType<TModel>(IFieldConfiguration<TModel, object> field)
        => FieldTypeCache<TModel>.Cache.GetValue(field, static configuration => ResolveActualFieldType(configuration));

    /// <summary>
    /// Computes a field configuration's actual value type. A <see cref="FieldConfigurationWrapper{TModel, TValue}"/>
    /// answers directly through <see cref="IActualFieldTypeSource"/> — a plain interface dispatch, not
    /// the reflective <c>GetMethod</c> + <c>MethodInfo.Invoke</c> this replaced (#314) — identifying the
    /// wrapper by what it implements rather than by a substring of its type name. Any other
    /// <see cref="IFieldConfiguration{TModel, TValue}"/> implementation falls back to reading its
    /// <see cref="IFieldConfiguration{TModel, TValue}.ValueExpression"/> body.
    /// </summary>
    private static Type ResolveActualFieldType<TModel>(IFieldConfiguration<TModel, object> field)
    {
        if (field is IActualFieldTypeSource typeSource)
        {
            return typeSource.GetActualFieldType();
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

    /// <summary>
    /// Caches each field configuration's resolved actual type, keyed by configuration <b>instance</b> —
    /// so two configurations over the same property never share an entry, and an entry lives no longer
    /// than the configuration it describes (#314).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately separate from <see cref="FieldValueGetterCache{TModel}"/>: that cache lives in
    /// <c>Forms/Core</c> because both this renderer and the validators (#312) read it, whereas the
    /// resolved field type is read only from <see cref="RenderField{TModel}"/>, so keeping this table
    /// local avoids widening a cache the validators have no use for.
    /// </para>
    /// <para>
    /// <b>What is deliberately NOT cached here.</b> The <see cref="MinimalFieldConfiguration"/>
    /// projection built in <see cref="RenderField{TModel}"/>, and the renderer selected from it, both
    /// read <i>mutable</i> configuration properties — <c>IsRequired</c>, <c>IsVisible</c>,
    /// <c>IsDisabled</c>, <c>IsReadOnly</c>, <c>Label</c>, <c>Placeholder</c>, <c>InputType</c>,
    /// <c>CssClass</c>, <c>Order</c> — that can legitimately change between renders. Memoizing either
    /// would pin a stale renderer choice. Only the resolved field <b>type</b> is a pure function of the
    /// configuration instance, which is what makes it — and only it — safe to cache this way.
    /// </para>
    /// </remarks>
    private static class FieldTypeCache<TModel>
    {
        internal static readonly ConditionalWeakTable<IFieldConfiguration<TModel, object>, Type> Cache = new();
    }

    private static object GetCurrentValue<TModel>(TModel model, IFieldConfiguration<TModel, object> field)
    {
        // Shared with the validators since #312 — see FieldValueGetterCache for why the cache lives
        // there rather than on the configuration. A field that is both rendered and validated
        // therefore compiles its getter once in total, not once per path.
        var getter = FieldValueGetterCache<TModel>.GetOrCompile(field);
        return getter(model);
    }
}
