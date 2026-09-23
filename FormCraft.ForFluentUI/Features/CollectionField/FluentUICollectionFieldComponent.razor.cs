using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a collection (one-to-many) field as a Fluent card with add, remove and reorder controls.
/// Each item is rendered as a sub-form from the configured item form fields.
/// </summary>
/// <typeparam name="TModel">The parent model type.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <remarks>
/// Item fields render through <see cref="IFieldRendererService"/>, exactly as ordinary fields do
/// (#203, #278). That is the whole of the implementation: this component owns the collection
/// chrome - the card, the add/remove/reorder buttons, the empty state - and knows nothing about
/// field types. A field type the adapter registers works inside a collection by construction.
/// <para>
/// ⛔ Do not add a type switch here that builds item fields with a <c>RenderTreeBuilder</c>. The
/// MudBlazor adapter carried one until #203, and every presentation attribute had to be taught to
/// it separately; #146 (Variant), #177 (ShrinkLabel), #184 (adornments), #190 (Required) and #209
/// (four numeric types rendering nothing at all) were each found from a bug report rather than a
/// test. Starting a second one in this adapter would restart that sequence from zero.
/// </para>
/// </remarks>
public partial class FluentUICollectionFieldComponent<TModel, TItem> : IAsyncDisposable
    where TModel : new()
    where TItem : new()
{
    /// <summary>The parent model instance.</summary>
    [Parameter]
    public TModel Model { get; set; } = default!;

    /// <summary>The collection field configuration.</summary>
    [Parameter]
    public ICollectionFieldConfiguration<TModel, TItem> Configuration { get; set; } = default!;

    /// <summary>Invoked when the collection changes (items added, removed or reordered).</summary>
    [Parameter]
    public EventCallback OnCollectionChanged { get; set; }

    /// <summary>
    /// The parent form's <see cref="EditContext"/>, cascaded from the surrounding EditForm. When
    /// present, item field changes raise <see cref="EditContext.NotifyFieldChanged(in FieldIdentifier)"/>
    /// with a nested identifier (e.g. <c>Lines[0].Product</c>) on the root model, so modification
    /// tracking and Blazor's validation infrastructure see collection item edits (#91).
    /// </summary>
    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>The JS runtime used to focus a <c>FluentButton</c> by id (#383). See <see cref="_idPrefix"/>.</summary>
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Lazily-imported handle to <c>collectionFocus.js</c>, cached for the component's lifetime.
    /// </summary>
    private IJSObjectReference? _focusModule;

    /// <summary>
    /// A per-instance prefix for the DOM ids this component assigns to its four <c>FluentButton</c>
    /// controls, so two rendered instances of this component (or two rows) never collide (#383,
    /// mirrors the reasoning behind the file-upload hint id in <c>MudBlazorFileUploadComponentBase</c>).
    /// </summary>
    private readonly string _idPrefix = $"formcraft-collection-{Guid.NewGuid():N}";

    /// <summary>
    /// The collection's header wrapper, the last-resort focus target when an action leaves the field
    /// with no control at all (#337, mirrors <c>FormCraft.ForMudBlazor</c>'s <c>_header</c>). Carries
    /// <c>tabindex="-1"</c> in the markup so it can take focus without joining the tab order.
    /// </summary>
    /// <remarks>
    /// <c>internal</c> rather than a leading-underscore private field so the focus-assertion test
    /// suite can read the exact <see cref="ElementReference"/> the component would itself focus (see
    /// <c>FormCraft.ForFluentUI.UnitTests.TestSupport.FocusAssertingTestBase</c> remarks). Unlike the
    /// four <c>FluentButton</c> controls (#383), this stays a plain <c>&lt;div&gt;</c> with its own
    /// <see cref="ElementReference"/> — it is a deliberately non-interactive landing spot, not a
    /// control, so it is out of this issue's scope.
    /// </remarks>
    internal ElementReference HeaderTarget;

    /// <summary>
    /// The id of the rendered <b>Add</b> control, when one is rendered — the second focus target in
    /// the removal chain (#337, mirrors <c>FormCraft.ForMudBlazor</c>'s <c>_addButton</c>). Computed
    /// live rather than captured via <c>@ref</c>: <c>FluentButton</c> exposes no
    /// <see cref="ElementReference"/> of its own to capture (#383, see <c>FocusRestore</c> remarks).
    /// </summary>
    internal string? AddTargetId => Configuration.CanAdd && !HasReachedMax ? $"{_idPrefix}-add" : null;

    /// <summary>
    /// The index a row was just removed from, pending the focus move on the next completed render.
    /// </summary>
    private int? _focusAfterRemovalFrom;

    /// <summary>
    /// Each row's header wrapper, by index — the focus target when a row has no usable control to
    /// take focus (#337, mirrors <c>FormCraft.ForMudBlazor</c>'s <c>_rowHeaders</c>).
    /// </summary>
    private readonly Dictionary<int, ElementReference> _rowHeaderTargets = new();

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
    /// The direction matters, it is not bookkeeping: focus must land on the control for the way the
    /// user was already going, so pressing <kbd>Enter</kbd> again keeps moving the item. Preferring
    /// <b>up</b> regardless would put a repeat keypress on "undo the move I just made" — the same
    /// hazard that keeps focus off Delete after an add.
    /// </remarks>
    private bool? _reorderMovedDown;

    private List<TItem> Items => Configuration.CollectionAccessor(Model);

    private bool HasReachedMax => Configuration.MaxItems > 0 && Items.Count >= Configuration.MaxItems;

    private bool HasReachedMin => Configuration.MinItems > 0 && Items.Count <= Configuration.MinItems;

    private bool DeleteTargetsRendered => Configuration.CanRemove && !HasReachedMin;

    /// <summary>
    /// The id of the delete control rendered at <paramref name="index"/>, or <see langword="null"/>
    /// when that row no longer exists or delete is not rendered at all.
    /// </summary>
    /// <remarks>
    /// Computed live rather than captured via <c>@ref</c> (#383, see <see cref="AddTargetId"/>):
    /// correctness comes from asking what is rendered <i>now</i>, the same two checks the old
    /// dictionary-staleness guard used — the index must still be within <see cref="Items"/>, and
    /// delete must still be rendered at all (reaching <c>MinItems</c> unmounts every one of them at
    /// once).
    /// </remarks>
    internal string? DeleteTargetIdAt(int index) =>
        DeleteTargetsRendered && index >= 0 && index < Items.Count
            ? $"{_idPrefix}-delete-{index}"
            : null;

    /// <summary>The id of the Move-up control at <paramref name="index"/> — test-only access, mirroring <see cref="DeleteTargetIdAt"/>.</summary>
    internal string MoveUpTargetIdAt(int index) => $"{_idPrefix}-move-up-{index}";

    /// <inheritdoc cref="MoveUpTargetIdAt"/>
    internal string MoveDownTargetIdAt(int index) => $"{_idPrefix}-move-down-{index}";

    /// <summary>The row header wrapper at <paramref name="index"/> — test-only access, mirroring <see cref="DeleteTargetIdAt"/>.</summary>
    internal ElementReference RowHeaderTargetAt(int index) => _rowHeaderTargets[index];

    /// <summary>
    /// Focuses the <c>FluentButton</c> with DOM id <paramref name="id"/> via <c>collectionFocus.js</c>
    /// (#383, see <c>FocusRestore</c> remarks for why this cannot go through
    /// <see cref="ElementReference"/> instead).
    /// </summary>
    private async Task FocusByIdAsync(string id) =>
        await FocusRestore.FocusSafelyAsync(async () =>
        {
            var module = await GetFocusModuleAsync();
            await module.InvokeVoidAsync("focusById", id);
        });

    private async ValueTask<IJSObjectReference> GetFocusModuleAsync() =>
        _focusModule ??= await JS.InvokeAsync<IJSObjectReference>(
            "import", "./_content/FormCraft.ForFluentUI/js/collectionFocus.js");

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_focusModule is null)
        {
            return;
        }

        try
        {
            await _focusModule.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone; there is nothing left to release.
        }
    }

    private async Task AddItem()
    {
        if (HasReachedMax)
        {
            return;
        }

        Items.Add(new TItem());
        await NotifyCollectionChanged();

        // ONLY when this add unmounted Add itself. MaxItems defaults to 0, so HasReachedMax is
        // normally never true and the control survives - with focus still on it, which is where a
        // user building a list wants to stay. Moving focus anyway would push them into the new row's
        // header (tabindex="-1", outside the tab order), forcing a Shift+Tab back to Add for every
        // subsequent row: a regression in the common case, in the name of a failure that did not
        // happen (#337).
        if (!HasReachedMax)
        {
            return;
        }

        _focusRowAfterRender = Items.Count - 1;
        _reorderMovedDown = null;
    }

    private async Task RemoveItem(int index)
    {
        if (HasReachedMin || index < 0 || index >= Items.Count)
        {
            return;
        }

        Items.RemoveAt(index);
        await NotifyCollectionChanged();

        // The delete control the user activated has just unmounted — and if this removal reached
        // MinItems, so has every other row's. Move focus deliberately or it falls to <body> (#337).
        // Deferred to OnAfterRenderAsync rather than done here: the @ref captures are only re-bound
        // when the next render batch is applied, so reading them now would hand back the controls
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
        // window. Leaving the other flag set lets it fire on some unrelated later render.
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
    /// Focus target after a row is removed: the delete control that takes the vacated slot, else the
    /// previous row's, else <b>Add</b>, else the collection header (#337, mirrors
    /// <c>FormCraft.ForMudBlazor</c>'s <c>FocusAfterRemovalAsync</c>).
    /// </summary>
    /// <remarks>
    /// The chain matters because a removal can unmount far more than the control that was clicked:
    /// reaching <c>MinItems</c> falsifies the <c>@if</c> guarding <i>every</i> row's delete control at
    /// once, and a field that also forbids adding is then left with no focusable control at all.
    /// </remarks>
    private async Task FocusAfterRemovalAsync(int removedIndex)
    {
        var survivor = DeleteTargetIdAt(removedIndex) ?? DeleteTargetIdAt(removedIndex - 1);
        if (survivor is { } survivorId)
        {
            await FocusByIdAsync(survivorId);
            return;
        }

        if (AddTargetId is { } addId)
        {
            await FocusByIdAsync(addId);
            return;
        }

        await FocusRestore.FocusSafelyAsync(HeaderTarget);
    }

    /// <summary>
    /// Puts focus in the row at <paramref name="index"/> after an add or a reorder (#337, mirrors
    /// <c>FormCraft.ForMudBlazor</c>'s <c>FocusRowAsync</c>).
    /// </summary>
    /// <param name="index">The row the acting item now occupies.</param>
    /// <param name="movedDown">
    /// Which way the item travelled for a reorder, or <see langword="null"/> after an <b>add</b>. A
    /// reorder prefers a still-enabled move control on that row - it keeps the user on the control
    /// they were operating. After an add the row itself is the target instead: the row's fields are
    /// what the user wants next, and its Delete control would put <kbd>Enter</kbd> on "undo the add".
    /// </param>
    private async Task FocusRowAsync(int index, bool? movedDown)
    {
        if (movedDown is { } down)
        {
            var targetId = EnabledMoveTargetIdAt(index, down);
            if (targetId is { } moveId)
            {
                await FocusByIdAsync(moveId);
                return;
            }
        }

        if (_rowHeaderTargets.TryGetValue(index, out var header))
        {
            await FocusRestore.FocusSafelyAsync(header);
        }
    }

    /// <summary>
    /// The id of a still-enabled reorder control on the given row, preferring the direction the item
    /// just travelled and falling back to its counterpart when that one has become disabled at an
    /// end (#337, mirrors <c>FormCraft.ForMudBlazor</c>'s <c>EnabledMoveButtonAt</c>).
    /// </summary>
    /// <remarks>
    /// ⚠️ The preference is the point, not a nicety: landing on the control for the direction the
    /// user was already going means a repeat <kbd>Enter</kbd> keeps moving the item. Always
    /// preferring <b>up</b> would put that repeat keypress on "undo the move I just made" whenever
    /// the item travelled down into a mid-list slot.
    /// </remarks>
    private string? EnabledMoveTargetIdAt(int index, bool movedDown)
    {
        if (!Configuration.CanReorder || index < 0 || index >= Items.Count)
        {
            return null;
        }

        // Mirrors the Disabled bindings in the markup: up is dead at the top, down at the bottom.
        var upEnabled = index > 0;
        var downEnabled = index < Items.Count - 1;

        var travelledEnabled = movedDown ? downEnabled : upEnabled;
        if (travelledEnabled)
        {
            return movedDown ? MoveDownTargetIdAt(index) : MoveUpTargetIdAt(index);
        }

        var counterpartEnabled = movedDown ? upEnabled : downEnabled;
        return counterpartEnabled ? (movedDown ? MoveUpTargetIdAt(index) : MoveDownTargetIdAt(index)) : null;
    }

    private async Task MoveItemUp(int index)
    {
        if (index <= 0 || index >= Items.Count)
        {
            return;
        }

        (Items[index], Items[index - 1]) = (Items[index - 1], Items[index]);
        await NotifyCollectionChanged();

        // Follow the item to its new row. At index 0 the Move-up control the user pressed becomes
        // Disabled under their finger, and browsers drop focus from a newly-disabled element - the
        // same 2.4.3 failure as an unmount (#337).
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
                    // Conversion failed - hand the value over as-is and let validation report it.
                }
            }

            property.SetValue(item, convertedValue);

            // Notify the parent EditContext with a nested field identifier (Blazor convention: the
            // model stays the root model, the field name encodes the collection path, e.g.
            // "Lines[0].Product") so IsModified tracking and validation messages work natively.
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
                builder.AddAttribute(1, "class", "formcraft-collection-item-field");
                builder.AddContent(2, RenderItemField(item, field, capturedIndex));
                builder.OpenComponent<FieldHelpText>(3);
                builder.AddAttribute(4, "Text", field.HelpText);
                // Only the FIRST row's help text carries the id. Every row shows the hint - it is
                // the same text, since it comes from one field configuration - but a document may
                // hold the id once: `formcraft-help-{FieldName}` is not row-scoped, so giving it to
                // every row emitted N elements sharing an id, which is invalid HTML and points each
                // input at whichever one the browser resolved first.
                //
                // Not fixed by scoping the id per row: the input's own `aria-describedby` comes
                // from FluentUIFieldComponentBase.AriaDescribedBy, which derives it from the field
                // name alone and cannot see the row. Leaving exactly one holder keeps that link
                // resolving, to an element carrying precisely the text the row would have shown.
                builder.AddAttribute(5, "Id", capturedIndex == 0 ? FieldHelpText.IdFor(capturedFieldName) : null);
                builder.CloseComponent();
                // Surface validation messages attached to the nested field identifier
                // (e.g. Lines[0].Product) next to the item field input.
                builder.OpenComponent<FieldValidationMessage>(6);
                builder.AddAttribute(7, "FieldName", $"{Configuration.FieldName}[{capturedIndex}].{capturedFieldName}");
                builder.CloseComponent();
                builder.CloseElement();
            }
        };
    }

    /// <summary>
    /// Renders one item field through <see cref="IFieldRendererService"/> - the same selector, and
    /// therefore the same per-type component, an ordinary field renders through (#203).
    /// </summary>
    /// <remarks>
    /// No nested-model machinery is needed: the service is generic over the model and an item field
    /// is already configured as <c>IFieldConfiguration&lt;TItem, object&gt;</c>, so the ITEM is
    /// passed as the model and the selected component binds against <typeparamref name="TItem"/>
    /// directly. What the item does not carry is the parent's identity, which is why the value
    /// callback goes through <see cref="UpdateItemFieldValue"/> - it writes the property on the item
    /// and then notifies the parent <see cref="EditContext"/> under the nested identifier (#91).
    /// </remarks>
    private RenderFragment RenderItemField(TItem item, IFieldConfiguration<TItem, object> field, int itemIndex)
        => FieldRendererService.RenderField(
            item,
            field,
            EventCallback.Factory.Create<object?>(
                this, value => UpdateItemFieldValue(itemIndex, field.FieldName, value)),
            EventCallback.Factory.Create(this, NotifyCollectionChanged));
}
