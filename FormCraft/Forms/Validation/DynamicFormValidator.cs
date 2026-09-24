using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft;

/// <summary>
/// A validation component that integrates Dynamic Form validation with Blazor's EditContext.
/// This component handles both form-level and field-level validation using the configured validators.
/// Add this component inside an EditForm to enable dynamic validation.
/// </summary>
/// <remarks>
/// UI-framework-agnostic — it references only <c>Microsoft.AspNetCore.Components</c>. It shipped
/// inside <c>FormCraft.ForMudBlazor</c> until #279, which is why the second adapter had to write its
/// own copy of the non-collection half rather than reuse this one. Every adapter's form container
/// now renders this component.
/// </remarks>
/// <typeparam name="TModel">The form's model type.</typeparam>
public class DynamicFormValidator<TModel> : ComponentBase, IDisposable where TModel : new()
{
    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// Gets or sets the form configuration containing field definitions and validation rules.
    /// </summary>
    [Parameter]
    public IFormConfiguration<TModel> Configuration { get; set; } = null!;

    /// <summary>
    /// Whether collection fields are validated. Defaults to <c>true</c>; set it to <c>false</c> in
    /// an adapter whose container does not render collection/item-form fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>Not a preference — a correctness requirement for such an adapter.</b> A collection
    /// error is attached to the collection's own field identifier, so a container that renders no
    /// control for it also renders no <c>ValidationMessage</c> for it. The form then reports
    /// invalid with nothing on screen to explain why and no input the user could correct, and since
    /// submit is gated on the result, the submit button silently stops working.
    /// </para>
    /// <para>
    /// This is why <c>FluentUIDynamicFormValidator</c> omitted the collection half while it existed
    /// (#260), and the flag is how that decision survives sharing one implementation (#279). Turn it
    /// back on for an adapter once that adapter renders collection fields — not before.
    /// </para>
    /// </remarks>
    [Parameter]
    public bool ValidateCollections { get; set; } = true;

    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;

    // Last-writer stamps for field-change writes (#443). A field-change handler takes its stamp when
    // it STARTS (before reading the value), so the stamp orders the reads, not the writes. A full
    // pass RESERVES its own stamp the same way, up front (#445 - a plain read of the counter is not
    // enough, see the reservation comment in ValidateModelAsync), and at its deferred flush leaves
    // alone every identifier a handler stamped later while also recording its own reserved stamp for
    // every identifier it just evaluated, so a handler parked since before the pass started cannot
    // resurrect a stale result once it resumes, and one still running when the pass flushes still
    // wins once IT resumes. A counter rather than a timestamp: one circuit is single-threaded, so
    // ordering is all that matters.
    // ponytail: entries are never pruned — overlapping passes each need their own view. The map is
    // bounded by every identifier ever written (collection row indices included), not by the form's
    // current shape; prune stamps older than the oldest in-flight pass if that ever matters. A kept
    // cell message can also briefly outlive a row removed mid-pass; the next pass clears it.
    private long _writeVersion;
    private readonly Dictionary<FieldIdentifier, long> _writtenAt = [];

    protected override void OnInitialized()
    {
        var editContext = CascadedEditContext ?? throw new InvalidOperationException(
            $"{nameof(DynamicFormValidator<TModel>)} requires a cascading parameter of type {nameof(EditContext)}. " +
            $"For example, you can use {nameof(DynamicFormValidator<TModel>)} inside an {nameof(EditForm)}.");

        _editContext = editContext;
        _messageStore = new ValidationMessageStore(_editContext);
        _editContext.OnValidationRequested += HandleValidationRequested;
        _editContext.OnFieldChanged += HandleFieldChanged;
    }

    [CascadingParameter] private EditContext CascadedEditContext { get; set; } = default!;

