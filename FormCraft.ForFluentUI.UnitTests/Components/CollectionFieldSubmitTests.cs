namespace FormCraft.ForFluentUI.UnitTests.Components;

/// <summary>An order with a collection the Fluent adapter cannot render yet.</summary>
public class OrderModel
{
    /// <summary>A plain field the adapter does render.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>The collection. Fluent has no collection/item-form renderer (blocked on #203).</summary>
    public List<OrderItem> Items { get; set; } = [];
}

/// <summary>An item inside <see cref="OrderModel.Items"/>.</summary>
public class OrderItem
{
    /// <summary>The item's name.</summary>
    public string ProductName { get; set; } = string.Empty;
}

/// <summary>
/// Collection/item-form fields are not implemented for the Fluent adapter yet, so its container
/// renders nothing for them. The validator must not report on them either.
/// </summary>
/// <remarks>
/// <para>
/// This is the constraint that made <c>FluentUIDynamicFormValidator</c> omit the collection half
/// deliberately rather than by oversight. #279 replaced that component with core's shared
/// <see cref="DynamicFormValidator{TModel}"/>, which does validate collections — so without an
/// explicit opt-out the move would have handed Fluent a form that cannot be submitted at all: the
/// error lands on a field identifier nothing renders, no <c>FieldValidationMessage</c> exists to
/// display it, and <c>HandleSubmit</c> gates <c>OnValidSubmit</c> on the result. The button would
/// simply stop working, with nothing on screen to explain why and no input the user could correct.
/// </para>
/// <para>
/// ⛔ Do not "unify" this by dropping <c>ValidateCollections="false"</c> from the Fluent container.
/// It becomes correct only once the adapter actually renders collection fields; until then it turns
/// a form that submits into one that cannot.
/// </para>
/// </remarks>
public class CollectionFieldSubmitTests : FluentUITestBase
{
    [Fact]
    public async Task Submitting_Should_Succeed_When_An_Unrendered_Collection_Field_Is_Invalid()
    {
        // Arrange - MinItems(1) against an empty collection: invalid by the collection rule, and
        // there is no rendered control anywhere on the form that could satisfy it.
        var model = new OrderModel { Reference = "A-1" };
        var config = FormBuilder<OrderModel>.Create()
            .AddField(x => x.Reference, f => f.WithLabel("Reference"))
            .AddCollectionField(x => x.Items, c => c
                .WithMinItems(1)
                .WithItemForm(item => item.AddField(x => x.ProductName)))
            .Build();

        var submitted = false;
        var component = Render<FormCraftComponent<OrderModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config)
            .Add(c => c.OnValidSubmit, _ => submitted = true));

        // Act
        await component.Find("form").SubmitAsync();

        // Assert - the collection is invisible to this adapter, so it must not block submission.
        submitted.ShouldBeTrue();
    }

    [Fact]
    public async Task Submitting_Should_Still_Be_Blocked_By_An_Ordinary_Invalid_Field()
    {
        // Arrange - the other half of the contract. Suppressing collection validation must not
        // suppress validation as such, or this "fix" would be a hole rather than a scope limit.
        var model = new OrderModel();
        var config = FormBuilder<OrderModel>.Create()
            .AddField(x => x.Reference, f => f.WithLabel("Reference").Required("Reference is required"))
            .AddCollectionField(x => x.Items, c => c.WithMinItems(1))
            .Build();

        var submitted = false;
        var component = Render<FormCraftComponent<OrderModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config)
            .Add(c => c.OnValidSubmit, _ => submitted = true));

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        submitted.ShouldBeFalse();
        component.Instance.GetEditContext()!.GetValidationMessages()
            .ShouldContain("Reference is required");
    }
}
