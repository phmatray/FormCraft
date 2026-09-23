using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Resources;

namespace FormCraft.UnitTests.Localization;

/// <summary>
/// Pins default validation messages to the embedded <c>ValidationMessages.resx</c> (and its
/// <c>fr</c> satellite), looked up under <see cref="CultureInfo.CurrentUICulture"/> (#354).
/// </summary>
public class ValidationMessagesLocalizationTests
{
    [Fact]
    public void AddNumericField_Required_Message_Is_French_Under_FrFr_Culture()
    {
        WithCulture("fr-FR", () =>
        {
            var config = FormBuilder<TestModel>.Create()
                .AddNumericField(x => x.Age, "Âge")
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<TestModel, int>>()
                .First(f => f.FieldName == "Age");
            var validator = field.TypedConfiguration.Validators.ShouldHaveSingleItem();

            // Not ValidateAsync: RequiredValidator<TModel, int> treats every non-null int, including
            // 0, as present (its switch has no int-specific case), so it never fails for a numeric
            // field — read the pre-formatted message the constructor built instead.
            validator.ErrorMessage.ShouldBe("Le champ Âge est obligatoire");
        });
    }

    [Fact]
    public void AddNumericField_Required_Message_Is_English_Under_EnUs_Culture()
    {
        WithCulture("en-US", () =>
        {
            var config = FormBuilder<TestModel>.Create()
                .AddNumericField(x => x.Age, "Âge")
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<TestModel, int>>()
                .First(f => f.FieldName == "Age");
            var validator = field.TypedConfiguration.Validators.ShouldHaveSingleItem();

            validator.ErrorMessage.ShouldBe("Âge is required");
        });
    }

    [Fact]
    public async Task AddNumericField_Range_Message_Is_French_Under_FrFr_Culture()
    {
        await WithCultureAsync("fr-FR", async () =>
        {
            var services = A.Fake<IServiceProvider>();
            var config = FormBuilder<TestModel>.Create()
                .AddNumericField(x => x.Age, "Âge", min: 18, max: 65, required: false)
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<TestModel, int>>()
                .First(f => f.FieldName == "Age");
            var validator = field.TypedConfiguration.Validators.ShouldHaveSingleItem();

            var result = await validator.ValidateAsync(new TestModel(), 17, services);
            result.IsValid.ShouldBeFalse();
            result.ErrorMessage.ShouldBe("Doit être compris entre 18 et 65");
        });
    }

    [Fact]
    public async Task WithEmailValidation_Default_Message_Is_French_Under_FrFr_Culture()
    {
        await WithCultureAsync("fr-FR", async () =>
        {
            var services = A.Fake<IServiceProvider>();
            var config = FormBuilder<TestModel>.Create()
                .AddField(x => x.Email, field => field.WithEmailValidation())
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<TestModel, string>>()
                .First(f => f.FieldName == "Email");
            var validator = field.TypedConfiguration.Validators.ShouldHaveSingleItem();

            var result = await validator.ValidateAsync(new TestModel(), "not-an-email", services);
            result.IsValid.ShouldBeFalse();
            result.ErrorMessage.ShouldBe("Veuillez saisir une adresse e-mail valide");
        });
    }

    [Fact]
    public void AddFieldsFromAttributes_Required_Message_Is_French_Under_FrFr_Culture()
    {
        WithCulture("fr-FR", () =>
        {
            var config = FormBuilder<AttributeTestModel>.Create()
                .AddFieldsFromAttributes()
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<AttributeTestModel, string>>()
                .First(f => f.FieldName == "Name");
            var validator = field.TypedConfiguration.Validators
                .OfType<RequiredValidator<AttributeTestModel, string>>()
                .ShouldHaveSingleItem();

            validator.ErrorMessage.ShouldBe("Le champ Nom est obligatoire");
        });
    }

    [Fact]
    public void RequiredValidator_Default_Message_Is_French_Under_FrFr_Culture()
    {
        WithCulture("fr-FR", () =>
        {
            var validator = new RequiredValidator<TestModel, string>();

            validator.ErrorMessage.ShouldBe("Ce champ est obligatoire.");
        });
    }

    [Fact]
    public void RequiredValidator_Explicit_Message_Still_Wins_Under_FrFr_Culture()
    {
        WithCulture("fr-FR", () =>
        {
            var validator = new RequiredValidator<TestModel, string>("Custom");

            validator.ErrorMessage.ShouldBe("Custom");
        });
    }

    [Fact]
    public async Task CollectionFieldValidator_MinItems_Message_Is_French_Under_FrFr_Culture()
    {
        await WithCultureAsync("fr-FR", async () =>
        {
            var services = A.Fake<IServiceProvider>();
            var config = new CollectionFieldConfiguration<CollectionTestModel, CollectionItemTestModel>(x => x.Items)
            {
                MinItems = 1
            };
            var validator = new CollectionFieldValidator<CollectionTestModel, CollectionItemTestModel>(config);

            var errors = await validator.ValidateAsync(new CollectionTestModel(), services);

            errors.ShouldHaveSingleItem();
            errors[0].ShouldBe("Items nécessite au moins 1 élément(s).");
        });
    }

    [Fact]
    public void Every_Neutral_Resource_Key_Has_A_Non_Empty_French_Value()
    {
        // Base name is "FormCraft.ValidationMessages", not "FormCraft.Resources.ValidationMessages"
        // — see the comment on ValidationMessages.Resources for why.
        var resources = new ResourceManager("FormCraft.ValidationMessages", typeof(FormBuilder<>).Assembly);
        var neutral = resources.GetResourceSet(CultureInfo.InvariantCulture, true, false);
        var french = resources.GetResourceSet(new CultureInfo("fr"), true, false);

        neutral.ShouldNotBeNull();
        french.ShouldNotBeNull();

        var missing = new List<string>();
        foreach (DictionaryEntry entry in neutral!)
        {
            var key = (string)entry.Key;
            var value = french!.GetString(key);
            if (string.IsNullOrEmpty(value))
            {
                missing.Add(key);
            }
        }

        missing.ShouldBeEmpty();
    }

    /// <summary>
    /// Sets <see cref="CultureInfo.CurrentCulture"/> and <see cref="CultureInfo.CurrentUICulture"/>
    /// for the duration of <paramref name="action"/>, restoring the previous values afterwards even
    /// if the assertion throws. Both are async-local, so this is safe under xunit v3 parallelism.
    /// </summary>
    private static void WithCulture(string culture, Action action)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var info = new CultureInfo(culture);
            CultureInfo.CurrentCulture = info;
            CultureInfo.CurrentUICulture = info;
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    /// <summary>Async counterpart of <see cref="WithCulture"/>, for assertions that await <c>ValidateAsync</c>.</summary>
    private static async Task WithCultureAsync(string culture, Func<Task> action)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var info = new CultureInfo(culture);
            CultureInfo.CurrentCulture = info;
            CultureInfo.CurrentUICulture = info;
            await action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    public class TestModel
    {
        public int Age { get; set; }
        public string Email { get; set; } = "";
    }

    public class AttributeTestModel
    {
        [Required]
        [TextField("Nom")]
        public string Name { get; set; } = "";
    }

    public class CollectionTestModel
    {
        public List<CollectionItemTestModel> Items { get; set; } = new();
    }

    public class CollectionItemTestModel
    {
        public string Name { get; set; } = "";
    }
}
