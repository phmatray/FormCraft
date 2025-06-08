namespace FormCraft.UnitTests.Builders;

public class FieldGroupBuilderTests
{
    private class TestModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Department { get; set; }
    }

    [Fact]
    public void AddFieldGroup_WithColumns_SetsColumnsCorrectly()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>
            .Create()
            .AddFieldGroup(group => group
                .WithGroupName("Test Group")
                .WithColumns(3)
                .AddField(x => x.FirstName)
                .AddField(x => x.LastName)
                .AddField(x => x.Email))
            .Build();

        // Assert
        var groupedConfig = config as IGroupedFormConfiguration<TestModel>;
        groupedConfig.ShouldNotBeNull();
        groupedConfig.UseFieldGroups.ShouldBeTrue();
        groupedConfig.FieldGroups.Count.ShouldBe(1);

        var fieldGroup = groupedConfig.FieldGroups[0];
        fieldGroup.Name.ShouldBe("Test Group");
        fieldGroup.Columns.ShouldBe(3);
        fieldGroup.FieldNames.Count.ShouldBe(3);
        fieldGroup.FieldNames.ShouldContain("FirstName");
        fieldGroup.FieldNames.ShouldContain("LastName");
        fieldGroup.FieldNames.ShouldContain("Email");
    }

    [Fact]
    public void AddFieldGroup_MultipleGroups_PreservesOrder()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>
            .Create()
            .AddFieldGroup(group => group
                .WithGroupName("Group 1")
                .WithColumns(2)
                .AddField(x => x.FirstName)
                .AddField(x => x.LastName))
            .AddFieldGroup(group => group
                .WithGroupName("Group 2")
                .WithColumns(3)
                .AddField(x => x.Email)
                .AddField(x => x.Phone)
                .AddField(x => x.Department))
            .Build();

        // Assert
        var groupedConfig = config as IGroupedFormConfiguration<TestModel>;
        groupedConfig.ShouldNotBeNull();
        groupedConfig.FieldGroups.Count.ShouldBe(2);

        groupedConfig.FieldGroups[0].Name.ShouldBe("Group 1");
        groupedConfig.FieldGroups[0].Columns.ShouldBe(2);
        groupedConfig.FieldGroups[0].Order.ShouldBe(0);

        groupedConfig.FieldGroups[1].Name.ShouldBe("Group 2");
        groupedConfig.FieldGroups[1].Columns.ShouldBe(3);
        groupedConfig.FieldGroups[1].Order.ShouldBe(1);
    }

    [Fact]
    public void WithColumns_SetsCorrectValue()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>
            .Create()
            .AddFieldGroup(group => group.WithColumns(4))
            .Build();

        // Assert
        var groupedConfig = config as IGroupedFormConfiguration<TestModel>;
        groupedConfig.ShouldNotBeNull();
        groupedConfig.FieldGroups[0].Columns.ShouldBe(4);
    }

    [Fact]
    public void ShowInCard_SetsCardProperties()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>
            .Create()
            .AddFieldGroup(group => group
                .WithGroupName("Card Group")
                .ShowInCard(3))
            .Build();

        // Assert
        var groupedConfig = config as IGroupedFormConfiguration<TestModel>;
        groupedConfig.ShouldNotBeNull();

        var fieldGroup = groupedConfig.FieldGroups[0];
        fieldGroup.ShowCard.ShouldBeTrue();
        fieldGroup.CardElevation.ShouldBe(3);
    }
}