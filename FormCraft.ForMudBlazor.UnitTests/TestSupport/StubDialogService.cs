namespace FormCraft.ForMudBlazor.UnitTests.TestSupport;

/// <summary>
/// A minimal <see cref="IDialogService"/> double that resolves a component's own
/// <c>ShowAsync&lt;TDialog&gt;</c> call to a canned result, so a lookup/LOV field's own selection
/// code path (<c>OpenLookupDialog</c>/<c>OpenLovDialog</c>, and everything each writes off the
/// result) can be driven without a <see cref="MudDialogProvider"/> in the render tree.
/// </summary>
/// <remarks>
/// Folded out of <c>FieldConfigurationParityTests</c> (its original home) after code review found
/// <c>MudBlazorLovFieldMultiSelectTests</c> had grown a verbatim, private copy of it (#461).
/// </remarks>
internal static class StubDialogService
{
    public static IDialogService Returning<TDialog>(object selectedData)
        where TDialog : IComponent
    {
        var reference = A.Fake<IDialogReference>();
        A.CallTo(() => reference.Result).Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(selectedData)));

        var service = A.Fake<IDialogService>();
        A.CallTo(() => service.ShowAsync<TDialog>(A<string>._, A<DialogParameters>._, A<DialogOptions>._))
            .Returns(Task.FromResult(reference));

        return service;
    }
}
