using FormCraft;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class InteractivePlayground
{
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    private string _selectedTemplate = "contact";
    private bool _showValidation = true;
    private bool _submitted;
    private bool _copied;

    private PlaygroundModel _model = new();
    private IFormConfiguration<PlaygroundModel>? _configuration;

    // Field configurations per template
    private readonly Dictionary<string, List<string>> _templateFields = new()
    {
        ["contact"] = ["name", "email", "phone", "message"],
        ["login"] = ["email", "password", "rememberMe"],
        ["order"] = ["name", "email", "quantity", "notes"]
    };

    private readonly Dictionary<string, string> _fieldLabels = new()
    {
        ["name"] = "Name",
        ["email"] = "Email",
        ["phone"] = "Phone",
        ["message"] = "Message",
        ["password"] = "Password",
        ["rememberMe"] = "Remember Me",
        ["quantity"] = "Quantity",
        ["notes"] = "Notes"
    };

    private HashSet<string> _enabledFields = new() { "name", "email" };

    protected override void OnInitialized()
    {
        UpdateEnabledFieldsForTemplate();
        RebuildConfiguration();
    }

    private void UpdateEnabledFieldsForTemplate()
    {
        var availableFields = _templateFields[_selectedTemplate];
        _enabledFields = new HashSet<string>(availableFields.Take(2));
    }

    private Dictionary<string, string> GetAvailableFields()
    {
        var available = _templateFields[_selectedTemplate];
        return available.ToDictionary(f => f, f => _fieldLabels[f]);
    }

    private bool IsFieldEnabled(string field) => _enabledFields.Contains(field);

    private void ToggleField(string field, bool enabled)
    {
        if (enabled)
            _enabledFields.Add(field);
        else
            _enabledFields.Remove(field);

        RebuildConfiguration();
    }

    private string _previousTemplate = "";

    protected override void OnParametersSet()
    {
        if (_selectedTemplate != _previousTemplate)
        {
            _previousTemplate = _selectedTemplate;
            UpdateEnabledFieldsForTemplate();
            _model = new PlaygroundModel();
            RebuildConfiguration();
        }
    }

    private void RebuildConfiguration()
    {
        var builder = FormBuilder<PlaygroundModel>.Create();

        foreach (var field in _enabledFields.OrderBy(f => _templateFields[_selectedTemplate].IndexOf(f)))
        {
            builder = field switch
            {
                "name" => builder.AddField(x => x.Name, f =>
                {
                    f.WithLabel("Name").WithPlaceholder("Enter your name");
                    if (_showValidation) f.Required("Name is required");
                }),
                "email" => builder.AddField(x => x.Email, f =>
                {
                    f.WithLabel("Email").WithPlaceholder("Enter your email").WithInputType("email");
                    if (_showValidation) f.Required("Email is required");
                }),
                "phone" => builder.AddField(x => x.Phone, f =>
                    f.WithLabel("Phone").WithPlaceholder("Enter your phone")),
                "message" => builder.AddField(x => x.Message, f =>
                    f.WithLabel("Message").WithPlaceholder("Enter your message").WithInputType("multiline")),
                "password" => builder.AddField(x => x.Password, f =>
                {
                    f.WithLabel("Password").WithPlaceholder("Enter password").WithInputType("password");
                    if (_showValidation) f.Required("Password is required");
                }),
                "rememberMe" => builder.AddField(x => x.RememberMe, f =>
                    f.WithLabel("Remember me")),
                "quantity" => builder.AddField(x => x.Quantity, f =>
                {
                    f.WithLabel("Quantity").WithPlaceholder("Enter quantity");
                    if (_showValidation) f.Required("Quantity is required");
                }),
                "notes" => builder.AddField(x => x.Notes, f =>
                    f.WithLabel("Notes").WithPlaceholder("Additional notes")),
                _ => builder
            };
        }

        _configuration = builder.Build();
        StateHasChanged();
    }

    private string GenerateCode()
    {
        var lines = new List<string>
        {
            "var config = FormBuilder<MyModel>.Create()"
        };

        foreach (var field in _enabledFields.OrderBy(f => _templateFields[_selectedTemplate].IndexOf(f)))
        {
            var (addMethod, label) = field switch
            {
                "name" => (".AddField(x => x.Name, f => f", "Name"),
                "email" => (".AddField(x => x.Email, f => f", "Email"),
                "phone" => (".AddField(x => x.Phone, f => f", "Phone"),
                "message" => (".AddField(x => x.Message, f => f", "Message"),
                "password" => (".AddField(x => x.Password, f => f", "Password"),
                "rememberMe" => (".AddField(x => x.RememberMe, f => f", "Remember me"),
                "quantity" => (".AddField(x => x.Quantity, f => f", "Quantity"),
                "notes" => (".AddField(x => x.Notes, f => f", "Notes"),
                _ => ("", "")
            };

            if (!string.IsNullOrEmpty(addMethod))
            {
                var config = $"        .WithLabel(\"{label}\")";
                if (_showValidation && field is "name" or "email" or "password" or "quantity")
                {
                    config += $"\n        .Required(\"{label} is required\")";
                }

                lines.Add($"    {addMethod}\n{config})");
            }
        }

        lines.Add("    .Build();");

        return string.Join("\n", lines);
    }

    private async Task CopyCode()
    {
        var code = GenerateCode();
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", code);
        _copied = true;
        StateHasChanged();

        await Task.Delay(2000);
        _copied = false;
        StateHasChanged();
    }

    private void HandleValidSubmit(PlaygroundModel model)
    {
        _submitted = true;
        Console.WriteLine($"Form submitted: Name={model.Name}, Email={model.Email}");
    }

    public class PlaygroundModel
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Message { get; set; } = "";
        public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; } = "";
    }
}
