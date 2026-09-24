using System.Runtime.CompilerServices;

namespace FormCraft;

/// <summary>
/// Tracks per-row identity for a rendered collection field — the mechanism behind the <c>@key</c>
/// each collection component binds its item loop to (#334, #401, #422).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> A keyed <c>@for</c> loop needs a key that survives the row it names moving
/// index — otherwise a removal or reorder reattaches a surviving row's component instance (and its
/// local state: a revealed password, a masked-value diagnostic latch) to the wrong data. Keying on the
/// item itself (#308) crashes the moment two rows compare equal (a <c>record</c>, a <c>struct</c>, or
/// any <c>Equals</c>-overriding class) into Blazor's duplicate-key render exception. This type keys on
/// a per-item weak token instead — minted once per item INSTANCE and never compared by value — so two
/// equal-by-value rows still get two different keys.
/// </para>
/// <para>
/// One instance is held per rendered collection component (see the brainstorm on #422): the token
/// table's lifetime must be scoped to one component instance, so two rendered collection fields never
/// share tokens. Lived twice, byte-for-byte identical, in <c>FormCraft.ForMudBlazor</c>'s
/// <c>CollectionFieldComponent</c> (#334, original fix) and <c>FormCraft.ForFluentUI</c>'s
/// <c>FluentUICollectionFieldComponent</c> (#401, ported verbatim) — both landing PRs named the
/// duplication and deferred moving it, until #422 made the move. Public and in core, so a third
/// adapter reuses it rather than porting a third copy.
/// </para>
/// </remarks>
public sealed class CollectionRowIdentity
{
    /// <summary>
    /// Weak per-item tokens, minted once per item instance and reused for its lifetime — the
    /// mechanism behind <see cref="KeyFor{TItem}"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="ConditionalWeakTable{TKey,TValue}"/> compares KEYS by reference, never by
    /// <c>Equals</c> — the property #308's reverted <c>@key="Items[index]"</c> was missing. Weak, and
    /// never written to except through <see cref="KeyFor{TItem}"/>: it stays correct even when the
    /// caller's own list is mutated from outside, and an item that leaves the list is collected
    /// normally once nothing else references it.
    /// </remarks>
    private readonly ConditionalWeakTable<object, object> _rowTokens = new();

    /// <summary>
    /// This row's identity — a per-item weak token for a reference-type item that appears only once in
    /// <paramref name="items"/>; a boxed <paramref name="index"/> otherwise. Bind the result as the
    /// row's <c>@key</c> so a surviving row's own component instance follows it across an add, remove,
    /// or reorder this type is never told about directly.
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
    /// <paramref name="items"/> right now — legal for a reference type, and it would otherwise mint
    /// one token for two rows, which is the duplicate-key crash a keyed loop exists to avoid (#308).
    /// The duplicate check reads <paramref name="items"/> live, not a snapshot, so it stays correct
    /// under mutation this type was never told about.
    /// </para>
    /// </remarks>
    public object KeyFor<TItem>(IReadOnlyList<TItem> items, int index)
    {
        if (typeof(TItem).IsValueType)
        {
            return index;
        }

        var item = items[index];
        if (item is null || HasDuplicateReference(items, item, index))
        {
            return index;
        }

        return _rowTokens.GetValue(item, _ => new object());
    }

    // ponytail: O(n) scan per row (O(n^2) per render) — collections here are short, hand-built lists
    // a user assembles by clicking "Add item", not bulk data. Switch to a single reference-identity
    // counting pass over items if that stops being true.
    private static bool HasDuplicateReference<TItem>(IReadOnlyList<TItem> items, TItem item, int index)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (i != index && ReferenceEquals(items[i], item))
            {
                return true;
            }
        }

        return false;
    }
}
