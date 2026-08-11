using FormCraft.ForMudBlazor.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Regression tests for renderer dispatch: fields configured with specialized
/// renderers (LOV, file upload) must not fall through to the generic type-based
/// renderers, and the single-file upload must render without crashing.
/// </summary>
public class SpecializedRendererDispatchTests : MudBlazorTestBase
{
    public SpecializedRendererDispatchTests()
    {
        ((IServiceCollection)Services).AddFormCraftMudBlazor();
    }

    [Fact]
    public void LovField_Should_Render_Lov_Component_Not_Numeric_Spinner()
    {
        // Arrange - an int? field configured as a List-of-Values selector
        var model = new OrderModel();
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .WithLabel("Customer")
                .AsLov<OrderModel, int?, Customer>(lov => lov
                    .WithKey(c => (int?)c.Id)
                    .WithDisplay(c => c.Name)
                    .WithDataSource(() => new List<Customer> { new() { Id = 1, Name = "ACME" } })
                    .AddColumn(c => c.Name, "Name")))
            .Build();

        // Act
        var component = Render<FormCraftComponent<OrderModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - must NOT render as a plain numeric spinner
        component.FindComponents<MudNumericField<int?>>().ShouldBeEmpty();
        component.FindComponents<MudNumericField<int>>().ShouldBeEmpty();
    }

    [Fact]
    public void SingleFileUploadField_Should_Render_Without_Crashing()
    {
        // Arrange - MudFileUpload.CustomContent is RenderFragment<MudFileUpload<T>>;
        // the legacy path passed a plain RenderFragment and crashed the renderer
        var model = new DocumentModel();
        var config = FormBuilder<DocumentModel>
            .Create()
            .AddField(x => x.Resume, field => field
                .WithLabel("Resume"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<DocumentModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - the upload field renders with a single-value binding (no 'multiple' attribute)
        component.FindComponents<MudFileUpload<Microsoft.AspNetCore.Components.Forms.IBrowserFile>>()
            .ShouldNotBeEmpty();
    }

    [Fact]
    public void MultiFileUploadField_Should_Render_Dropzone()
    {
        var model = new DocumentModel();
        var config = FormBuilder<DocumentModel>
            .Create()
            .AddField(x => x.Certificates, field => field
                .WithLabel("Certificates"))
            .Build();

        var component = Render<FormCraftComponent<DocumentModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        component.FindComponents<MudFileUpload<IReadOnlyList<Microsoft.AspNetCore.Components.Forms.IBrowserFile>>>()
            .ShouldNotBeEmpty();
    }

    private class OrderModel
    {
        public int? CustomerId { get; set; }
    }

    private class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class DocumentModel
    {
        public Microsoft.AspNetCore.Components.Forms.IBrowserFile? Resume { get; set; }
        public IReadOnlyList<Microsoft.AspNetCore.Components.Forms.IBrowserFile>? Certificates { get; set; }
    }
}
