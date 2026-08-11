namespace FormCraft.UnitTests.Forms.Lov;

/// <summary>
/// Tests for <see cref="LovBuilder{TModel, TValue, TItem}"/>.WithDisplay (#180).
/// <para>
/// WithDisplay used to declare both an <c>Expression&lt;Func&lt;TItem, string&gt;&gt;</c> and a
/// <c>Func&lt;TItem, string&gt;</c> overload. They did the same thing — the Expression one only
/// called <c>.Compile()</c> before assigning to the same <c>Func</c> field — but a lambda converts
/// to both, so the natural call failed with CS0121. These tests call it the natural way, with no
/// cast, which is exactly what would not compile before the redundant overload was removed.
/// </para>
/// </summary>
public class LovBuilderDisplayTests
{
    [Fact]
    public void WithDisplay_Should_Accept_A_Bare_Lambda()
    {
        // Arrange & Act - no cast: this is the form the XML docs and demo Code Example tab show
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .WithLabel("Customer")
                .AsLov<OrderModel, int, CustomerDto>(lov => lov
                    .WithDataSource(() => Customers)
                    .WithKey(c => c.Id)
                    .WithDisplay(c => c.Name)))
            .Build();

        // Assert - both rows, so a selector wired to the wrong item cannot pass
        var lovConfig = GetLovConfiguration(config);
        lovConfig.DisplaySelector(Customers[0]).ShouldBe("Acme");
        lovConfig.DisplaySelector(Customers[1]).ShouldBe("Globex");
    }

    [Fact]
    public void WithDisplay_Should_Accept_A_Complex_Formatting_Lambda()
    {
        // Arrange & Act - the "complex display formatting" case the XML docs advertise
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .WithLabel("Customer")
                .AsLov<OrderModel, int, CustomerDto>(lov => lov
                    .WithDataSource(() => Customers)
                    .WithKey(c => c.Id)
                    .WithDisplay(c => $"{c.Code} - {c.Name}")))
            .Build();

        // Assert
        var lovConfig = GetLovConfiguration(config);
        lovConfig.DisplaySelector(Customers[0]).ShouldBe("ACM - Acme");
        lovConfig.DisplaySelector(Customers[1]).ShouldBe("GLB - Globex");
    }

    [Fact]
    public void WithDisplay_Should_Reject_Null()
    {
        // Arrange - DisplaySelector is non-nullable and ships a safe default; letting null
        // through would replace it and only surface as a NullReferenceException at render time.
        var builder = FormBuilder<OrderModel>.Create();
        Func<CustomerDto, string> displayFunc = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder
            .AddField(x => x.CustomerId, field => field
                .AsLov<OrderModel, int, CustomerDto>(lov => lov
                    .WithDataSource(() => Customers)
                    .WithKey(c => c.Id)
                    .WithDisplay(displayFunc))));
    }

    [Fact]
    public void WithDisplay_Should_Accept_A_Method_Group()
    {
        // Arrange & Act - a method group has no natural conversion to an expression tree, so this
        // one compiled even before the fix; it guards against the survivor being the wrong overload
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .WithLabel("Customer")
                .AsLov<OrderModel, int, CustomerDto>(lov => lov
                    .WithDataSource(() => Customers)
                    .WithKey(c => c.Id)
                    .WithDisplay(FormatCustomer)))
            .Build();

        // Assert
        var lovConfig = GetLovConfiguration(config);
        lovConfig.DisplaySelector(Customers[0]).ShouldBe("Acme (ACM)");
    }

    private static string FormatCustomer(CustomerDto customer) => $"{customer.Name} ({customer.Code})";

    private static readonly CustomerDto[] Customers =
    [
        new() { Id = 1, Code = "ACM", Name = "Acme" },
        new() { Id = 2, Code = "GLB", Name = "Globex" }
    ];

    /// <summary>
    /// Casts to the ILovConfiguration interface the renderer actually consumes
    /// (MudBlazorLovFieldComponent reads the attribute as ILovConfiguration), not the concrete
    /// type — so swapping the stored implementation would not turn these into InvalidCastException.
    /// </summary>
    private static ILovConfiguration<CustomerDto, int> GetLovConfiguration(
        IFormConfiguration<OrderModel> config)
    {
        var field = config.Fields.Single(f => f.FieldName == nameof(OrderModel.CustomerId));
        return (ILovConfiguration<CustomerDto, int>)field.AdditionalAttributes["LovConfiguration"];
    }

    private class OrderModel
    {
        public int CustomerId { get; set; }
    }

    private class CustomerDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
