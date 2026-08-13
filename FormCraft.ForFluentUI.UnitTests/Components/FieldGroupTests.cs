namespace FormCraft.ForFluentUI.UnitTests.Components;

/// <summary>
/// <c>.AddFieldGroup(...)</c> produces the card and column layout it configures, rather than the
/// flat field list #260 shipped (#278).
/// </summary>
/// <remarks>
/// This is the gap that changed what a <i>working</i> configuration did on switching adapters: the
/// fields all rendered, so nothing failed, but a form designed around grouping silently lost its
/// structure. Absence of an error is what made it worth fixing early.
/// </remarks>
public class FieldGroupTests : FluentUITestBase
{
    [Fact]
    public void Two_Card_Groups_Should_Render_Two_Cards_Each_Showing_Its_Name()
    {
        // Arrange
        var config = FormBuilder<GroupModel>.Create()
            .AddFieldGroup(group => group
                .WithGroupName("Contact")
                .ShowInCard()
                .AddField(x => x.Email, f => f.WithLabel("Email"))
                .AddField(x => x.Phone, f => f.WithLabel("Phone")))
            .AddFieldGroup(group => group
                .WithGroupName("Address")
                .ShowInCard()
                .AddField(x => x.Street, f => f.WithLabel("Street")))
            .Build();

        // Act
        var component = Render<FormCraftComponent<GroupModel>>(p => p
            .Add(c => c.Model, new GroupModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.FindAll("[data-testid=formcraft-field-group-card]").Count.ShouldBe(2);
        var names = component.FindAll("[data-testid=formcraft-field-group-name]")
            .Select(e => e.TextContent.Trim())
            .ToList();
        names.ShouldBe(["Contact", "Address"]);
    }

    [Fact]
    public void A_Group_Without_ShowCard_Should_Render_Without_A_Card()
    {
        // Arrange
        var config = FormBuilder<GroupModel>.Create()
            .AddFieldGroup(group => group
                .WithGroupName("Contact")
                .AddField(x => x.Email, f => f.WithLabel("Email")))
            .Build();

        // Act
        var component = Render<FormCraftComponent<GroupModel>>(p => p
            .Add(c => c.Model, new GroupModel())
            .Add(c => c.Configuration, config));

        // Assert - the name still shows, the card does not
        component.FindAll("[data-testid=formcraft-field-group-card]").ShouldBeEmpty();
        component.Find("[data-testid=formcraft-field-group-name]").TextContent.ShouldContain("Contact");
    }

    [Fact]
    public void A_Multi_Column_Group_Should_Split_Its_Fields_Across_Grid_Items()
    {
        // Arrange - 2 columns over 12 means each field spans 6
        var config = FormBuilder<GroupModel>.Create()
            .AddFieldGroup(group => group
                .WithGroupName("Contact")
                .WithColumns(2)
                .AddField(x => x.Email, f => f.WithLabel("Email"))
                .AddField(x => x.Phone, f => f.WithLabel("Phone")))
            .Build();

        // Act
        var component = Render<FormCraftComponent<GroupModel>>(p => p
            .Add(c => c.Model, new GroupModel())
            .Add(c => c.Configuration, config));

        // Assert
        var items = component.FindComponents<FluentGridItem>();
        items.Count.ShouldBe(2);
        items.ShouldAllBe(i => i.Instance.Md == 6);
        items.ShouldAllBe(i => i.Instance.Xs == 12);
    }

    [Fact]
    public void Ungrouped_Fields_Should_Still_Render_Alongside_Groups()
    {
        // Arrange - the regression that matters: adding one group must not hide everything else
        var config = FormBuilder<GroupModel>.Create()
            .AddFieldGroup(group => group
                .WithGroupName("Contact")
                .ShowInCard()
                .AddField(x => x.Email, f => f.WithLabel("Email")))
            .AddField(x => x.Notes, f => f.WithLabel("Notes"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<GroupModel>>(p => p
            .Add(c => c.Model, new GroupModel())
            .Add(c => c.Configuration, config));

        // Assert - both the grouped field and the ungrouped one are present
        var labels = component.FindComponents<FluentTextInput>()
            .Select(i => i.Instance.Label)
            .ToList();
        labels.ShouldContain("Email");
        labels.ShouldContain("Notes");
    }

    [Fact]
    public void A_Hidden_Field_In_A_Group_Should_Not_Render()
    {
        // Arrange - visibility conditions must be honoured inside a group too
        var config = FormBuilder<GroupModel>.Create()
            .AddFieldGroup(group => group
                .WithGroupName("Contact")
                .AddField(x => x.Email, f => f.WithLabel("Email"))
                .AddField(x => x.Phone, f => f.WithLabel("Phone").VisibleWhen(_ => false)))
            .Build();

        // Act
        var component = Render<FormCraftComponent<GroupModel>>(p => p
            .Add(c => c.Model, new GroupModel())
            .Add(c => c.Configuration, config));

        // Assert
        var labels = component.FindComponents<FluentTextInput>()
            .Select(i => i.Instance.Label)
            .ToList();
        labels.ShouldContain("Email");
        labels.ShouldNotContain("Phone");
    }

    [Fact]
    public void A_Group_Header_Right_Content_Should_Render()
    {
        // Arrange
        var config = FormBuilder<GroupModel>.Create()
            .AddFieldGroup(group => group
                .WithGroupName("Contact")
                .WithHeaderRightContent(builder => builder.AddMarkupContent(0, "<span id=\"group-hint\">hint</span>"))
                .AddField(x => x.Email, f => f.WithLabel("Email")))
            .Build();

        // Act
        var component = Render<FormCraftComponent<GroupModel>>(p => p
            .Add(c => c.Model, new GroupModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Find("#group-hint").TextContent.ShouldBe("hint");
    }

    /// <summary>Model with enough fields to group and to leave ungrouped.</summary>
    public class GroupModel
    {
        /// <summary>Grouped field.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Grouped field.</summary>
        public string Phone { get; set; } = string.Empty;

        /// <summary>Grouped field in the second group.</summary>
        public string Street { get; set; } = string.Empty;

        /// <summary>Deliberately left out of every group.</summary>
        public string Notes { get; set; } = string.Empty;
    }
}
