namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Pins that <c>.ShowValidationSummary()</c> renders the stock <see cref="ValidationSummary"/> and
/// that the default renders none (#457). The setting used to be stored and never read, with a
/// default of <c>true</c>; the default is now <c>false</c> so existing forms do not change.
/// </summary>
public class FormContainerValidationSummaryTests : MudBlazorTestBase
{
    [Fact]
    public async Task Invalid_Submit_Should_List_Errors_In_A_ValidationSummary_When_Opted_In()
    {
        // Arrange
        var config = FormBuilder<SummaryModel>.Create()
            .ShowValidationSummary()
            .AddField(x => x.Name, f => f.WithLabel("Name").Required("Name is required"))
            .Build();

        var component = Render<FormCraftComponent<SummaryModel>>(p => p
            .Add(c => c.Model, new SummaryModel())
            .Add(c => c.Configuration, config));

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        component.FindComponents<ValidationSummary>().ShouldNotBeEmpty();
        component.WaitForAssertion(() =>
            component.Find("ul.validation-errors").TextContent.ShouldContain("Name is required"));
    }

    [Fact]
    public async Task Invalid_Submit_Should_Render_No_ValidationSummary_By_Default()
    {
        // Arrange
        var config = FormBuilder<SummaryModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name").Required("Name is required"))
            .Build();

        var component = Render<FormCraftComponent<SummaryModel>>(p => p
            .Add(c => c.Model, new SummaryModel())
            .Add(c => c.Configuration, config));

        // Act
        await component.Find("form").SubmitAsync();

        // Assert
        component.FindComponents<ValidationSummary>().ShouldBeEmpty();
    }

    public class SummaryModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
