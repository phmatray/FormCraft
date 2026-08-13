using FormCraft;

namespace FormCraft.DemoBlazorApp.Components.Shared;

/// <summary>
/// The home page's opening statement: a FormCraft builder chain beside the form it produces.
/// </summary>
/// <remarks>
/// Both halves are derived from <see cref="AllFields"/>, so the code on the left is exactly the
/// code that built the form on the right — adding a field writes a line and renders a control in
/// the same frame. The form is a real <c>FormCraftComponent</c>.
/// </remarks>
public partial class BindingHero
{
    /// <summary>The demo model. Deliberately small: the point is the mapping, not the model.</summary>
    public class Contact
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Topic { get; set; } = "";
        public bool Consent { get; set; }
    }

    /// <summary>One coloured run of the displayed chain.</summary>
    /// <param name="Text">The literal text.</param>
    /// <param name="Kind">A CSS class: <c>t-call</c>, <c>t-str</c>, <c>t-prop</c>, or empty for plain.</param>
    public sealed record Token(string Text, string Kind);

    /// <summary>A field the visitor can add or remove, with the single line of C# that creates it.</summary>
    /// <param name="Key">Stable id, used to tie the code line to the rendered control.</param>
    /// <param name="Label">Name shown on the toggle.</param>
    /// <param name="Tokens">The chain line, already split for colouring.</param>
    public sealed record FieldSpec(string Key, string Label, IReadOnlyList<Token> Tokens);

    // Every line below is a real FormCraft call. If you change one, change the matching
    // arm of ApplyField so the panel keeps telling the truth.
    private static readonly IReadOnlyList<FieldSpec> AllFields =
    [
        new("name", "Name",
        [
            new(".", ""), new("AddRequiredTextField", "t-call"), new("(x => x.", ""),
            new("Name", "t-prop"), new(", ", ""), new("\"Name\"", "t-str"), new(")", "")
        ]),
        new("email", "Email",
        [
            new(".", ""), new("AddEmailField", "t-call"), new("(x => x.", ""),
            new("Email", "t-prop"), new(")", "")
        ]),
        new("phone", "Phone",
        [
            new(".", ""), new("AddPhoneField", "t-call"), new("(x => x.", ""),
            new("Phone", "t-prop"), new(")", "")
        ]),
        // Kept to two short options so the line does not need a horizontal
        // scrollbar in the hero panel.
        new("topic", "Topic",
        [
            new(".", ""), new("AddDropdownField", "t-call"), new("(x => x.", ""),
            new("Topic", "t-prop"), new(", ", ""), new("\"Topic\"", "t-str"), new(", ", ""),
            new("(\"bug\", \"Bug\")", "t-str"), new(", ", ""), new("(\"idea\", \"Idea\")", "t-str"),
            new(")", "")
        ]),
        new("consent", "Consent",
        [
            new(".", ""), new("AddCheckboxField", "t-call"), new("(x => x.", ""),
            new("Consent", "t-prop"), new(", ", ""), new("\"Email me about releases\"", "t-str"), new(")", "")
        ])
    ];

    private readonly HashSet<string> _enabled = ["name", "email"];

    private Contact _model = new();
    private IFormConfiguration<Contact>? _configuration;
    private string? _active;
    private string? _justAdded;
    private bool _submitted;

    protected override void OnInitialized() => Rebuild();

    private IEnumerable<FieldSpec> EnabledFields() =>
        AllFields.Where(f => _enabled.Contains(f.Key));

    /// <summary>
    /// 1-based position of the highlighted field among those currently rendered. The form's
    /// controls are direct children in this same order, so this is what lets CSS light up the
    /// control that belongs to the hovered line.
    /// </summary>
    private int ActiveIndex()
    {
        if (_active is null)
        {
            return 0;
        }

        var index = EnabledFields().ToList().FindIndex(f => f.Key == _active);
        return index < 0 ? 0 : index + 1;
    }

    private void Toggle(string key)
    {
        if (!_enabled.Remove(key))
        {
            _enabled.Add(key);
            _justAdded = key;
            _active = key;
        }
        else if (_active == key)
        {
            _active = null;
        }

        _submitted = false;
        Rebuild();

        // No timer clears _justAdded any more, and none is needed: fcBindFlash runs for 0.9s and both
        // ends of it are `background-color: transparent` with the default fill-mode, so a line that
        // keeps the class is pixel-identical to one that never had it. The delay this replaces existed
        // only to strip that inert class, and awaiting it meant StateHasChanged() could resume on a
        // component the renderer had already disposed if the visitor navigated inside the window.
        // The flash still replays on a re-add because the code lines are @key'd by field: toggling a
        // field off destroys its element, so toggling it back on creates a fresh one.
    }

    private void Rebuild()
    {
        var builder = FormBuilder<Contact>.Create();

        foreach (var field in EnabledFields())
        {
            builder = ApplyField(builder, field.Key);
        }

        _configuration = builder.Build();
    }

    private static FormBuilder<Contact> ApplyField(FormBuilder<Contact> builder, string key) => key switch
    {
        "name" => builder.AddRequiredTextField(x => x.Name, "Name"),
        "email" => builder.AddEmailField(x => x.Email),
        "phone" => builder.AddPhoneField(x => x.Phone),
        "topic" => builder.AddDropdownField(x => x.Topic, "Topic", ("bug", "Bug"), ("idea", "Idea")),
        "consent" => builder.AddCheckboxField(x => x.Consent, "Email me about releases"),
        _ => builder
    };

    private void HandleSubmit(Contact model)
    {
        _submitted = true;
        _model = model;
    }
}
