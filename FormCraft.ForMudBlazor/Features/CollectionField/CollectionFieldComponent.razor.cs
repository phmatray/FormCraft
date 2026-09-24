using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// A MudBlazor component that renders a collection (one-to-many) field with add, remove, reorder capabilities.
/// Each item in the collection is rendered as a sub-form using the configured item form fields.
/// </summary>
/// <typeparam name="TModel">The parent model type.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <remarks>
/// Item fields render through <see cref="IFieldRendererService"/>, exactly as ordinary fields do
/// (#203). This component used to build them by hand with a <c>RenderTreeBuilder</c> instead — a
/// second implementation of "render a field" that had to be taught every presentation attribute
/// separately, and silently lacked whichever one had most recently been added to the components.
/// That produced #146 (Variant), #177 (ShrinkLabel), #184 (adornments) and #190 (Required), each
/// found from a bug report rather than a test, and left a documented list of attributes the two
/// paths were known to disagree on. Deleting the second implementation is what closed the class.
/// </remarks>
public partial class CollectionFieldComponent<TModel, TItem>
    where TModel : new()
    where TItem : new()
{
    /// <summary>
    /// Each row's delete button, by <see cref="RowKey"/> — the focus targets for a removal (#318).
    /// </summary>
    /// <remarks>
    /// Keyed by row IDENTITY rather than held as a single reference because these controls are
    /// rendered per row: a removal has to focus the button that takes the vacated slot, which is a
    /// different one each time. Keyed by <see cref="RowKey"/> — not by plain index — since #334: the
    /// row's own <c>@key</c> can now move a surviving row's whole subtree (buttons included) to a
    /// different index, and a component's <c>@ref</c> fires only once, at creation, so an
    /// index-keyed dictionary would go stale the moment a row moved. Entries deliberately outlive the
    /// rows that produced them — see <see cref="DeleteButtonAt"/> for why pruning is the wrong fix and
    /// what guards staleness instead.
    /// </remarks>
    private readonly Dictionary<object, MudIconButton> _deleteButtons = new();

    /// <summary>Each row's reorder buttons, by <see cref="RowKey"/>. Same capture rules as <see cref="_deleteButtons"/>.</summary>
    private readonly Dictionary<object, MudIconButton> _moveUpButtons = new();

    /// <inheritdoc cref="_moveUpButtons"/>
    private readonly Dictionary<object, MudIconButton> _moveDownButtons = new();

    /// <summary>
    /// Each row's header element, by <see cref="RowKey"/> — the focus target when a row has no usable
    /// control to take focus. Element references, unlike component references, are re-captured every
    /// render, so keying them by row identity costs nothing extra; it is done anyway so every focus
    /// dictionary in this component shares one lookup key.
    /// </summary>
    private readonly Dictionary<object, ElementReference> _rowHeaders = new();

    /// <summary>
    /// Weak per-item tokens, minted once per item instance and reused for its lifetime — the
    /// mechanism behind <see cref="RowKey"/> (#334).
    /// </summary>
    /// <remarks>
    /// A <see cref="ConditionalWeakTable{TKey,TValue}"/> compares KEYS by reference, never by
    /// <c>Equals</c> — the property #308's reverted <c>@key="Items[index]"</c> was missing. That
    /// attempt keyed on the item itself, so two rows whose items compared EQUAL (a <c>record</c>, a
    /// <c>struct</c>, or any <c>Equals</c>-overriding class) collided into Blazor's duplicate-key
    /// render exception. A token is a fresh, unique <see cref="object"/> minted once per item
    /// instance and never compared by value, so two equal-by-value rows still get two different
    /// tokens. Weak, and never written to by this component's own Add/Remove/Move: it stays correct
    /// even when <see cref="Items"/> — the CALLER's list — is mutated from outside, and an item that
    /// leaves the list is collected normally once nothing else references it.
    /// </remarks>
    private readonly ConditionalWeakTable<object, object> _rowTokens = new();

    /// <summary>The <b>Add</b> button, when one is rendered — the second focus target in the chain.</summary>
    private MudButton? _addButton;

    /// <summary>
    /// The index a row was just removed from, pending the focus move on the next completed render.
    /// </summary>
    private int? _focusAfterRemovalFrom;

    /// <summary>
    /// The row index to put focus in on the next completed render, after an add or a reorder.
    /// </summary>
    /// <remarks>
    /// Deferred for the same reason as <see cref="_focusAfterRemovalFrom"/>: the row the focus is
    /// aimed at may not exist — or may not be at that index — until the next render batch is applied.
    /// </remarks>
    private int? _focusRowAfterRender;

    /// <summary>
    /// Which way the item travelled, when <see cref="_focusRowAfterRender"/> came from a reorder;
    /// <see langword="null"/> when it came from an add.
    /// </summary>
    /// <remarks>
    /// The direction matters, it is not bookkeeping: focus must land on the button for the way the
    /// user was already going, so pressing <kbd>Enter</kbd> again keeps moving the item. Preferring
    /// <b>up</b> regardless would put a repeat keypress on "undo the move I just made" — the same
    /// hazard that keeps focus off Delete after an add.
    /// </remarks>
    private bool? _reorderMovedDown;

    /// <summary>
    /// The collection's header, the last-resort focus target when an action leaves the field with no
    /// button at all. Carries <c>tabindex="-1"</c> so it can take focus without joining the tab order.
    /// </summary>
    private ElementReference _header;

    /// <summary>
    /// Gets or sets the parent model instance.
    /// </summary>
    [Parameter]
    public TModel Model { get; set; } = default!;

    /// <summary>
    /// Gets or sets the collection field configuration.
    /// </summary>
    [Parameter]
    public ICollectionFieldConfiguration<TModel, TItem> Configuration { get; set; } = default!;

    /// <summary>
    /// Gets or sets the callback invoked when the collection changes (items added, removed, or reordered).
    /// </summary>
    [Parameter]
    public EventCallback OnCollectionChanged { get; set; }

    /// <summary>
    /// Gets or sets the parent form's EditContext, cascaded from the surrounding EditForm.
    /// When present, item field changes raise <see cref="EditContext.NotifyFieldChanged(in FieldIdentifier)"/>
    /// with a nested field identifier (e.g. <c>Items[0].ProductName</c>) on the root model, so
    /// modification tracking and Blazor's validation infrastructure see collection item edits.
    /// </summary>
    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>
    /// The nested context cascaded to every item field this collection renders (#203).
    /// </summary>
    /// <remarks>
    /// Created once and reused: it carries a once-per-field diagnostic latch, and a scope rebuilt
    /// on each render would reset that latch and reintroduce the per-row warning flood it exists to
    /// prevent. Rebuilt only if the collection is repointed at a different field, which would make
    /// the old name — and the keys derived from it — wrong.
    /// </remarks>
    private CollectionItemFieldScope? _itemFieldScope;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (_itemFieldScope is null || _itemFieldScope.CollectionName != Configuration.FieldName)
        {
            _itemFieldScope = new CollectionItemFieldScope(Configuration.FieldName);
        }
    }

    /// <summary>
    /// The collection this component renders, read through
    /// <see cref="ICollectionFieldConfiguration{TModel, TItem}.CollectionAccessor"/>.
    /// </summary>
    /// <remarks>
    /// Read on essentially the first line of this component's own markup (<c>Items.Count == 0</c>),
    /// so a nested binding with a null intermediate (e.g. <c>x => x.Details.Items</c> where
    /// <c>Details</c> is null) used to crash the component the instant it rendered — before
    /// validation ever ran (#419). Mirrors <c>CollectionFieldValidator.TryReadCollection</c>'s
    /// "unreadable collection validates as zero items" treatment on the validation path (#408), but
    /// as a local <c>try</c>/<c>catch</c> rather than a call to the same helper:
    /// <c>FieldValueGetterCache&lt;TModel&gt;.TryInvoke</c> is <c>internal</c> to <c>FormCraft</c>
    /// core, and this assembly has no <c>InternalsVisibleTo</c> to it. Catches
    /// <see cref="NullReferenceException"/> specifically — not <see cref="Exception"/> broadly, unlike
    /// <c>TryReadCollection</c> — so a genuinely broken accessor, or a cancellation, still propagates
    /// instead of being swallowed here.
    /// </remarks>
    private List<TItem> Items
    {
        get
        {
            try
            {
                return Configuration.CollectionAccessor(Model);
            }
            catch (NullReferenceException)
            {
                return new List<TItem>();
            }
        }
    }

    /// <summary>
    /// This row's identity (#334) — a per-item weak token for a reference-type item that appears
    /// only once in <see cref="Items"/>; a boxed <paramref name="index"/> otherwise. Used both as the
    /// row's <c>@key</c> and as the lookup key for every focus dictionary above, so "the button now
    /// at index N" and "the button for the row Blazor rendered at N" always name the same object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The boxed-<c>int</c> fallback is not a cop-out: Blazor's keyed reconciliation compares keys
    /// with <c>Equals</c>/<c>GetHashCode</c> (<c>EqualityComparer&lt;object&gt;.Default</c>), and a
    /// boxed <see cref="int"/> compares by VALUE across renders — so a loop whose length does not
    /// change out from under a given index reconciles identically to an unkeyed one. That is exactly
    /// what a value-typed <typeparamref name="TItem"/> gets: it boxes fresh on every access, so no
    /// reference-stable identity exists to hand <see cref="_rowTokens"/>, and today's positional
    /// behaviour is preserved rather than approximated.
    /// </para>
    /// <para>
    /// The other fallback case is the SAME item object instance appearing more than once in
    /// <see cref="Items"/> right now (<see cref="HasDuplicateReference"/>) — legal for a reference
    /// type, and it would otherwise mint one token for two rows, which is the duplicate-key crash a
    /// keyed loop exists to avoid (#308). Both checks read <see cref="Items"/> live, not a snapshot,
    /// so they stay correct under mutation this component was never told about.
    /// </para>
    /// </remarks>
    private object RowKey(int index)
    {
        if (typeof(TItem).IsValueType)
        {
            return index;
        }

        var item = Items[index];
        if (item is null || HasDuplicateReference(item, index))
        {
            return index;
        }

        return _rowTokens.GetValue(item, _ => new object());
    }

    // ponytail: O(n) scan per row (O(n^2) per render) — collections here are short, hand-built lists
    // a user assembles by clicking "Add item", not bulk data. Switch to a single reference-identity
    // counting pass over Items if that stops being true.
    private bool HasDuplicateReference(TItem item, int index)
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (i != index && ReferenceEquals(Items[i], item))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasReachedMax => Configuration.MaxItems > 0 && Items.Count >= Configuration.MaxItems;

    private bool HasReachedMin => Configuration.MinItems > 0 && Items.Count <= Configuration.MinItems;

    private List<string> ValidationErrors { get; set; } = new();

    private async Task AddItem()
    {
        if (HasReachedMax)
        {
            return;
        }

        Items.Add(new TItem());
        await NotifyCollectionChanged();

        // ONLY when this add unmounted Add itself. MaxItems defaults to 0, so HasReachedMax is
        // normally never true and the button survives — with focus still on it, which is where a
        // user building a list wants to stay. Moving focus anyway would push them into the new row's
        // header (tabindex="-1", outside the tab order), forcing a Shift+Tab back to Add for every
        // subsequent row: a regression in the common case, in the name of a failure that did not
        // happen (#318).
        if (!HasReachedMax)
        {
            return;
        }

        _focusRowAfterRender = Items.Count - 1;
        _reorderMovedDown = null;
    }

    private async Task RemoveItem(int index)
    {
        if (HasReachedMin)
        {
            return;
        }

        if (index < 0 || index >= Items.Count)
        {
            return;
        }

        Items.RemoveAt(index);
        await NotifyCollectionChanged();

        // The delete button the user activated has just unmounted — and if this removal reached
        // MinItems, so has every other row's. Move focus deliberately or it falls to <body> (#318).
        // Deferred to OnAfterRenderAsync rather than done here: the @ref captures are only re-bound
        // when the next render batch is applied, so reading them now would hand back the buttons
        // from *before* the removal — which is exactly a detached one in the MinItems case.
        _focusAfterRemovalFrom = index;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // Take and clear BOTH pending requests before dispatching either. They can both be set:
        // AddItem/MoveItem* await NotifyCollectionChanged(), which releases the Blazor dispatcher
        // when the consumer's handler is genuinely async, so a delete click can be processed in that
        // window. Leaving the other flag set lets it fire on some unrelated later render — e.g.
        // typing in an item field re-renders and focus is yanked out of the input into a row header.
        var removedIndex = _focusAfterRemovalFrom;
        var rowIndex = _focusRowAfterRender;
        var movedDown = _reorderMovedDown;
        _focusAfterRemovalFrom = null;
        _focusRowAfterRender = null;
        _reorderMovedDown = null;

        if (removedIndex is { } removed)
        {
            await FocusAfterRemovalAsync(removed);
            return;
        }

        if (rowIndex is { } row)
        {
            await FocusRowAsync(row, movedDown);
        }
    }

    /// <summary>
    /// Puts focus in the row at <paramref name="index"/> after an add or a reorder.
    /// </summary>
    /// <param name="index">The row the acting item now occupies.</param>
    /// <param name="movedDown">
    /// Which way the item travelled for a reorder, or <see langword="null"/> after an <b>add</b>.
    /// A reorder prefers a still-enabled move button on that row — it keeps the user on the control
    /// they were operating. After an add the row itself is the target instead: the row's fields are
    /// what the user wants next, and its Delete button would put <kbd>Enter</kbd> on "undo the add".
    /// </param>
    private async Task FocusRowAsync(int index, bool? movedDown)
    {
        if (movedDown is { } down)
        {
            var target = EnabledMoveButtonAt(index, down);
            if (target is not null)
            {
                await FocusRestore.FocusSafelyAsync(target);
                return;
            }
        }

        if (_rowHeaders.TryGetValue(RowKey(index), out var header))
        {
            await FocusRestore.FocusSafelyAsync(header);
        }
    }

    /// <summary>
    /// A still-enabled move button on the given row, preferring the direction the item just
    /// travelled and falling back to its counterpart when that one has become disabled at an end.
    /// </summary>
    /// <remarks>
    /// ⚠️ The preference is the point, not a nicety: landing on the button for the direction the
    /// user was already going means a repeat <kbd>Enter</kbd> keeps moving the item. Always
    /// preferring <b>up</b> — which this did until review caught it — puts that repeat keypress on
    /// "undo the move I just made" whenever the item travelled down into a mid-list slot.
    /// </remarks>
    private MudIconButton? EnabledMoveButtonAt(int index, bool movedDown)
    {
        if (!Configuration.CanReorder || index < 0 || index >= Items.Count)
        {
            return null;
        }

        // Mirrors the Disabled bindings in the markup: up is dead at the top, down at the bottom.
        var upEnabled = index > 0;
        var downEnabled = index < Items.Count - 1;

        var key = RowKey(index);
        var travelled = movedDown ? _moveDownButtons : _moveUpButtons;
        var travelledEnabled = movedDown ? downEnabled : upEnabled;
        if (travelledEnabled && travelled.TryGetValue(key, out var preferred))
        {
            return preferred;
        }

        var counterpart = movedDown ? _moveUpButtons : _moveDownButtons;
        var counterpartEnabled = movedDown ? upEnabled : downEnabled;
        return counterpartEnabled && counterpart.TryGetValue(key, out var fallback) ? fallback : null;
    }

    /// <summary>
    /// Focus target after a row is removed: the delete button that takes the vacated slot, else the
    /// previous row's, else <b>Add</b>, else the collection header.
    /// </summary>
    /// <remarks>
    /// The chain matters because a removal can unmount far more than the button that was clicked:
    /// reaching <c>MinItems</c> falsifies the <c>@if</c> guarding <i>every</i> row's delete button at
    /// once, and a field that also forbids adding is then left with no focusable control at all.
    /// </remarks>
    private async Task FocusAfterRemovalAsync(int removedIndex)
    {
        var survivor = DeleteButtonAt(removedIndex) ?? DeleteButtonAt(removedIndex - 1);
        if (survivor is not null)
        {
            await FocusRestore.FocusSafelyAsync(survivor);
            return;
        }

        if (_addButton is not null)
        {
            await FocusRestore.FocusSafelyAsync(_addButton);
            return;
        }

        await FocusRestore.FocusSafelyAsync(_header);
    }

    /// <summary>
    /// Whether delete buttons are rendered at all right now — the same condition the markup gates
    /// them on.
    /// </summary>
    private bool DeleteButtonsRendered => Configuration.CanRemove && !HasReachedMin;

    /// <summary>
    /// The delete button for the row currently at <paramref name="index"/>, or
    /// <see langword="null"/> when that row no longer exists or delete is not rendered at all.
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Staleness is handled by these checks, not by pruning <see cref="_deleteButtons"/>.</b> A
    /// <c>@ref</c> on a <i>component</i> is captured when that component is created and is
    /// <b>not</b> re-run on later renders, so clearing the dictionary per render permanently loses
    /// the references for rows that were merely retained — measured: every removal then fell through
    /// to <b>Add</b>. Entries therefore outlive the rows they came from, and correctness comes from
    /// asking what is rendered <i>now</i>: the index must still be within <c>Items</c>, delete must
    /// still be rendered at all (reaching <c>MinItems</c> unmounts every one of them at once), and
    /// the lookup goes through <see cref="RowKey"/> — computed from <b>today's</b>
    /// <c>Items[index]</c> — rather than the index itself, so a row <c>@key</c> has moved to a new
    /// position (#334) is still found by the button its OWN item was given, not by whatever button
    /// used to occupy this slot.
    /// </remarks>
    private MudIconButton? DeleteButtonAt(int index) =>
        DeleteButtonsRendered
        && index >= 0
        && index < Items.Count
        && _deleteButtons.TryGetValue(RowKey(index), out var button)
            ? button
            : null;

    private async Task MoveItemUp(int index)
    {
        if (index <= 0 || index >= Items.Count)
        {
            return;
        }

        (Items[index], Items[index - 1]) = (Items[index - 1], Items[index]);
        await NotifyCollectionChanged();

        // Follow the item to its new row. At index 0 the Move-up button the user pressed becomes
        // Disabled under their finger, and browsers drop focus from a newly-disabled element — the
        // same 2.4.3 failure as an unmount (#318).
        _focusRowAfterRender = index - 1;
        _reorderMovedDown = false;
    }

    private async Task MoveItemDown(int index)
    {
        if (index < 0 || index >= Items.Count - 1)
        {
            return;
        }

        (Items[index], Items[index + 1]) = (Items[index + 1], Items[index]);
        await NotifyCollectionChanged();

        _focusRowAfterRender = index + 1;
        _reorderMovedDown = true;
    }

    private async Task NotifyCollectionChanged()
    {
        if (OnCollectionChanged.HasDelegate)
        {
            await OnCollectionChanged.InvokeAsync();
        }

        StateHasChanged();
    }

    private async Task UpdateItemFieldValue(int itemIndex, string fieldName, object? value)
    {
        if (itemIndex < 0 || itemIndex >= Items.Count)
        {
            return;
        }

        var item = Items[itemIndex];
        var property = typeof(TItem).GetProperty(fieldName);
        if (property != null)
        {
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var convertedValue = value;

            if (value != null && value.GetType() != targetType)
            {
                try
                {
                    convertedValue = Convert.ChangeType(value, targetType);
                }
                catch
                {
                    // If conversion fails, use the value as-is
                }
            }

            property.SetValue(item, convertedValue);

            // Notify the parent EditContext with a nested field identifier
            // (Blazor convention: model stays the root model, the field name
            // encodes the collection path, e.g. "Items[0].ProductName") so
            // IsModified tracking and validation messages work natively.
            if (EditContext != null && Model is not null)
            {
                EditContext.NotifyFieldChanged(GetItemFieldIdentifier(itemIndex, fieldName));
            }
        }

        await NotifyCollectionChanged();
    }

    private FieldIdentifier GetItemFieldIdentifier(int itemIndex, string fieldName)
        => new(Model!, $"{Configuration.FieldName}[{itemIndex}].{fieldName}");

    private RenderFragment RenderItemFields(int itemIndex)
    {
        return builder =>
        {
            if (Configuration.ItemFormConfiguration == null)
            {
                return;
            }

            var item = Items[itemIndex];

            foreach (var field in Configuration.ItemFormConfiguration.Fields.OrderBy(f => f.Order))
            {
                var capturedIndex = itemIndex;
                var capturedFieldName = field.FieldName;

                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "mb-3");
                builder.AddContent(2, RenderItemField(item, field, capturedIndex));
                // Surface validation messages attached to the nested field identifier
                // (e.g. Items[0].ProductName) next to the item field input.
                builder.OpenComponent<FieldValidationMessage>(3);
                builder.AddAttribute(4, "FieldName", $"{Configuration.FieldName}[{capturedIndex}].{capturedFieldName}");
                builder.CloseComponent();
                builder.CloseElement();
            }
        };
    }

    /// <summary>
    /// Renders one item field through <see cref="IFieldRendererService"/> — the same selector, and
    /// therefore the same per-type component, that an ordinary field renders through (#203).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the whole of the convergence, and the reason the file below it is gone. What stood
    /// here before was a type switch over <c>string</c>/<c>int</c>/<c>decimal</c>/… dispatching to
    /// hand-written <c>RenderTreeBuilder</c> methods, each re-deriving the presentation attributes
    /// that <c>MudBlazorFieldComponentBase</c> already resolves. A missing arm did not degrade
    /// gracefully either: it emitted no frames at all, which is how four numeric types rendered
    /// nothing whatsoever until #209.
    /// </para>
    /// <para>
    /// No nested-model machinery is needed to do this: the service is generic over the model and an
    /// item field is already configured as <c>IFieldConfiguration&lt;TItem, object&gt;</c>, so the
    /// ITEM is passed as the model and the selected component binds against
    /// <typeparamref name="TItem"/> directly.
    /// </para>
    /// <para>
    /// What the item does not carry is the parent's identity, which is why the value callback still
    /// goes through <see cref="UpdateItemFieldValue"/>: that writes the property on the item and then
    /// notifies the parent <see cref="EditContext"/> under the nested
    /// <c>&lt;collection&gt;[i].&lt;field&gt;</c> identifier (#91). Binding against the item while
    /// reporting against the parent is the entire "nested context" this path requires; the rest of
    /// it — which collection a field belongs to, and whether it has already warned — is cascaded as
    /// a <see cref="CollectionItemFieldScope"/>.
    /// </para>
    /// </remarks>
    private RenderFragment RenderItemField(TItem item, IFieldConfiguration<TItem, object> field, int itemIndex)
        => FieldRendererService.RenderField(
            item,
            field,
            EventCallback.Factory.Create<object?>(
                this, value => UpdateItemFieldValue(itemIndex, field.FieldName, value)),
            EventCallback.Factory.Create(this, NotifyCollectionChanged));
}
