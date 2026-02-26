namespace FormCraft.UnitTests.Extensions;

public class AutocompleteFieldBuilderTests
{
    [Fact]
    public void AsAutocomplete_WithSearchFunc_Should_Set_SearchFunc_Attribute()
    {
        // Arrange
        Func<string, CancellationToken, Task<IEnumerable<SelectOption<string>>>> searchFunc =
            (text, ct) => Task.FromResult<IEnumerable<SelectOption<string>>>(
                new List<SelectOption<string>>());

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.City, field => field
                .AsAutocomplete(searchFunc))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "City");
        field.AdditionalAttributes.ShouldContainKey("AutocompleteSearchFunc");
        field.AdditionalAttributes["AutocompleteSearchFunc"].ShouldBeSameAs(searchFunc);
    }

    [Fact]
    public void AsAutocomplete_WithSearchFunc_Should_Set_Default_DebounceMs()
    {
        // Arrange
        Func<string, CancellationToken, Task<IEnumerable<SelectOption<string>>>> searchFunc =
            (text, ct) => Task.FromResult<IEnumerable<SelectOption<string>>>(
                new List<SelectOption<string>>());

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.City, field => field
                .AsAutocomplete(searchFunc))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "City");
        field.AdditionalAttributes.ShouldContainKey("AutocompleteDebounceMs");
        field.AdditionalAttributes["AutocompleteDebounceMs"].ShouldBe(300);
    }

    [Fact]
    public void AsAutocomplete_WithSearchFunc_Should_Set_Custom_DebounceMs()
    {
        // Arrange
        Func<string, CancellationToken, Task<IEnumerable<SelectOption<string>>>> searchFunc =
            (text, ct) => Task.FromResult<IEnumerable<SelectOption<string>>>(
                new List<SelectOption<string>>());

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.City, field => field
                .AsAutocomplete(searchFunc, debounceMs: 500))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "City");
        field.AdditionalAttributes["AutocompleteDebounceMs"].ShouldBe(500);
    }

    [Fact]
    public void AsAutocomplete_WithSearchFunc_Should_Set_Custom_MinCharacters()
    {
        // Arrange
        Func<string, CancellationToken, Task<IEnumerable<SelectOption<string>>>> searchFunc =
            (text, ct) => Task.FromResult<IEnumerable<SelectOption<string>>>(
                new List<SelectOption<string>>());

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.City, field => field
                .AsAutocomplete(searchFunc, minCharacters: 3))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "City");
        field.AdditionalAttributes.ShouldContainKey("AutocompleteMinCharacters");
        field.AdditionalAttributes["AutocompleteMinCharacters"].ShouldBe(3);
    }

    [Fact]
    public void AsAutocomplete_WithSearchFunc_Should_Set_ToStringFunc_When_Provided()
    {
        // Arrange
        Func<string, CancellationToken, Task<IEnumerable<SelectOption<string>>>> searchFunc =
            (text, ct) => Task.FromResult<IEnumerable<SelectOption<string>>>(
                new List<SelectOption<string>>());
        Func<string, string> toStringFunc = v => $"City: {v}";

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.City, field => field
                .AsAutocomplete(searchFunc, toStringFunc: toStringFunc))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "City");
        field.AdditionalAttributes.ShouldContainKey("AutocompleteToStringFunc");
        field.AdditionalAttributes["AutocompleteToStringFunc"].ShouldBeSameAs(toStringFunc);
    }

    [Fact]
    public void AsAutocomplete_WithSearchFunc_Should_Not_Set_ToStringFunc_When_Null()
    {
        // Arrange
        Func<string, CancellationToken, Task<IEnumerable<SelectOption<string>>>> searchFunc =
            (text, ct) => Task.FromResult<IEnumerable<SelectOption<string>>>(
                new List<SelectOption<string>>());

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.City, field => field
                .AsAutocomplete(searchFunc))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "City");
        field.AdditionalAttributes.ShouldNotContainKey("AutocompleteToStringFunc");
    }

    [Fact]
    public void AsAutocomplete_WithOptionProvider_Should_Set_OptionProvider_Attribute()
    {
        // Arrange
        var optionProvider = A.Fake<IOptionProvider<TestModel, string>>();

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.City, field => field
                .AsAutocomplete(optionProvider))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "City");
        field.AdditionalAttributes.ShouldContainKey("AutocompleteOptionProvider");
        field.AdditionalAttributes["AutocompleteOptionProvider"].ShouldBeSameAs(optionProvider);
    }

    [Fact]
    public void AsAutocomplete_WithOptionProvider_Should_Set_Default_Settings()
    {
        // Arrange
        var optionProvider = A.Fake<IOptionProvider<TestModel, string>>();

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.City, field => field
                .AsAutocomplete(optionProvider))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "City");
        field.AdditionalAttributes["AutocompleteDebounceMs"].ShouldBe(300);
        field.AdditionalAttributes["AutocompleteMinCharacters"].ShouldBe(1);
        field.AdditionalAttributes.ShouldNotContainKey("AutocompleteToStringFunc");
    }

    [Fact]
    public void AsAutocomplete_Should_Support_Int_Value_Type()
    {
        // Arrange
        Func<string, CancellationToken, Task<IEnumerable<SelectOption<int>>>> searchFunc =
            (text, ct) => Task.FromResult<IEnumerable<SelectOption<int>>>(
                new List<SelectOption<int>>());

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.CityId, field => field
                .AsAutocomplete(searchFunc))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "CityId");
        field.AdditionalAttributes.ShouldContainKey("AutocompleteSearchFunc");
    }

    [Fact]
    public void AsAutocomplete_Should_Be_Chainable()
    {
        // Arrange
        Func<string, CancellationToken, Task<IEnumerable<SelectOption<string>>>> searchFunc =
            (text, ct) => Task.FromResult<IEnumerable<SelectOption<string>>>(
                new List<SelectOption<string>>());

        // Act
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.City, field => field
                .WithLabel("City")
                .AsAutocomplete(searchFunc)
                .WithHelpText("Start typing to search"))
            .Build();

        // Assert
        var field = config.Fields.First(f => f.FieldName == "City");
        field.Label.ShouldBe("City");
        field.HelpText.ShouldBe("Start typing to search");
        field.AdditionalAttributes.ShouldContainKey("AutocompleteSearchFunc");
    }

    public class TestModel
    {
        public string City { get; set; } = string.Empty;
        public int CityId { get; set; }
    }
}
