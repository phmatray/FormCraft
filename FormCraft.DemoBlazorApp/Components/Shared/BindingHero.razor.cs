using FormCraft.ForMudBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class BindingHero
{
    public class Contact
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Topic { get; set; } = "general";
        public bool Consent { get; set; }

        public override string ToString() =>
            $"Contact {{ Name = \"{Name}\", Email = \"{Email}\", Topic = \"{Topic}\" }}";
    }

    private static readonly SelectOption<string>[] Topics =
    [
        new("general", "General question"),
        new("bug", "Bug report")
    ];

    /// <summary>
    /// The file shown in the editor. It is the configuration <see cref="Build"/> really runs:
    /// change one and change the other, or the hero stops telling the truth.
    /// </summary>
    private const string Source =
        """
        @page "/contact"

        <FormCraftComponent
            TModel="Contact"
            Model="@_contact"
            Configuration="@_config"
            OnValidSubmit="@Send" />

        @code {
            private readonly Contact _contact = new();

            private static readonly SelectOption<string>[] Topics =
                [new("general", "General question"), new("bug", "Bug report")];

            private readonly IFormConfiguration<Contact> _config =
                FormBuilder<Contact>.Create()
                    .AddField(x => x.Name, f => f
                        .WithLabel("Name")
                        .Required())
                    .AddField(x => x.Email, f => f
                        .WithLabel("Email")
                        .Required()
                        .WithEmailValidation())
                    .AddField(x => x.Topic, f => f
                        .WithLabel("Topic")
                        .WithSelectOptions(Topics))
                    .AddField(x => x.Consent, f => f
                        .WithLabel("Reply by email"))
                    .Build();

            private void Send(Contact contact) => Console.WriteLine(contact);
        }
        """;

    private static readonly string[] Lines = Source.Split('\n');

    private static readonly int LineCount = Lines.Length;

    /// <summary>
    /// For each line, how many fields are complete once it is written: a field appears when the
    /// line that closes its AddField statement does, i.e. the line before the next AddField or Build.
    /// </summary>
    private static readonly int[] FieldsAfterLine = BuildFieldTimeline();

    private static int[] BuildFieldTimeline()
    {
        var result = new int[Lines.Length];
        var done = 0;
        var open = false;
        for (var i = 0; i < Lines.Length; i++)
        {
            if (Lines[i].Contains(".AddField(", StringComparison.Ordinal))
            {
                open = true;
            }

            var next = i + 1 < Lines.Length ? Lines[i + 1] : "";
            if (open && (next.Contains(".AddField(", StringComparison.Ordinal) || next.Contains(".Build()", StringComparison.Ordinal)))
            {
                done++;
                open = false;
            }

            result[i] = done;
        }

        return result;
    }

    private static readonly (string Title, string Detail)[] Phases =
    [
        ("Describe", "One <code>AddField</code> per field."),
        ("Render", "Each field appears as its line is written."),
        ("Validate", "Rules run on the server, against your model."),
        ("Submit", "<code>OnValidSubmit</code> gets a typed <code>Contact</code>.")
    ];

    private readonly IFormConfiguration<Contact> _configuration = Build();

    private static IFormConfiguration<Contact> Build() =>
        FormBuilder<Contact>.Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Name")
                .Required())
            .AddField(x => x.Email, f => f
                .WithLabel("Email")
                .Required()
                .WithEmailValidation())
            .AddField(x => x.Topic, f => f
                .WithLabel("Topic")
                .WithSelectOptions(Topics))
            .AddField(x => x.Consent, f => f
                .WithLabel("Reply by email"))
            .Build();

    private Contact _model = new();
    private FormCraftComponent<Contact>? _form;
    private ElementReference _codeHost;

    private int _shownLines;
    private int _shownFields;
    private bool _showSubmit;
    private int _phase = -1;
    private string? _result;
    private bool _resultIsError;
    private bool _paused;
    private int _run;
    // The form is @key'd on this: a replay needs a fresh EditContext bound to the fresh model,
    // or validation keeps reading the previous run's instance.
    private int _formKey;

    // A plain string, not an @if inside <style>: Razor reads a line starting with '#' there as a
    // C# preprocessor directive.
    private string SubmitRule => _showSubmit ? "" : "#fc-hero-form form > button { opacity: 0; pointer-events: none; }";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        var reduceMotion = false;
        try
        {
            await JS.InvokeVoidAsync("formcraftCode.highlightUnder", _codeHost);
            reduceMotion = await JS.InvokeAsync<bool>("formcraftPrefersReducedMotion");
        }
        catch (JSException)
        {
            // Unhighlighted source still reads; play the sequence regardless.
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (reduceMotion)
        {
            await SkipAsync();
        }
        else
        {
            _ = PlayAsync();
        }
    }

    /// <summary>Waits, honouring pause, and reports false once this run is superseded or the page is gone.</summary>
    private async Task<bool> StepAsync(int milliseconds, int run)
    {
        while (_paused)
        {
            if (!await DelayAsync(60) || run != _run)
            {
                return false;
            }
        }

        return await DelayAsync(milliseconds) && run == _run;
    }

    private async Task PlayAsync()
    {
        var run = ++_run;
        Reset();
        _phase = 0;
        StateHasChanged();

        for (var i = 0; i < LineCount; i++)
        {
            if (!await StepAsync(string.IsNullOrWhiteSpace(Lines[i]) ? 60 : 150, run))
            {
                return;
            }

            _shownLines = i + 1;
            await FollowAsync();
            if (FieldsAfterLine[i] > _shownFields)
            {
                _shownFields = FieldsAfterLine[i];
                _phase = 1;
            }

            StateHasChanged();
        }

        _showSubmit = true;
        StateHasChanged();
        if (!await StepAsync(700, run))
        {
            return;
        }

        // Validate: a good name, then a bad email, then the fix.
        _phase = 2;
        if (!await TypeAsync(v => _model.Name += v, "Ada Lovelace", run))
        {
            return;
        }

        NotifyChanged(nameof(Contact.Name));
        if (!await TypeAsync(v => _model.Email += v, "ada@", run))
        {
            return;
        }

        NotifyChanged(nameof(Contact.Email));
        StateHasChanged();
        if (!await StepAsync(1400, run))
        {
            return;
        }

        if (!await TypeAsync(v => _model.Email += v, "analytical.io", run))
        {
            return;
        }

        NotifyChanged(nameof(Contact.Email));
        StateHasChanged();
        if (!await StepAsync(800, run))
        {
            return;
        }

        _phase = 3;
        await SubmitAsync();
        if (!await StepAsync(900, run))
        {
            return;
        }

        _phase = Phases.Length;
        StateHasChanged();
    }

    private async Task<bool> TypeAsync(Action<string> append, string text, int run)
    {
        foreach (var ch in text)
        {
            if (!await StepAsync(45, run))
            {
                return false;
            }

            append(ch.ToString());
            StateHasChanged();
        }

        return true;
    }

    private async Task FollowAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("formcraftCode.follow", _codeHost, _shownLines);
        }
        catch (JSException)
        {
            // The editor just stays where it is.
        }
        catch (InvalidOperationException)
        {
            // Torn down mid-sequence.
        }
    }

    /// <summary>Field-level validation, as if the visitor had just left the field.</summary>
    private void NotifyChanged(string field)
    {
        var context = _form?.GetEditContext();
        context?.NotifyFieldChanged(context.Field(field));
    }

    private async Task SubmitAsync()
    {
        if (_form is null)
        {
            return;
        }

        if (await _form.ValidateAsync())
        {
            HandleSubmit(_model);
        }
        else
        {
            _result = "Fix the highlighted fields. Nothing was sent.";
            _resultIsError = true;
        }

        if (!IsDisposed)
        {
            StateHasChanged();
        }
    }

    private void HandleSubmit(Contact contact)
    {
        _result = $"OnValidSubmit → {contact}";
        _resultIsError = false;
    }

    private void Reset()
    {
        _model = new Contact();
        _formKey++;
        _shownLines = 0;
        _shownFields = 0;
        _showSubmit = false;
        _result = null;
        _resultIsError = false;
    }

    private void TogglePause() => _paused = !_paused;

    private void Replay()
    {
        _paused = false;
        _ = PlayAsync();
    }

    private async Task SkipAsync()
    {
        _run++;
        _paused = false;
        _shownLines = LineCount;
        _shownFields = FieldsAfterLine[^1];
        _showSubmit = true;
        _model.Name = "Ada Lovelace";
        _model.Email = "ada@analytical.io";
        _phase = Phases.Length;
        StateHasChanged();
        await Task.Yield();
        await SubmitAsync();
    }
}
