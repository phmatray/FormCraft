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

    // The tests below close gaps a verification-gap review found in the original three tasks:
    // several default messages (RequiredSelect, RequiredAtLeastOne, InvalidPhone, AmountPositive,
    // PercentageRange, SpecialCharacterRequired, and the MinLength/MaxLength family) were wired to
    // ValidationMessages but never had their actual TEXT asserted anywhere — only validator *count*
    // was. A wrong key, or two swapped resx values, would have shipped undetected.

    [Fact]
    public void AddDropdownField_Required_Message_Is_French_Under_FrFr_Culture()
    {
        WithCulture("fr-FR", () =>
        {
            var config = FormBuilder<TestModel>.Create()
                .AddDropdownField(x => x.Country, "Pays", ("FR", "France"))
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<TestModel, string>>()
                .First(f => f.FieldName == "Country");
            var validator = field.TypedConfiguration.Validators
                .OfType<RequiredValidator<TestModel, string>>()
                .ShouldHaveSingleItem();

            validator.ErrorMessage.ShouldBe("Veuillez sélectionner Pays");
        });
    }

    [Fact]
    public void AddMultipleFileUploadField_Required_Message_Is_French_Under_FrFr_Culture()
    {
        WithCulture("fr-FR", () =>
        {
            var config = FormBuilder<TestModel>.Create()
                .AddMultipleFileUploadField(x => x.Documents, "Documents", required: true)
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<TestModel, IReadOnlyList<IBrowserFile>>>()
                .First(f => f.FieldName == "Documents");
            var validator = field.TypedConfiguration.Validators
                .OfType<RequiredValidator<TestModel, IReadOnlyList<IBrowserFile>>>()
                .ShouldHaveSingleItem();

            validator.ErrorMessage.ShouldBe("Au moins un(e) documents est requis(e)");
        });
    }

    [Fact]
    public async Task AddPhoneField_Invalid_Message_Is_French_Under_FrFr_Culture()
    {
        await WithCultureAsync("fr-FR", async () =>
        {
            var services = A.Fake<IServiceProvider>();
            var config = FormBuilder<TestModel>.Create()
                .AddPhoneField(x => x.Phone)
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<TestModel, string>>()
                .First(f => f.FieldName == "Phone");
            var validator = field.TypedConfiguration.Validators.ShouldHaveSingleItem();

            var result = await validator.ValidateAsync(new TestModel(), "abc", services);
            result.IsValid.ShouldBeFalse();
            result.ErrorMessage.ShouldBe("Veuillez saisir un numéro de téléphone valide");
        });
    }

    [Fact]
    public async Task AddCurrencyField_AmountPositive_Message_Is_French_Under_FrFr_Culture()
    {
        await WithCultureAsync("fr-FR", async () =>
        {
            var services = A.Fake<IServiceProvider>();
            var config = FormBuilder<TestModel>.Create()
                .AddCurrencyField(x => x.Amount, "Montant", required: false)
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<TestModel, decimal>>()
                .First(f => f.FieldName == "Amount");
            var validator = field.TypedConfiguration.Validators.ShouldHaveSingleItem();

            var result = await validator.ValidateAsync(new TestModel(), -1m, services);
            result.IsValid.ShouldBeFalse();
            result.ErrorMessage.ShouldBe("Le montant doit être positif");
        });
    }

    [Fact]
    public async Task AddPercentageField_PercentageRange_Message_Is_French_Under_FrFr_Culture()
    {
        await WithCultureAsync("fr-FR", async () =>
        {
            var services = A.Fake<IServiceProvider>();
            var config = FormBuilder<TestModel>.Create()
                .AddPercentageField(x => x.Rate, "Taux", required: false)
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<TestModel, decimal>>()
                .First(f => f.FieldName == "Rate");
            var validator = field.TypedConfiguration.Validators.ShouldHaveSingleItem();

            var result = await validator.ValidateAsync(new TestModel(), 150m, services);
            result.IsValid.ShouldBeFalse();
            result.ErrorMessage.ShouldBe("Le pourcentage doit être compris entre 0 et 100");
        });
    }

    [Fact]
    public async Task AddPasswordField_SpecialCharacterRequired_Message_Is_French_Under_FrFr_Culture()
    {
        await WithCultureAsync("fr-FR", async () =>
        {
            var services = A.Fake<IServiceProvider>();
            var config = FormBuilder<TestModel>.Create()
                .AddPasswordField(x => x.Password)
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<TestModel, string>>()
                .First(f => f.FieldName == "Password");
            // Validators, in order: Required, MinLength(8), SpecialCharacterRequired.
            var validator = field.TypedConfiguration.Validators[2];

            // 8 chars, satisfies MinLength — isolates the special-character validator.
            var result = await validator.ValidateAsync(new TestModel(), "abcdefgh", services);
            result.IsValid.ShouldBeFalse();
            result.ErrorMessage.ShouldBe("Doit contenir au moins un caractère spécial");
        });
    }

    [Fact]
    public async Task AddRequiredTextField_Length_Messages_Are_French_Under_FrFr_Culture()
    {
        await WithCultureAsync("fr-FR", async () =>
        {
            var services = A.Fake<IServiceProvider>();
            var config = FormBuilder<TestModel>.Create()
                .AddRequiredTextField(x => x.Bio, "Bio", minLength: 3, maxLength: 5)
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<TestModel, string>>()
                .First(f => f.FieldName == "Bio");
            // Validators, in order: Required, MinLength(3), MaxLength(5).
            var validators = field.TypedConfiguration.Validators;

            var tooShort = await validators[1].ValidateAsync(new TestModel(), "ab", services);
            tooShort.IsValid.ShouldBeFalse();
            tooShort.ErrorMessage.ShouldBe("Doit comporter au moins 3 caractères");

            var tooLong = await validators[2].ValidateAsync(new TestModel(), "abcdef", services);
            tooLong.IsValid.ShouldBeFalse();
            tooLong.ErrorMessage.ShouldBe("Ne doit pas dépasser 5 caractères");
        });
    }

    [Fact]
    public async Task WithMinLength_WithMaxLength_Default_Messages_Are_English_Under_EnUs_Culture()
    {
        // English, not French: MinLengthLong/MaxLengthLong translate identically to MinLength/
        // MaxLength in French ("long" adds nothing idiomatic), so only the neutral English text —
        // which keeps the "long" suffix — can tell the two key families apart.
        await WithCultureAsync("en-US", async () =>
        {
            var services = A.Fake<IServiceProvider>();
            var config = FormBuilder<TestModel>.Create()
                .AddField(x => x.Bio, field => field.WithMinLength(3).WithMaxLength(5))
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<TestModel, string>>()
                .First(f => f.FieldName == "Bio");
            var validators = field.TypedConfiguration.Validators;
            validators.Count.ShouldBe(2);

            var tooShort = await validators[0].ValidateAsync(new TestModel(), "ab", services);
            tooShort.ErrorMessage.ShouldBe("Must be at least 3 characters long");

            var tooLong = await validators[1].ValidateAsync(new TestModel(), "abcdef", services);
            tooLong.ErrorMessage.ShouldBe("Must be no more than 5 characters long");
        });
    }

    [Fact]
    public async Task AddFieldsFromAttributes_MinLength_MaxLength_Messages_Are_French_Under_FrFr_Culture()
    {
        await WithCultureAsync("fr-FR", async () =>
        {
            var services = A.Fake<IServiceProvider>();
            var config = FormBuilder<AttributeLengthTestModel>.Create()
                .AddFieldsFromAttributes()
                .Build();

            var field = config.Fields
                .OfType<FieldConfigurationWrapper<AttributeLengthTestModel, string>>()
                .First(f => f.FieldName == "Bio");
            var validators = field.TypedConfiguration.Validators;
            validators.Count.ShouldBe(2);

            var tooShort = await validators[0].ValidateAsync(new AttributeLengthTestModel(), "ab", services);
            tooShort.ErrorMessage.ShouldBe("Doit comporter au moins 3 caractères");

            var tooLong = await validators[1].ValidateAsync(new AttributeLengthTestModel(), "abcdef", services);
            tooLong.ErrorMessage.ShouldBe("Ne doit pas dépasser 5 caractères");
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
        public string Country { get; set; } = "";
        public IReadOnlyList<IBrowserFile> Documents { get; set; } = new List<IBrowserFile>();
        public string Phone { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal Rate { get; set; }
        public string Password { get; set; } = "";
        public string Bio { get; set; } = "";
    }

    public class AttributeTestModel
    {
        [Required]
        [TextField("Nom")]
        public string Name { get; set; } = "";
    }

    public class AttributeLengthTestModel
    {
        [TextField("Bio")]
        [MinLength(3)]
        [MaxLength(5)]
        public string Bio { get; set; } = "";
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
