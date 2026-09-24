using Microsoft.Extensions.Logging;
using FormCraft.ForFluentUI.UnitTests.TestSupport;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// FluentUI parity for
/// <c>FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemUnwritableBindingTests</c> (#450): a
/// collection item field bound through a nested path whose intermediate is null still drops the edit
/// silently (unchanged, #437/#448) but must now warn once per field via <see cref="FormDiagnosticLog"/>
/// — the write-side counterpart of #433's read-side <c>UnreadableBindingDiagnosticCategory</c>
/// diagnostic.
/// </summary>
public class CollectionItemUnwritableBindingTests : FluentUITestBase
{
    [Fact]
    public async Task Editing_An_Unwritable_Nested_Item_Field_Should_Log_One_Warning()
    {
        // Arrange - the item field is bound through `x => x.Details!.Name`, whose intermediate
        // (Details) is null, so the compiled setter throws when it tries to write.
        var logs = new CapturingLoggerProvider();
        Services.AddLogging(builder => builder.AddProvider(logs));

        var model = new ParentModel { Lines = { new Line() } };
        var component = Render<FormCraftComponent<ParentModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, FormBuilder<ParentModel>.Create()
                .AddCollectionField(x => x.Lines, collection => collection
                    .WithItemForm(item => item
                        .AddField(x => x.Details!.Name, field => field.WithLabel("Name"))))
                .Build()));

        // Act - invoked directly on the field's context, mirroring
        // NestedFieldValueReloadTests.Editing_A_Nested_Item_Field_Should_Write_The_Nested_Member_Not_A_Decoy,
        // since Fluent UI's rendered web components are not DOM-input-friendly under bUnit.
        var field = component.FindComponent<FluentUITextFieldComponent<Line>>();
        await component.InvokeAsync(() => field.Instance.Context.OnValueChanged.InvokeAsync("bad"));

        // Assert - AC1: one warning naming the field and the exception type/message.
        logs.Warnings.Count(w => w.Contains("could not be written back to the model")).ShouldBe(1);
        logs.Warnings.ShouldContain(w => w.Contains("Name") && w.Contains("NullReferenceException"));
    }

    [Fact]
    public async Task A_Second_Failing_Edit_On_The_Same_Field_Should_Not_Log_Again()
    {
        // Arrange
        var logs = new CapturingLoggerProvider();
        Services.AddLogging(builder => builder.AddProvider(logs));

        var model = new ParentModel { Lines = { new Line() } };
        var component = Render<FormCraftComponent<ParentModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, FormBuilder<ParentModel>.Create()
                .AddCollectionField(x => x.Lines, collection => collection
                    .WithItemForm(item => item
                        .AddField(x => x.Details!.Name, field => field.WithLabel("Name"))))
                .Build()));

        // Act - two failing edits on the same field.
        var field = component.FindComponent<FluentUITextFieldComponent<Line>>();
        await component.InvokeAsync(() => field.Instance.Context.OnValueChanged.InvokeAsync("bad"));
        await component.InvokeAsync(() => field.Instance.Context.OnValueChanged.InvokeAsync("still bad"));

        // Assert - AC2: once-per-field, not once-per-edit.
        logs.Warnings.Count(w => w.Contains("could not be written back to the model")).ShouldBe(1);
    }

    [Fact]
    public async Task A_Failing_Edit_On_A_Different_Item_Field_Should_Log_Its_Own_Warning()
    {
        // Arrange - two independently-nullable nested item fields in the same item form.
        var logs = new CapturingLoggerProvider();
        Services.AddLogging(builder => builder.AddProvider(logs));

        var model = new ParentModel { Lines = { new Line() } };
        var component = Render<FormCraftComponent<ParentModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, FormBuilder<ParentModel>.Create()
                .AddCollectionField(x => x.Lines, collection => collection
                    .WithItemForm(item => item
                        .AddField(x => x.Details!.Name, field => field.WithLabel("Name"))
                        .AddField(x => x.Extra!.Note, field => field.WithLabel("Note"))))
                .Build()));

        // Act - fail both fields.
        var fields = component.FindComponents<FluentUITextFieldComponent<Line>>();
        await component.InvokeAsync(() => fields[0].Instance.Context.OnValueChanged.InvokeAsync("bad"));
        await component.InvokeAsync(() => fields[1].Instance.Context.OnValueChanged.InvokeAsync("also bad"));

        // Assert - AC3: the latch is keyed per field, not per collection instance.
        logs.Warnings.Count(w => w.Contains("could not be written back to the model")).ShouldBe(2);
    }

    [Fact]
    public async Task The_Edit_Should_Still_Be_Dropped_Not_Written_And_Not_Notified()
    {
        // Arrange
        EditContext? editContext = null;
        var model = new ParentModel { Lines = { new Line() } };
        var component = Render<FormCraftComponent<ParentModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, FormBuilder<ParentModel>.Create()
                .AddCollectionField(x => x.Lines, collection => collection
                    .WithItemForm(item => item
                        .AddField(x => x.Details!.Name, field => field.WithLabel("Name"))))
                .Build())
            .Add(p => p.OnEditContextCreated, (EditContext ctx) => editContext = ctx));

        // Act
        var field = component.FindComponent<FluentUITextFieldComponent<Line>>();
        await component.InvokeAsync(() => field.Instance.Context.OnValueChanged.InvokeAsync("bad"));

        // Assert - AC4: write semantics are unchanged by adding the diagnostic.
        model.Lines[0].Details.ShouldBeNull();
        editContext!.IsModified(new FieldIdentifier(model, "Lines[0].Details.Name")).ShouldBeFalse();
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
