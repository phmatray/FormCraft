namespace FormCraft.UnitTests.Forms.Lov;

/// <summary>
/// Behavioural tests for <see cref="LovFieldBuilderExtensions.AsMultiSelectLov{TModel, TValue, TItem}"/>
/// (#459) — the multi-select LOV entry point, exercised end to end rather than only through the
/// single-select <c>AsLov</c>.
/// <para>
/// #467: <c>AsMultiSelectLov&lt;TModel, TValue, TItem&gt;</c> used to instantiate
/// <c>LovBuilder&lt;TModel, IEnumerable&lt;TValue&gt;, TItem&gt;</c>, binding <c>LovBuilder</c>'s own
/// <c>TValue</c> slot to the model field's type instead of the per-item key's type — so
/// <c>WithKey</c> demanded <c>Func&lt;TItem, IEnumerable&lt;TValue&gt;&gt;</c> instead of
/// <c>Func&lt;TItem, TValue&gt;</c>, and even the extension's own XML <c>&lt;example&gt;</c>
/// (<c>.WithKey(e =&gt; e.Id)</c> with an <c>int Id</c>) did not compile as written. Fixed by
/// keeping <c>LovBuilder</c>'s <c>TValue</c> the scalar per-item key type in both single- and
/// multi-select mode; <see cref="WithKey_Should_Bind_The_Scalar_Per_Item_Key_Not_A_Collection"/>
/// is the regression test for exactly that shape.
/// </para>
/// </summary>
public class AsMultiSelectLovTests
{
    [Fact]
    public void AsMultiSelectLov_Should_Build_An_LovConfiguration_With_Multiple_Selection_Mode()
    {
        var config = FormBuilder<TaskModel>
            .Create()
            .AddField(x => x.AssignedEmployeeIds, field => field
                .AsMultiSelectLov<TaskModel, int, Employee>(lov => lov
                    .WithDataSource(() => Employees)
                    .WithDisplay(e => e.Name)))
            .Build();

        GetLovConfiguration(config).SelectionMode.ShouldBe(LovSelectionMode.Multiple);
    }

    [Fact]
    public void AllowMultipleSelection_With_A_Max_Should_Set_The_Modal_Title_And_Keep_SelectionMode_At_Multiple()
    {
        // AsMultiSelectLov already calls the parameterless AllowMultipleSelection() before the
        // configure callback runs, so re-calling it with no argument here would be a no-op that
        // proves nothing beyond the test above. Passing maxSelections exercises the one branch
        // (the ModalOptions.Title side effect) neither test covered.
        var config = FormBuilder<TaskModel>
            .Create()
            .AddField(x => x.AssignedEmployeeIds, field => field
                .AsMultiSelectLov<TaskModel, int, Employee>(lov => lov
                    .WithDataSource(() => Employees)
                    .WithDisplay(e => e.Name)
                    .AllowMultipleSelection(5)))
            .Build();

        var lovConfig = GetLovConfiguration(config);
        lovConfig.SelectionMode.ShouldBe(LovSelectionMode.Multiple);
        lovConfig.ModalOptions.Title.ShouldBe("Select Items (max 5)");
    }

    [Fact]
    public void WithKey_Should_Bind_The_Scalar_Per_Item_Key_Not_A_Collection()
    {
        // Regression test for #467 — this is the extension's own documented <example>, which did
        // not compile before the fix: e => e.Id returns int, and the old generics demanded
        // Func<Employee, IEnumerable<int>>.
        var config = FormBuilder<TaskModel>
            .Create()
            .AddField(x => x.AssignedEmployeeIds, field => field
                .AsMultiSelectLov<TaskModel, int, Employee>(lov => lov
                    .WithDataSource(() => Employees)
                    .WithKey(e => e.Id)
                    .WithDisplay(e => e.Name)))
            .Build();

        var lovConfig = GetLovConfiguration(config);
        lovConfig.ValueSelector(Employees[0]).ShouldBe(1);
    }

    // The stored configuration is keyed by the scalar per-item key type (int), not
    // IEnumerable<int> — #467 fixed AsMultiSelectLov to keep LovBuilder's own TValue slot scalar.
    private static ILovConfiguration<Employee, int> GetLovConfiguration(
        IFormConfiguration<TaskModel> config)
    {
        var field = config.Fields.Single(f => f.FieldName == nameof(TaskModel.AssignedEmployeeIds));
        return (ILovConfiguration<Employee, int>)field.AdditionalAttributes["LovConfiguration"];
    }

    private static readonly Employee[] Employees =
    [
        new() { Id = 1, Name = "Alice" }
    ];

    private class TaskModel
    {
        public IEnumerable<int> AssignedEmployeeIds { get; set; } = [];
    }

    private class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
