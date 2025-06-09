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

    [Fact]
    public void AddField_WithConfiguration_Should_Add_Configured_Field_To_Group()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddFieldGroup(group => group
                .WithGroupName("Personal Info")
                .WithColumns(2)
                .AddField(x => x.FirstName, field => field
                    .WithLabel("First Name")
                    .Required("First name is required")
                    .WithPlaceholder("Enter first name"))
                .AddField(x => x.Email, field => field
                    .WithLabel("Email Address")
                    .Required()
                    .WithEmailValidation()))
            .Build();

        // Assert
        var groupedConfig = config as IGroupedFormConfiguration<TestModel>;
        groupedConfig.ShouldNotBeNull();

        var fieldGroup = groupedConfig.FieldGroups[0];
        fieldGroup.FieldNames.Count.ShouldBe(2);
        fieldGroup.FieldNames.ShouldContain("FirstName");
        fieldGroup.FieldNames.ShouldContain("Email");

        // Verify field configurations
        var firstNameField = config.Fields.First(f => f.FieldName == "FirstName");
        firstNameField.Label.ShouldBe("First Name");
        firstNameField.IsRequired.ShouldBeTrue();
        firstNameField.Placeholder.ShouldBe("Enter first name");

        var emailField = config.Fields.First(f => f.FieldName == "Email");
        emailField.Label.ShouldBe("Email Address");
        emailField.IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void Mixed_AddField_Approaches_Should_Work_Together()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>.Create()
            .AddFieldGroup(group => group
                .WithGroupName("Mixed Fields")
                .AddField(x => x.FirstName)  // Old style
                .AddField(x => x.Email, field => field  // New style
                    .WithLabel("Email")
                    .Required()))
            .Build();

        // Assert
        config.Fields.Count.ShouldBe(2);
        
        var groupedConfig = config as IGroupedFormConfiguration<TestModel>;
        var fieldGroup = groupedConfig?.FieldGroups[0];
        fieldGroup?.FieldNames.Count.ShouldBe(2);
    }
}