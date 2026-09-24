using Microsoft.Extensions.Logging;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// A collection item field bound through a nested path whose intermediate is null still drops the
/// edit silently (unchanged, #437/#448) but must now warn once per field via
/// <see cref="FormDiagnosticLog"/> — the write-side, per-item-field counterpart of #433's read-side
/// <c>UnreadableBindingDiagnosticCategory</c> diagnostic (#450).
/// </summary>
public class CollectionItemUnwritableBindingTests : MudBlazorTestBase
{
    [Fact]
    public void Editing_An_Unwritable_Nested_Item_Field_Should_Log_One_Warning()
    {
        // Arrange - the item field is bound through `x => x.Details!.Name`, whose intermediate
        // (Details) is null, so the compiled setter throws when it tries to write.
        var logs = new TestSupport.CapturingLoggerProvider();
        Services.AddLogging(builder => builder.AddProvider(logs));

        var model = new ParentModel { Lines = { new Line() } };
        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithItemForm(item => item
                    .AddField(x => x.Details!.Name, field => field.WithLabel("Name"))))
            .Build();
        var component = this.RenderItemForm(model, config);

        // Act
        component.Find("input").Input("bad");

        // Assert - AC1: one warning naming the field and the exception type/message.
        component.WaitForAssertion(() =>
        {
            logs.Warnings.Count(w => w.Contains("could not be written back to the model")).ShouldBe(1);
            logs.Warnings.ShouldContain(w => w.Contains("Name") && w.Contains("NullReferenceException"));
        });
    }

    [Fact]
    public void A_Second_Failing_Edit_On_The_Same_Field_Should_Not_Log_Again()
    {
        // Arrange
        var logs = new TestSupport.CapturingLoggerProvider();
        Services.AddLogging(builder => builder.AddProvider(logs));

        var model = new ParentModel { Lines = { new Line() } };
        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithItemForm(item => item
                    .AddField(x => x.Details!.Name, field => field.WithLabel("Name"))))
            .Build();
        var component = this.RenderItemForm(model, config);

        // Act - two failing edits on the same field.
        component.Find("input").Input("bad");
        component.Find("input").Input("still bad");

        // Assert - AC2: once-per-field, not once-per-edit.
        component.WaitForAssertion(() =>
            logs.Warnings.Count(w => w.Contains("could not be written back to the model")).ShouldBe(1));
    }

    [Fact]
    public void A_Failing_Edit_On_A_Different_Item_Field_Should_Log_Its_Own_Warning()
    {
        // Arrange - two independently-nullable nested item fields in the same item form.
        var logs = new TestSupport.CapturingLoggerProvider();
        Services.AddLogging(builder => builder.AddProvider(logs));

        var model = new ParentModel { Lines = { new Line() } };
        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithItemForm(item => item
                    .AddField(x => x.Details!.Name, field => field.WithLabel("Name"))
                    .AddField(x => x.Extra!.Note, field => field.WithLabel("Note"))))
            .Build();
        var component = this.RenderItemForm(model, config);

        // Act - fail both fields.
        var inputs = component.FindAll("input");
        inputs[0].Input("bad");
        inputs[1].Input("also bad");

        // Assert - AC3: the latch is keyed per field, not per collection instance.
        component.WaitForAssertion(() =>
            logs.Warnings.Count(w => w.Contains("could not be written back to the model")).ShouldBe(2));
    }

    [Fact]
    public void The_Edit_Should_Still_Be_Dropped_Not_Written_And_Not_Notified()
    {
        // Arrange
        EditContext? editContext = null;
        var model = new ParentModel { Lines = { new Line() } };
        var config = FormBuilder<ParentModel>.Create()
            .AddCollectionField(x => x.Lines, collection => collection
                .WithItemForm(item => item
                    .AddField(x => x.Details!.Name, field => field.WithLabel("Name"))))
            .Build();
        var component = this.RenderItemForm(model, config,
            parameters => parameters.Add(p => p.OnEditContextCreated, ctx => editContext = ctx));

        // Act
        component.Find("input").Input("bad");

        // Assert - AC4: write semantics are unchanged by adding the diagnostic.
        component.WaitForAssertion(() =>
        {
            model.Lines[0].Details.ShouldBeNull();
            editContext!.IsModified(new FieldIdentifier(model, "Lines[0].Details.Name")).ShouldBeFalse();
        });
    }

    public class ParentModel
    {
        public List<Line> Lines { get; set; } = new();
    }

    public class Line
    {
        public LineDetails? Details { get; set; }

        public LineExtra? Extra { get; set; }
    }

    public class LineDetails
    {
        public string Name { get; set; } = string.Empty;
    }

    public class LineExtra
    {
        public string Note { get; set; } = string.Empty;
    }
}
