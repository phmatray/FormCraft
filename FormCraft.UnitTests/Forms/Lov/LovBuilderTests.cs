namespace FormCraft.UnitTests.Forms.Lov;

/// <summary>
/// Behavioural tests for the remaining <see cref="LovBuilder{TModel, TValue, TItem}"/> methods not
/// already covered by <see cref="LovBuilderDisplayTests"/> (#459): field mapping registration, the
/// DI data-service hook, and the modal/search option setters.
/// </summary>
public class LovBuilderTests
{
    [Fact]
    public void MapField_Should_Register_A_Mapping_That_LovMappingProcessor_Later_Applies()
    {
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .AsLov<OrderModel, int, CustomerDto>(lov => lov
                    .WithDataSource(() => Customers)
                    .WithKey(c => c.Id)
                    .WithDisplay(c => c.Name)
                    .MapField(c => c.Email, m => m.CustomerEmail)))
            .Build();
        var lovConfig = GetLovConfiguration(config);
        var processor = new LovMappingProcessor(A.Fake<IServiceProvider>());
        var model = new OrderModel();

        processor.ApplyMappings(model, Customers[0], lovConfig.FieldMappings);

        model.CustomerEmail.ShouldBe("acme@example.com");
    }

    [Fact]
    public async Task MapFieldAsync_Should_Register_An_Async_Mapping_Whose_Action_Runs_On_Apply()
    {
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .AsLov<OrderModel, int, CustomerDto>(lov => lov
                    .WithDataSource(() => Customers)
                    .WithKey(c => c.Id)
                    .WithDisplay(c => c.Name)
                    .MapFieldAsync(
                        c => c.Email,
                        m => m.CustomerEmail,
                        async (_, m, _, _) =>
                        {
                            await Task.Yield();
                            m.AsyncActionRan = true;
                        })))
            .Build();
        var lovConfig = GetLovConfiguration(config);
        var processor = new LovMappingProcessor(A.Fake<IServiceProvider>());
        var model = new OrderModel();

        await processor.ApplyMappingsAsync(
            model, Customers[0], lovConfig.FieldMappings, Xunit.TestContext.Current.CancellationToken);

        model.CustomerEmail.ShouldBe("acme@example.com");
        model.AsyncActionRan.ShouldBeTrue();
    }

    [Fact]
    public void WithDataService_Should_Set_DataProviderServiceType_To_The_Given_Service()
    {
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .AsLov<OrderModel, int, CustomerDto>(lov => lov
                    .WithDataService<ICustomerLovService>()
                    .WithKey(c => c.Id)
                    .WithDisplay(c => c.Name)))
            .Build();

        GetLovConfiguration(config).DataProviderServiceType.ShouldBe(typeof(ICustomerLovService));
    }

    [Fact]
    public void Modal_And_Search_Configuration_Methods_Should_Each_Set_Their_Corresponding_Value()
    {
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .AsLov<OrderModel, int, CustomerDto>(lov => lov
                    .WithDataSource(() => Customers)
                    .WithKey(c => c.Id)
                    .WithDisplay(c => c.Name)
                    .WithModalTitle("Pick a customer")
                    .WithModalSize(LovModalSize.Small)
                    .WithGridHeight("250px")
                    .WithSearchDebounce(750)
                    .WithSearchPlaceholder("Type to search")
                    .DisableSearch()))
            .Build();

        var lovConfig = GetLovConfiguration(config);
        lovConfig.ModalOptions.Title.ShouldBe("Pick a customer");
        lovConfig.ModalOptions.Size.ShouldBe(LovModalSize.Small);
        lovConfig.ModalOptions.GridHeight.ShouldBe("250px");
        lovConfig.SearchOptions.DebounceMs.ShouldBe(750);
        lovConfig.SearchOptions.Placeholder.ShouldBe("Type to search");
        lovConfig.SearchOptions.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void DependsOn_Should_Register_A_Cascading_Dependency()
    {
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .AsLov<OrderModel, int, CustomerDto>(lov => lov
                    .WithDataSource(() => Customers)
                    .WithKey(c => c.Id)
                    .WithDisplay(c => c.Name)
                    .DependsOn(x => x.RegionId, "regionId", clearOnChange: false)))
            .Build();

        var dependency = GetLovConfiguration(config).Dependencies.Single();
        dependency.DependentPropertyName.ShouldBe(nameof(OrderModel.RegionId));
        dependency.ContextKey.ShouldBe("regionId");
        dependency.ClearOnChange.ShouldBeFalse();
    }

    [Fact]
    public void WithJsonConfig_Should_Set_The_JsonConfigId()
    {
        var config = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.CustomerId, field => field
                .AsLov<OrderModel, int, CustomerDto>(lov => lov
                    .WithJsonConfig("customers")
                    .WithKey(c => c.Id)
                    .WithDisplay(c => c.Name)))
            .Build();

        GetLovConfiguration(config).JsonConfigId.ShouldBe("customers");
    }

    private static ILovConfiguration<CustomerDto, int> GetLovConfiguration(IFormConfiguration<OrderModel> config)
    {
        var field = config.Fields.Single(f => f.FieldName == nameof(OrderModel.CustomerId));
        return (ILovConfiguration<CustomerDto, int>)field.AdditionalAttributes["LovConfiguration"];
    }

    private static readonly CustomerDto[] Customers =
    [
        new() { Id = 1, Name = "Acme", Email = "acme@example.com" }
    ];

    private interface ICustomerLovService : ILovDataProvider<CustomerDto>;

    private class OrderModel
    {
        public int CustomerId { get; set; }
        public int RegionId { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public bool AsyncActionRan { get; set; }
    }

    private class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
