namespace FormCraft.UnitTests.Forms.Lov;

/// <summary>
/// Behavioural tests for <see cref="LovColumnBuilder{TItem}"/> (#459), built the same way
/// <see cref="LovBuilderDisplayTests"/> reads a configuration back off the field.
/// </summary>
public class LovColumnBuilderTests
{
    [Fact]
    public void AddColumn_Configure_Should_Set_Width_Sortable_Filterable_Format_And_Template()
    {
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .AsLov<OrderModel, int, CustomerDto>(lov => lov
                    .WithDataSource(() => Customers)
                    .WithKey(c => c.Id)
                    .WithDisplay(c => c.Name)
                    .AddColumn(c => c.Name, "Name", col => col
                        .Width("200px")
                        .NotSortable()
                        .NotFilterable()
                        .Format("N2")
                        .Template(item => item.Name.ToUpper()))))
            .Build();

        var column = GetLovConfiguration(config).Columns.Single();
        column.Width.ShouldBe("200px");
        column.Sortable.ShouldBeFalse();
        column.Filterable.ShouldBeFalse();
        column.Format.ShouldBe("N2");
        column.CellTemplate.ShouldNotBeNull();
        column.CellTemplate(Customers[0]).ShouldBe("ACME");
    }

    [Fact]
    public void AddColumn_Without_Configure_Should_Keep_The_Defaults_Sortable_And_Filterable()
    {
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .AsLov<OrderModel, int, CustomerDto>(lov => lov
                    .WithDataSource(() => Customers)
                    .WithKey(c => c.Id)
                    .WithDisplay(c => c.Name)
                    .AddColumn(c => c.Name, "Name")))
            .Build();

        var column = GetLovConfiguration(config).Columns.Single();
        column.Sortable.ShouldBeTrue();
        column.Filterable.ShouldBeTrue();
        column.Width.ShouldBeNull();
        column.Format.ShouldBeNull();
        column.CellTemplate.ShouldBeNull();
    }

    private static ILovConfiguration<CustomerDto, int> GetLovConfiguration(IFormConfiguration<OrderModel> config)
    {
        var field = config.Fields.Single(f => f.FieldName == nameof(OrderModel.CustomerId));
        return (ILovConfiguration<CustomerDto, int>)field.AdditionalAttributes["LovConfiguration"];
    }

    private static readonly CustomerDto[] Customers =
    [
        new() { Id = 1, Name = "Acme" }
    ];

    private class OrderModel
    {
        public int CustomerId { get; set; }
    }

    private class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
