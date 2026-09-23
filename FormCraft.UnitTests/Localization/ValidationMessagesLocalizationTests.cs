using System.Collections;
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

    public class TestModel
    {
        public int Age { get; set; }
        public string Email { get; set; } = "";
    }
}
