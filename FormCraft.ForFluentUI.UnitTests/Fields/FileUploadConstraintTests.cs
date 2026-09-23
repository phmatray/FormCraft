using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// Regression and parity tests for #340: every upload component now resolves its constraints
/// through <see cref="UploadConstraintResolver"/> — the same path the MudBlazor adapter uses —
/// rather than each reading its own subset of keys with its own CLR types.
/// </summary>
/// <remarks>
/// The specific defect reported for this adapter: a hand-written <c>.WithAttribute("MaxFileSize",
/// 5_000_000)</c> boxes the literal as <c>int</c>, and the previous fallback
/// (<c>GetAttribute("MaxFileSize", GetAttribute("MaximumFileSize", 10L * 1024 * 1024))</c>) inferred
/// <c>long</c>, so <c>GetAttribute&lt;T&gt;</c>'s exact-type match failed and silently reverted to
/// the 10&#160;MB default.
/// </remarks>
public class FileUploadConstraintTests : FluentUITestBase
{
    [Fact]
    public void A_Raw_Int_MaxFileSize_Attribute_Should_Bind_On_The_Single_File_Component()
    {
        // Arrange
        var config = FormBuilder<UploadModel>.Create()
            .AddField(x => x.Resume, f => f.WithAttribute("MaxFileSize", 5_000_000))
            .Build();

        // Act
        var component = Render<FormCraftComponent<UploadModel>>(p => p
            .Add(c => c.Model, new UploadModel())
            .Add(c => c.Configuration, config));

        // Assert - not the 10 MB default the int/long mismatch used to fall back to
        var input = component.FindComponent<FluentInputFile>().Instance;
        input.MaximumFileSize.ShouldBe(5_000_000L);
    }

    [Fact]
    public void A_Raw_Int_MaxFileSize_Attribute_Should_Bind_On_The_Multiple_File_Component()
    {
        // Arrange
        var config = FormBuilder<UploadModel>.Create()
            .AddField(x => x.Documents, f => f.WithAttribute("MaxFileSize", 5_000_000))
            .Build();

        // Act
        var component = Render<FormCraftComponent<UploadModel>>(p => p
            .Add(c => c.Model, new UploadModel())
            .Add(c => c.Configuration, config));

        // Assert
        var input = component.FindComponent<FluentInputFile>().Instance;
        input.MaximumFileSize.ShouldBe(5_000_000L);
    }

    [Fact]
    public void The_Same_AsFileUpload_Configuration_Should_Resolve_Identically_On_Single_And_Multiple()
    {
        // Arrange - the drift itself is the defect: one configuration, applied through each field's
        // own builder extension, must resolve to the same Accept and MaxFileSize on both components.
        var config = FormBuilder<UploadModel>.Create()
            .AddField(x => x.Resume, f => f.AsFileUpload(acceptedFileTypes: [".pdf"], maxFileSize: 2 * 1024 * 1024))
            .AddField(x => x.Documents, f => f.AsMultipleFileUpload(acceptedFileTypes: [".pdf"], maxFileSize: 2 * 1024 * 1024))
            .Build();

        // Act
        var component = Render<FormCraftComponent<UploadModel>>(p => p
            .Add(c => c.Model, new UploadModel())
            .Add(c => c.Configuration, config));

        // Assert
        var inputs = component.FindComponents<FluentInputFile>();
        inputs.Count.ShouldBe(2);
        inputs[0].Instance.Accept.ShouldBe(inputs[1].Instance.Accept);
        inputs[0].Instance.MaximumFileSize.ShouldBe(inputs[1].Instance.MaximumFileSize);
    }

    /// <summary>Model with single- and multiple-file fields.</summary>
    public class UploadModel
    {
        public IBrowserFile? Resume { get; set; }

        public IReadOnlyList<IBrowserFile> Documents { get; set; } = [];
    }
}
