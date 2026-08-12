using Microsoft.Extensions.Logging;
using static FormCraft.ForMudBlazor.UnitTests.Fields.CollectionItemFixture;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests the diagnostic that warns when <c>ShrinkLabel=false</c> cannot be honoured (#181).
/// <para>
/// MudBlazor ORs ShrinkLabel with has-value / has-placeholder / has-start-adornment before
/// emitting the "mud-shrink" class, so the setting is silently inert on a field carrying a
/// placeholder or a start adornment. These tests assert that FormCraft says so, and — just as
/// important — that it stays quiet in the cases where the setting works or where the override
/// is correct behaviour.
/// </para>
/// </summary>
public class ShrinkLabelDiagnosticsTests : MudBlazorTestBase
{
    private readonly CapturingLoggerProvider _logs = new();

    public ShrinkLabelDiagnosticsTests()
    {
        Services.AddLogging(builder => builder.AddProvider(_logs));
    }

    [Fact]
    public void Should_Warn_When_ShrinkLabel_False_Meets_A_Placeholder()
    {
        // Arrange
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Email")
                .WithPlaceholder("user@example.com")
                .WithShrinkLabel(false))
            .Build();

        // Act
        RenderForm(config);

        // Assert - names the field, so a form of many fields points at the right one
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Email");
        warnings[0].ShouldContain("Placeholder");
    }

    [Fact]
    public void Should_Not_Warn_When_ShrinkLabel_False_Is_Honoured()
    {
        // Arrange - no placeholder, so the setting genuinely takes effect
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Email")
                .WithShrinkLabel(false))
            .Build();

        // Act
        RenderForm(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Not_Warn_For_Default_Configuration()
    {
        // Arrange - ShrinkLabel unset; there is no conflict to report
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Email")
                .WithPlaceholder("user@example.com"))
            .Build();

        // Act
        RenderForm(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Warn_When_ShrinkLabel_False_Meets_A_Start_Adornment()
    {
        // Arrange - a start adornment occupies the same space a floating label needs
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Email, Adornment.Start)
                .WithShrinkLabel(false))
            .Build();

        // Act
        RenderForm(config);

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Email");
        warnings[0].ShouldContain("Adornment");
    }

    [Fact]
    public void Should_Not_Warn_For_An_End_Adornment()
    {
        // Arrange - only a START adornment competes with the label; End is harmless
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Email, Adornment.End)
                .WithShrinkLabel(false))
            .Build();

        // Act
        RenderForm(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Not_Warn_When_The_Field_Merely_Has_A_Value()
    {
        // Arrange - a populated field MUST shrink its label or the two overlap. That override is
        // correct behaviour, not a surprise, so warning about it would be noise on every filled form.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Email")
                .WithShrinkLabel(false))
            .Build();

        // Act
        Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, new TestModel { Name = "philippe@example.com" })
            .Add(p => p.Configuration, config));

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Not_Warn_For_A_Lov_Field()
    {
        // Arrange - the LOV input always supplies its own "Click to select..." placeholder, so its
        // label can never float. Warning would fire on every LOV field of any form that sets
        // DefaultShrinkLabel="false" — noise the developer cannot act on.
        //
        // An EXPLICIT placeholder is set here on purpose: without one the diagnostic is silent
        // anyway, because it reads the configured placeholder and LOV's is a rendering fallback
        // the configuration never sees. Only this form of the test actually exercises the opt-out.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.CityId, f => f
                .WithLabel("City")
                .WithPlaceholder("Pick a city")
                .AsLov<TestModel, int, CityDto>(lov => lov
                    .WithDataSource(() => new[] { new CityDto { Id = 1, Name = "Paris" } })
                    .WithKey(c => c.Id)
                    .WithDisplay(c => c.Name))
                .WithShrinkLabel(false))
            .Build();

        // Act
        RenderFormWithPopover(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Aggregate_Into_One_Warning_For_A_Whole_Form()
    {
        // Arrange - DefaultShrinkLabel="false" on a form of placeholder-bearing fields is the
        // realistic way to hit this at scale. One warning listing the fields is actionable;
        // five identical ones are noise that trains developers to ignore the channel.
        var config = FormBuilder<WideModel>
            .Create()
            .AddField(x => x.A, f => f.WithLabel("Alpha").WithPlaceholder("a"))
            .AddField(x => x.B, f => f.WithLabel("Bravo").WithPlaceholder("b"))
            .AddField(x => x.C, f => f.WithLabel("Charlie").WithPlaceholder("c"))
            .AddField(x => x.D, f => f.WithLabel("Delta").WithPlaceholder("d"))
            .AddField(x => x.E, f => f.WithLabel("Echo").WithPlaceholder("e"))
            .Build();

        // Act
        Render<FormCraftComponent<WideModel>>(parameters => parameters
            .Add(p => p.Model, new WideModel())
            .Add(p => p.Configuration, config)
            .Add(p => p.DefaultShrinkLabel, false));

        // Assert - exactly one warning, naming every affected field
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        foreach (var field in new[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo" })
        {
            warnings[0].ShouldContain(field);
        }
    }

    private class WideModel
    {
        public string A { get; set; } = string.Empty;
        public string B { get; set; } = string.Empty;
        public string C { get; set; } = string.Empty;
        public string D { get; set; } = string.Empty;
        public string E { get; set; } = string.Empty;
    }

    [Fact]
    public void Should_Warn_For_A_Collection_Item_Field()
    {
        // Arrange - collection item fields render through CollectionFieldComponent's imperative
        // RenderTreeBuilder path, which resolves presentation attributes itself and so needs the
        // diagnostic wired separately from the component path.
        // The fixture's text item form already labels the field "Product" (#205).
        var config = TextItemForm(field => field
            .WithPlaceholder("e.g. Widget")
            .WithShrinkLabel(false));

        // Act
        this.RenderItemForm(NewOrder(), config);

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Product");
        warnings[0].ShouldContain("Placeholder");
    }

    [Fact]
    public void Should_Not_Warn_For_A_Collection_Item_Field_Without_A_Conflict()
    {
        // Arrange
        var config = TextItemForm(field => field.WithShrinkLabel(false));

        // Act
        this.RenderItemForm(NewOrder(), config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Warn_About_A_Start_Adornment_On_A_Collection_Item_Field()
    {
        // Arrange - #183 suppressed this warning because the collection path dropped the adornment
        // entirely, so ShrinkLabel=false WAS honoured and warning would have pushed the developer
        // to remove a setting that worked. Since #184 the adornment really is rendered there, so
        // the same conflict as the component path applies and the diagnostic must say so.
        var config = TextItemForm(field => field
            .WithAdornment(Icons.Material.Filled.Search, Adornment.Start)
            .WithShrinkLabel(false));

        // Act
        this.RenderItemForm(NewOrder(), config);

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Product");
        warnings[0].ShouldContain("Adornment");
    }

    [Fact]
    public void Should_Not_Warn_About_An_End_Adornment_On_A_Collection_Item_Field()
    {
        // Arrange - only a START adornment competes with the label, on either render path
        var config = TextItemForm(field => field
            .WithAdornment(Icons.Material.Filled.Search, Adornment.End)
            .WithShrinkLabel(false));

        // Act
        this.RenderItemForm(NewOrder(), config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Warn_About_An_Adornment_On_A_Collection_Date_Item_Field()
    {
        // Arrange - inverted by #217, and the inversion is the point rather than a concession.
        //
        // This test used to assert SILENCE, and was right to: the date path passed
        // `rendersAdornment: false`, so a configured start adornment was dropped and ShrinkLabel=false
        // really was honoured. #183's rule — the diagnostic must judge what a path RENDERS, not what
        // was configured — pointed at silence given that behaviour.
        //
        // #217 made the date path forward a configured adornment (while keeping MudDatePicker's
        // calendar icon as the default). The rule has not changed; what the path renders has. A start
        // adornment is now really drawn, it really does pin the label, and warning is now the correct
        // answer under exactly the same rule.
        // The fixture's date item form (#205), relabelled — the callback runs after the default
        // label, so a suite can name the field whatever its assertions read.
        var config = DateItemForm(field => field
            .WithLabel("Ordered on")
            .WithAttribute("Adornment", Adornment.Start)
            .WithShrinkLabel(false));

        // Act - next to a MudPopoverProvider, or MudDatePicker logs a warning of its own that has
        // nothing to do with this diagnostic
        Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<FormCraftComponent<AppointmentModel>>(1);
            builder.AddComponentParameter(2, "Model", NewAppointment());
            builder.AddComponentParameter(3, "Configuration", config);
            builder.CloseComponent();
        });

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("Ordered on");
        warnings[0].ShouldContain("Adornment");
    }

    [Fact]
    public void Should_Count_Two_Fields_That_Share_A_Label()
    {
        // Arrange - grouped forms routinely repeat a label ("Name" under Billing and Shipping).
        // Keying the collector on the label would merge them and report "1 field(s)", so the
        // developer fixes one and believes they are done.
        var config = FormBuilder<WideModel>
            .Create()
            .AddField(x => x.A, f => f.WithLabel("Name").WithPlaceholder("a").WithShrinkLabel(false))
            .AddField(x => x.B, f => f.WithLabel("Name").WithPlaceholder("b").WithShrinkLabel(false))
            .Build();

        // Act
        Render<FormCraftComponent<WideModel>>(parameters => parameters
            .Add(p => p.Model, new WideModel())
            .Add(p => p.Configuration, config));

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("2 field(s)");
    }


    private IRenderedComponent<FormCraftComponent<TestModel>> RenderFormWithPopover(
        IFormConfiguration<TestModel> config)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<FormCraftComponent<TestModel>>(1);
            builder.AddComponentParameter(2, "Model", new TestModel());
            builder.AddComponentParameter(3, "Configuration", config);
            builder.CloseComponent();
        });

        return cut.FindComponent<FormCraftComponent<TestModel>>();
    }

    private class CityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private IRenderedComponent<FormCraftComponent<TestModel>> RenderForm(
        IFormConfiguration<TestModel> config, bool? defaultShrinkLabel = null)
    {
        return Render<FormCraftComponent<TestModel>>(parameters =>
        {
            parameters.Add(p => p.Model, new TestModel());
            parameters.Add(p => p.Configuration, config);
            if (defaultShrinkLabel is { } shrink)
            {
                parameters.Add(p => p.DefaultShrinkLabel, shrink);
            }
        });
    }

    [Fact]
    public void Should_Warn_About_A_Start_Adornment_On_A_Date_Field()
    {
        // Arrange - inverted by #203, for the same reason and under the same rule that #217 inverted
        // the collection-path twin above (Should_Warn_About_An_Adornment_On_A_Collection_Date_Item_Field).
        //
        // This test used to assert SILENCE, and was right to under #212: FormCraft's date component
        // bound none of our adornments, so a configured start adornment was dropped, the label
        // floated exactly as asked, and warning would have told the developer to remove a setting
        // that was working. #183's rule — the diagnostic judges what a path RENDERS, not what was
        // configured — pointed at silence given that behaviour.
        //
        // #203 converged the two render paths onto this component, which meant the component had to
        // learn #217's date adornment binding or date ITEM fields would have silently lost it. So a
        // configured start adornment is now really drawn here too, it really does pin the label, and
        // warning is the correct answer under exactly the same unchanged rule.
        //
        // The two tests asserting opposite things was the divergence; them agreeing is the fix.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.When, f => f
                .WithLabel("When")
                .WithAttribute("Adornment", Adornment.Start)
                .WithShrinkLabel(false))
            .Build();

        // Act - through the popover-providing host: MudDatePicker logs its own unrelated
        // "Missing <MudPopoverProvider />" warning otherwise, which this logger would capture and
        // which has nothing to do with ShrinkLabel.
        RenderFormWithPopover(config);

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("When");
        warnings[0].ShouldContain("Adornment");
    }

    [Fact]
    public void Should_Not_Warn_For_A_Date_Field_That_Configures_No_Adornment()
    {
        // Arrange - the other half of the pair, and the one that keeps #212 honest: MudDatePicker's
        // OWN calendar adornment sits at the End, where it cannot displace a floating label. An
        // unconfigured date field must therefore stay silent even though this component now binds an
        // adornment unconditionally.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.When, f => f
                .WithLabel("When")
                .WithShrinkLabel(false))
            .Build();

        // Act
        RenderFormWithPopover(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Not_Warn_For_A_Select_Field_That_Renders_No_Adornment()
    {
        // Arrange - a second component type that binds no adornment, so the fix is shown to be about
        // the rule rather than about one special-cased component.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("City")
                .WithSelectOptions([
                    new SelectOption<string>("paris", "Paris"),
                    new SelectOption<string>("london", "London")
                ])
                .WithAttribute("Adornment", Adornment.Start)
                .WithShrinkLabel(false))
            .Build();

        // Act - MudSelect also needs the popover provider; same reasoning as the date case above.
        RenderFormWithPopover(config);

        // Assert
        _logs.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Still_Warn_For_A_Numeric_Field_That_Does_Render_The_Adornment()
    {
        // Arrange - the guard on the fix. #191 made numeric adornments real, so this warning is
        // CORRECT and must survive the removal of the false ones. Silencing everything would have
        // been an easy way to make the negative tests above pass.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.CityId, f => f
                .WithLabel("City id")
                .WithAttribute("Adornment", Adornment.Start)
                .WithAttribute("AdornmentIcon", Icons.Material.Filled.Search)
                .WithShrinkLabel(false))
            .Build();

        // Act
        RenderForm(config);

        // Assert
        var warnings = _logs.Warnings;
        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("City id");
        warnings[0].ShouldContain("Adornment");
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int CityId { get; set; }
        public DateTime? When { get; set; }
    }

    /// <summary>
    /// Minimal ILoggerProvider that records warning-level messages so tests can assert on
    /// what a developer would actually see in their console.
    /// </summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _warnings = [];

        public IReadOnlyList<string> Warnings
        {
            get
            {
                lock (_warnings)
                {
                    return _warnings.ToList();
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Dispose()
        {
        }

        private void Record(string message)
        {
            lock (_warnings)
            {
                _warnings.Add(message);
            }
        }

        private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (logLevel >= LogLevel.Warning)
                {
                    provider.Record(formatter(state, exception));
                }
            }
        }
    }
}