    private async void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        // EditContext.Validate() is synchronous and returns before this handler's
        // first await completes, so it cannot reliably gate submission when async
        // validators are configured. FormCraftComponent awaits ValidateModelAsync()
        // directly on submit; this handler only keeps EditContext.Validate() callers
        // working for synchronously-completing validators.
        try
        {
            await ValidateModelAsync();
        }
        catch
        {
            // Exceptions escaping an async void handler would crash the circuit.
        }
    }

    /// <summary>
    /// Runs all configured validators for visible fields and collection fields,
    /// updates the validation message store, and returns whether the model is valid.
    /// Unlike <see cref="EditContext.Validate"/>, this method awaits asynchronous
    /// validators before reporting the result.
    /// </summary>
    public async Task<bool> ValidateModelAsync()
    {
        var model = (TModel)_editContext!.Model;

        // Build the new message set locally first, without touching _messageStore, so a throw
        // anywhere in this pass (a faulting accessor, a throwing validator) leaves the store
        // exactly as the previous successful pass left it (#440). _messageStore.Clear() moves to
        // immediately before this list is flushed, at the bottom of the method, instead of running
        // unconditionally up front.
        var pendingMessages = new List<(FieldIdentifier Identifier, string Message)>();
        // Every ordinary-field and item-cell identifier this pass actually evaluated, valid or not
        // (#445) - the flush below stamps all of them so a field-changed handler parked since before
        // this pass started cannot overwrite a "now valid" result with its own stale one. The
        // collection's own flat-set identifier is deliberately never added here (see the flush
        // block's exemption further down).
        var evaluatedIdentifiers = new HashSet<FieldIdentifier>();
        // Reserves this pass's own stamp up front (#445, review finding) rather than merely reading
        // the counter: a handler that starts DURING this pass - after this line, before the flush -
        // must take a stamp greater than passStartedAt and WIN once it resumes, exactly like #443
        // already guarantees. Taking flushStamp separately, later, at the flush instead broke that:
        // a flush stamp taken after the pass's own awaits is always greater than such a handler's
        // stamp too, so the flush would refuse a genuinely newer write it has no business refusing.
        // Reusing one pre-incremented value for both the "newer than" threshold and the value
        // written at flush closes that gap, and - being pre-incremented - it is also unique, so it
        // can never equal a handler's own stamp the way a plain read of _writeVersion could.
        var passStartedAt = ++_writeVersion;

        foreach (var field in Configuration.Fields)
        {
            // Hidden fields must not block submission with invisible errors
            if (!IsFieldVisible(field, model))
            {
                continue;
            }

            var fieldIdentifier = _editContext.Field(field.FieldName);
            evaluatedIdentifiers.Add(fieldIdentifier);

            // TryGetValue rather than GetOrCompile(field)(model) directly (#397): a nested binding
            // with a null intermediate would otherwise throw out of the whole validation pass instead
            // of just failing this field's own validators against null, the same treatment a
            // genuinely-null leaf value already gets.
            FieldValueGetterCache<TModel>.TryGetValue(field, model, out var value);

            foreach (var validator in field.Validators)
            {
                // A failed read (TryGetValue above) is treated as null, which validators already
                // handle as a legitimate value — the null-forgiving operator matches the interface's
                // non-nullable `object value` parameter, not a claim that value can never be null.
                var result = await validator.ValidateAsync(model, value!, ServiceProvider);
                if (!result.IsValid)
                {
                    pendingMessages.Add((fieldIdentifier, result.ErrorMessage!));
                }
            }
        }

        // Validate collection fields. FormConfiguration<TModel> always implements the interface, so
        // ValidateCollections is what actually decides this for an adapter that renders no
        // collection UI - see the parameter's remarks.
        if (ValidateCollections && Configuration is ICollectionFormConfiguration<TModel> collectionConfig)
        {
            foreach (var collectionField in collectionConfig.CollectionFields)
            {
                // Hidden collections must not block submission with invisible errors - mirrors the
                // ordinary-field guard above. ICollectionFieldConfigurationBase exposes only
                // the static IsVisible flag (no VisibilityCondition), and both adapters' render
                // loops gate rendering on exactly this flag, so there is nothing else to mirror
                // (#342).
                if (!collectionField.IsVisible)
                {
                    continue;
                }

                // ONE traversal produces both message shapes. Asking for them separately meant
                // running every item field's validator twice per pass, because the flat-message call
                // already performs the per-item walk internally (#329).
                var result = await ValidateCollectionAsync(model, collectionField);

                foreach (var error in result.Messages)
                {
                    pendingMessages.Add((_editContext.Field(collectionField.FieldName), error));
                }

                // Additionally attach per-item errors to nested field identifiers
                // (e.g. Items[0].ProductName) so ValidationMessage/ValidationSummary
                // and FieldValidationMessage can display them natively.
                foreach (var itemError in result.ItemErrors)
                {
                    var itemIdentifier = CreateCollectionItemFieldIdentifier(
                        collectionField.FieldName, itemError.ItemIndex, itemError.FieldName);
                    evaluatedIdentifiers.Add(itemIdentifier);
                    pendingMessages.Add((itemIdentifier, itemError.Message));
                }
            }
        }

        // The pass completed with no throw: only now is it safe to clear the previous pass's
        // messages and flush this pass's in their place — except for identifiers a field-change
        // handler rewrote while this pass was awaiting (#443). Those carry a result newer than, or
        // as new as, this pass's own snapshot, so they are carried over rather than clobbered.
        var newer = _writtenAt
            .Where(entry => entry.Value > passStartedAt)
            .Select(entry => entry.Key)
            .ToHashSet();
        // Materialised before Clear(): the store's indexer returns its live inner list.
        var carried = newer
            .SelectMany(identifier => _messageStore![identifier].Select(message => (identifier, message)))
            .ToList();
        var fresh = pendingMessages.Where(pending => !newer.Contains(pending.Identifier));

        _messageStore!.Clear();
        foreach (var (identifier, message) in carried.Concat(fresh))
        {
            _messageStore.Add(identifier, message);
        }

        // Stamp every identifier this pass just wrote fresh - including ones it evaluated and found
        // valid, which never enter pendingMessages at all (#445). A field-changed handler that took
        // its own stamp before passStartedAt and is still awaiting must lose the TryWrite race for
        // any of these identifiers once it resumes; one started AFTER passStartedAt must still win
        // (#443) once IT resumes, which is exactly why this reuses passStartedAt itself rather than
        // taking a fresh, later stamp here - see the reservation comment above. Carried identifiers
        // are skipped - they already carry a handler's own newer stamp from #443, and this flush did
        // not write them.
        foreach (var identifier in evaluatedIdentifiers)
        {
            if (!newer.Contains(identifier))
            {
                _writtenAt[identifier] = passStartedAt;
            }
        }

        // The collection's flat set came from this pass (it carries the count rules, which exist
        // nowhere else), so re-apply each carried cell's newer lines to it - otherwise the flat set
        // would contradict the cell it summarises.
        foreach (var identifier in newer)
        {
            if (ValidateCollections && TryResolveCell(identifier.FieldName, out var collectionField, out var itemIndex, out var itemFieldName))
            {
                var cellErrors = _messageStore[identifier]
                    .Select(message => new CollectionItemError(itemIndex, itemFieldName, message))
                    .ToList();
                RefreshCollectionFlatMessages(collectionField, itemIndex, itemFieldName, cellErrors);
            }
        }

        _editContext.NotifyValidationStateChanged();
        return !_editContext.GetValidationMessages().Any();
    }

    private static bool IsFieldVisible(IFieldConfiguration<TModel, object> field, TModel model)
    {
        if (field.VisibilityCondition != null)
        {
            return field.VisibilityCondition(model);
        }

        return field.IsVisible;
    }

    /// <summary>
    /// Runs one validation pass over a collection field and returns both message shapes.
    /// </summary>
    /// <remarks>
    /// Replaces the pair of calls this method used to make. <see cref="CollectionValidationResult" /> is
    /// non-generic precisely so <c>GetInvoker</c> can return it through <see cref="ICollectionValidator" />
    /// without knowing the item type — a typed interface call now, not a reflective invoke (#344).
    /// </remarks>
    private Task<CollectionValidationResult> ValidateCollectionAsync(TModel model, ICollectionFieldConfigurationBase collectionField)
        => GetInvoker(collectionField).ValidateAllAsync(model!, ServiceProvider);

    /// <summary>
    /// Validates one item field of one row — the cell a field-change notification named.
    /// </summary>
    private Task<List<CollectionItemError>> ValidateCollectionCellAsync(
        TModel model,
        ICollectionFieldConfigurationBase collectionField,
        int itemIndex,
        string itemFieldName)
        => GetInvoker(collectionField).ValidateItemFieldAsync(model!, itemIndex, itemFieldName, ServiceProvider);

    /// <summary>
    /// One collection field's typed validator, resolved once.
    /// </summary>
    /// <remarks>
    /// Keyed by the <b>configuration instance</b>, not by item type. The generic type depends only on
    /// the item type, but the validator instance is constructed <i>from the configuration</i> — so two
    /// collections of the same item type with different configurations (different item forms,
    /// different min/max) must not share one. A <see cref="ConditionalWeakTable{TKey, TValue}" /> also
    /// means an entry lives no longer than the configuration it describes.
    /// </remarks>
    private static readonly ConditionalWeakTable<ICollectionFieldConfigurationBase, ICollectionValidator> ValidatorCache = new();

    private static ICollectionValidator GetInvoker(ICollectionFieldConfigurationBase collectionField)
        => ValidatorCache.GetValue(collectionField, static field => CreateValidator(field));

    /// <summary>
    /// Constructs one collection field's typed validator. This is the only reflective step left
    /// (#344): the concrete <c>CollectionFieldValidator&lt;TModel, TItem&gt;</c> depends on
    /// <c>TItem</c>, unknown here at compile time, so <c>MakeGenericType</c>/<c>Activator.CreateInstance</c>
    /// still run once per configuration — but every call on the result afterwards goes through
    /// <see cref="ICollectionValidator"/> directly, with no further <c>MethodInfo.Invoke</c>.
    /// </summary>
    private static ICollectionValidator CreateValidator(ICollectionFieldConfigurationBase collectionField)
    {
        var validatorType = typeof(CollectionFieldValidator<,>)
            .MakeGenericType(typeof(TModel), collectionField.ItemType);

        return (ICollectionValidator)(Activator.CreateInstance(validatorType, collectionField)
            ?? throw new InvalidOperationException(
                $"Could not construct a collection validator for item type '{collectionField.ItemType}'."));
    }

    private FieldIdentifier CreateCollectionItemFieldIdentifier(string collectionFieldName, int itemIndex, string itemFieldName)
        => new(_editContext!.Model, $"{collectionFieldName}[{itemIndex}].{itemFieldName}");

    // Matches nested collection item field names such as "Items[0].ProductName" - including a
    // dotted, nested collection FieldName such as "Billing.Items[0].ProductName" (#428: FieldName is
    // now qualified by its full member-access chain, not just its last segment, so the collection
    // group here must accept the same dots or a nested collection's per-keystroke item validation
    // would silently stop matching).
    private static readonly System.Text.RegularExpressions.Regex CollectionItemFieldPattern =
        new(@"^(?<collection>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\[(?<index>\d+)\]\.(?<field>[A-Za-z_]\w*)$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private async void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        // Taken before anything is read, so it orders this handler's read against a full pass's.
        var stamp = ++_writeVersion;

        try
        {
            // Nested collection item identifiers (Items[0].ProductName) are validated
            // against the owning collection field's item form configuration.
            var nestedMatch = CollectionItemFieldPattern.Match(e.FieldIdentifier.FieldName);
            if (ValidateCollections && nestedMatch.Success)
            {
                await ValidateCollectionItemFieldAsync(e.FieldIdentifier, stamp);
                return;
            }

            // Find the field configuration for the changed field
            var fieldConfig = Configuration.Fields.FirstOrDefault(f => f.FieldName == e.FieldIdentifier.FieldName);
            if (fieldConfig == null)
            {
                return;
            }

            var model = (TModel)_editContext!.Model;

            // TryGetValue, not GetOrCompile(fieldConfig)(model) directly: the same unguarded-read
            // hazard #397 fixed in ValidateModelAsync above applies here too — a nested binding with
            // a null intermediate must not crash a single field's re-validation on change.
            FieldValueGetterCache<TModel>.TryGetValue(fieldConfig, model, out var value);

            // Run every validator BEFORE clearing (#443): a throwing validator is swallowed by the
            // catch below, and clearing first would leave the field blank — silently "valid".
            var messages = new List<string>();
            foreach (var validator in fieldConfig.Validators)
            {
                // A failed read (TryGetValue above) is treated as null, which validators already
                // handle as a legitimate value — the null-forgiving operator matches the interface's
                // non-nullable `object value` parameter, not a claim that value can never be null.
                var result = await validator.ValidateAsync(model, value!, ServiceProvider);
                if (!result.IsValid)
                {
                    messages.Add(result.ErrorMessage!);
                }
            }

            if (TryWrite(e.FieldIdentifier, stamp, messages))
            {
                _editContext.NotifyValidationStateChanged();
            }
        }
        catch
        {
            // Exceptions escaping an async void handler would crash the circuit.
        }
    }

    private async Task ValidateCollectionItemFieldAsync(FieldIdentifier fieldIdentifier, long stamp)
    {
        if (!TryResolveCell(fieldIdentifier.FieldName, out var collectionField, out var itemIndex, out var itemFieldName))
        {
            return;
        }

        var model = (TModel)_editContext!.Model;

        // Validate just this cell. This used to validate the whole collection and filter the result
        // down to the matching item/field, which runs items × fields validators per keystroke (#329).
        var itemErrors = await ValidateCollectionCellAsync(model, collectionField, itemIndex, itemFieldName);

        // Write only now that validation succeeded (#443), so a throwing validator leaves the
        // cell's previous message in place instead of a blank, "valid" cell.
        if (!TryWrite(fieldIdentifier, stamp, itemErrors.Select(error => error.Message)))
        {
            return;
        }

        // Keep the collection's own flat message set (what a ValidationSummary shows) in agreement
        // with the nested identifier just updated above - otherwise a corrected cell's line
        // survives in the flat set until the next full pass (#342).
        RefreshCollectionFlatMessages(collectionField, itemIndex, itemFieldName, itemErrors);

        _editContext.NotifyValidationStateChanged();
    }

    /// <summary>
    /// Resolves a nested item identifier (<c>Items[0].ProductName</c>) to its collection field,
    /// row index and item field name.
    /// </summary>
    private bool TryResolveCell(
        string fieldName,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ICollectionFieldConfigurationBase? collectionField,
        out int itemIndex,
        out string itemFieldName)
    {
        collectionField = null;
        itemIndex = 0;
        itemFieldName = string.Empty;

        var match = CollectionItemFieldPattern.Match(fieldName);
        if (!match.Success || Configuration is not ICollectionFormConfiguration<TModel> collectionConfig)
        {
            return false;
        }

        // TryParse, not Parse: the regex guarantees digits but not that they fit in an int, and an
        // OverflowException here would be swallowed by HandleFieldChanged's catch, silently skipping
        // the cell's re-validation.
        if (!int.TryParse(match.Groups["index"].Value, out itemIndex))
        {
            return false;
        }

        itemFieldName = match.Groups["field"].Value;
        var collectionFieldName = match.Groups["collection"].Value;
        collectionField = collectionConfig.CollectionFields.FirstOrDefault(f => f.FieldName == collectionFieldName);
        return collectionField != null;
    }

    /// <summary>
    /// Replaces one item field's line(s) in a collection's flat message set with its current
    /// errors, after a single-cell edit.
    /// </summary>
    /// <remarks>
    /// Reads the flat set's current lines straight out of <see cref="_messageStore" /> (the only
    /// source of truth already in hand) and removes the edited cell's own line(s) by their
    /// formatted prefix, rather than recomputing the whole set - recomputing would mean
    /// revalidating every other row, which is exactly the per-keystroke cost #329 removed from this
    /// path. See <see cref="CollectionFieldValidator{TModel,TItem}.FormatItemMessagePrefix" /> for
    /// the one collision this prefix match cannot distinguish.
    /// </remarks>
    private void RefreshCollectionFlatMessages(
        ICollectionFieldConfigurationBase collectionField,
        int itemIndex,
        string itemFieldName,
        List<CollectionItemError> cellErrors)
    {
        var invoker = GetInvoker(collectionField);
        var collectionIdentifier = _editContext!.Field(collectionField.FieldName);

        var prefix = invoker.FormatItemMessagePrefix(itemIndex, itemFieldName);
        var remaining = _messageStore![collectionIdentifier]
            .Where(line => !line.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (var error in cellErrors)
        {
            remaining.Add(invoker.FormatItemMessage(error));
        }

        _messageStore.Clear(collectionIdentifier);
        foreach (var message in remaining)
        {
            _messageStore.Add(collectionIdentifier, message);
        }
    }

    /// <summary>
    /// Replaces <paramref name="identifier" />'s messages and records <paramref name="stamp" />, so
    /// a full <see cref="ValidateModelAsync" /> pass already in flight does not overwrite them (#443).
    /// Refused when a handler that started later has already written this identifier - two
    /// handlers for one field may finish out of order.
    /// </summary>
    /// <remarks>
    /// Only the cell and ordinary-field identifiers are stamped, never a collection's flat set: that
    /// set also carries the count rules (min/max items), which no cell write recomputes, so a pass's
    /// flush must keep ownership of it and re-apply carried cells instead.
    /// </remarks>
    /// <returns><c>true</c> if the write happened.</returns>
    private bool TryWrite(FieldIdentifier identifier, long stamp, IEnumerable<string> messages)
    {
        if (_writtenAt.TryGetValue(identifier, out var existing) && existing > stamp)
        {
            return false;
        }

        _messageStore!.Clear(identifier);
        _messageStore.Add(identifier, messages);
        _writtenAt[identifier] = stamp;
        return true;
    }

    public void Dispose()
    {
        if (_editContext != null)
        {
            _editContext.OnValidationRequested -= HandleValidationRequested;
            _editContext.OnFieldChanged -= HandleFieldChanged;
        }
    }
}
