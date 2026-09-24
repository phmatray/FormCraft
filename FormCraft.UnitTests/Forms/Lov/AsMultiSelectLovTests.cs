namespace FormCraft.UnitTests.Forms.Lov;

/// <summary>
/// Behavioural tests for <see cref="LovFieldBuilderExtensions.AsMultiSelectLov{TModel, TValue, TItem}"/>
/// (#459) — the multi-select LOV entry point, exercised end to end rather than only through the
/// single-select <c>AsLov</c>.
/// <para>
/// These tests deliberately do NOT call <c>.WithKey(...)</c>. Under the current generics,
/// <c>AsMultiSelectLov&lt;TModel, TValue, TItem&gt;</c> instantiates
/// <c>LovBuilder&lt;TModel, IEnumerable&lt;TValue&gt;, TItem&gt;</c>, so <c>WithKey</c> demands
/// <c>Func&lt;TItem, IEnumerable&lt;TValue&gt;&gt;</c> instead of <c>Func&lt;TItem, TValue&gt;</c> —
/// even the extension's own XML <c>&lt;example&gt;</c> (<c>.WithKey(e =&gt; e.Id)</c> with an
/// <c>int Id</c>) does not compile as written (filed separately; not fixed here, per #459's
/// "tests only" scope).
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

    // Note: the stored configuration is keyed by IEnumerable<int>, not int — see the type-level
    // remark above; AsMultiSelectLov binds LovBuilder's own TValue slot to IEnumerable<TValue>.
    private static ILovConfiguration<Employee, IEnumerable<int>> GetLovConfiguration(
        IFormConfiguration<TaskModel> config)
    {
        var field = config.Fields.Single(f => f.FieldName == nameof(TaskModel.AssignedEmployeeIds));
        return (ILovConfiguration<Employee, IEnumerable<int>>)field.AdditionalAttributes["LovConfiguration"];
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
