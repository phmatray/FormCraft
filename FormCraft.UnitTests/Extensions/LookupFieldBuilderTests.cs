namespace FormCraft.UnitTests.Extensions;

public class LookupFieldBuilderTests
{
    [Fact]
    public void AsLookup_Should_Set_DataProvider_Attribute()
    {
        // Arrange
        Func<LookupQuery, Task<LookupResult<CityDto>>> dataProvider =
            query => Task.FromResult(new LookupResult<CityDto>());

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.CityId, field => field
                .AsLookup(
                    dataProvider: dataProvider,
                    valueSelector: city => city.Id,
                    displaySelector: city => city.Name))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "CityId");
        field.AdditionalAttributes.ShouldContainKey("LookupDataProvider");
        field.AdditionalAttributes["LookupDataProvider"].ShouldBeSameAs(dataProvider);
    }

    [Fact]
    public void AsLookup_Should_Set_ValueSelector_Attribute()
    {
        // Arrange
        Func<CityDto, int> valueSelector = city => city.Id;

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.CityId, field => field
                .AsLookup(
                    dataProvider: query => Task.FromResult(new LookupResult<CityDto>()),
                    valueSelector: valueSelector,
                    displaySelector: city => city.Name))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "CityId");
        field.AdditionalAttributes.ShouldContainKey("LookupValueSelector");
        field.AdditionalAttributes["LookupValueSelector"].ShouldBeSameAs(valueSelector);
    }

    [Fact]
    public void AsLookup_Should_Set_DisplaySelector_Attribute()
    {
        // Arrange
        Func<CityDto, string> displaySelector = city => city.Name;

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.CityId, field => field
                .AsLookup(
                    dataProvider: query => Task.FromResult(new LookupResult<CityDto>()),
                    valueSelector: city => city.Id,
                    displaySelector: displaySelector))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "CityId");
        field.AdditionalAttributes.ShouldContainKey("LookupDisplaySelector");
        field.AdditionalAttributes["LookupDisplaySelector"].ShouldBeSameAs(displaySelector);
    }

    [Fact]
    public void AsLookup_Should_Set_Columns_When_Configured()
    {
        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.CityId, field => field
                .AsLookup(
                    dataProvider: query => Task.FromResult(new LookupResult<CityDto>()),
                    valueSelector: city => city.Id,
                    displaySelector: city => city.Name,
                    configureColumns: cols =>
                    {
                        cols.Add(new LookupColumn<CityDto> { Title = "Name", ValueSelector = c => c.Name });
                        cols.Add(new LookupColumn<CityDto> { Title = "Country", ValueSelector = c => c.Country });
                    }))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "CityId");
        field.AdditionalAttributes.ShouldContainKey("LookupColumns");

        var columns = field.AdditionalAttributes["LookupColumns"] as List<LookupColumn<CityDto>>;
        columns.ShouldNotBeNull();
        columns.Count.ShouldBe(2);
        columns[0].Title.ShouldBe("Name");
        columns[1].Title.ShouldBe("Country");
    }

    [Fact]
    public void AsLookup_Should_Not_Set_Columns_When_Not_Configured()
    {
        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.CityId, field => field
                .AsLookup(
                    dataProvider: query => Task.FromResult(new LookupResult<CityDto>()),
                    valueSelector: city => city.Id,
                    displaySelector: city => city.Name))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "CityId");
        field.AdditionalAttributes.ShouldNotContainKey("LookupColumns");
    }

    [Fact]
    public void AsLookup_Should_Set_OnItemSelected_When_Provided()
    {
        // Arrange
        Action<TestModel, CityDto> onItemSelected = (model, city) => model.CityName = city.Name;

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.CityId, field => field
                .AsLookup(
                    dataProvider: query => Task.FromResult(new LookupResult<CityDto>()),
                    valueSelector: city => city.Id,
                    displaySelector: city => city.Name,
                    onItemSelected: onItemSelected))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "CityId");
        field.AdditionalAttributes.ShouldContainKey("LookupOnItemSelected");
        field.AdditionalAttributes["LookupOnItemSelected"].ShouldBeSameAs(onItemSelected);
    }

    [Fact]
    public void AsLookup_Should_Not_Set_OnItemSelected_When_Null()
    {
        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.CityId, field => field
                .AsLookup(
                    dataProvider: query => Task.FromResult(new LookupResult<CityDto>()),
                    valueSelector: city => city.Id,
                    displaySelector: city => city.Name))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "CityId");
        field.AdditionalAttributes.ShouldNotContainKey("LookupOnItemSelected");
    }

    [Fact]
    public void AsLookup_Should_Be_Chainable()
    {
        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.CityId, field => field
                .WithLabel("City")
                .AsLookup(
                    dataProvider: query => Task.FromResult(new LookupResult<CityDto>()),
                    valueSelector: city => city.Id,
                    displaySelector: city => city.Name)
                .WithHelpText("Click search to find a city"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "CityId");
        field.Label.ShouldBe("City");
        field.HelpText.ShouldBe("Click search to find a city");
        field.AdditionalAttributes.ShouldContainKey("LookupDataProvider");
    }

    [Fact]
    public void AsLookup_Column_ValueSelector_Should_Extract_Values()
    {
        // Arrange
        var city = new CityDto { Id = 1, Name = "Paris", Country = "France" };
        var column = new LookupColumn<CityDto>
        {
            Title = "Name",
            ValueSelector = c => c.Name
        };

        // Act
        var result = column.ValueSelector(city);

        // Assert
        result.ShouldBe("Paris");
    }

    [Fact]
    public void LookupQuery_Should_Have_Default_Values()
    {
        // Act
        var query = new LookupQuery();

        // Assert
        query.SearchText.ShouldBe(string.Empty);
        query.Page.ShouldBe(0);
        query.PageSize.ShouldBe(10);
        query.SortField.ShouldBeNull();
        query.SortDescending.ShouldBeFalse();
    }

    [Fact]
    public void LookupResult_Should_Have_Default_Values()
    {
        // Act
        var result = new LookupResult<CityDto>();

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public void LookupColumn_Should_Have_Default_Values()
    {
        // Act
        var column = new LookupColumn<CityDto>();

        // Assert
        column.Title.ShouldBe(string.Empty);
        column.Sortable.ShouldBeTrue();
        column.Filterable.ShouldBeTrue();
        column.ValueSelector.ShouldNotBeNull();
    }

    public class TestModel
    {
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
    }

    public class CityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
