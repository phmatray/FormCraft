using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class ErrorHandlingDemo
{
    // Validation error scenario
    private ValidationErrorModel _validationModel = new();
    private IFormConfiguration<ValidationErrorModel>? _validationConfig;
    private bool _validationSubmitted;

    // Async validation scenario
    private AsyncValidationModel _asyncModel = new();
    private IFormConfiguration<AsyncValidationModel>? _asyncConfig;
    private bool _asyncSubmitted;

    // Submission error scenario
    private SubmissionModel _submissionModel = new();
    private IFormConfiguration<SubmissionModel>? _submissionConfig;
    private bool _simulateNetworkError;
    private bool _isSubmitting;
    private string? _submissionError;
    private bool _submissionSuccess;

    // Recovery pattern scenario
    private RecoveryModel _recoveryModel = new();
    private IFormConfiguration<RecoveryModel>? _recoveryConfig;
    private bool _isRecovering;
    private int _retryCount;
    private const int _maxRetries = 3;
    private bool _recoverySuccess;

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "error-handling",
        Title = "Error Handling Patterns",
        Description = "Learn how to handle validation errors, async failures, and form submission errors gracefully.",
        Icon = Icons.Material.Filled.ErrorOutline,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.ReportProblem, Color = Color.Error, Text = "Field-level validation errors" },
            new() { Icon = Icons.Material.Filled.CloudOff, Color = Color.Warning, Text = "Async validation failure handling" },
            new() { Icon = Icons.Material.Filled.WifiOff, Color = Color.Error, Text = "Network error recovery" },
            new() { Icon = Icons.Material.Filled.Refresh, Color = Color.Info, Text = "Auto-retry patterns" },
            new() { Icon = Icons.Material.Filled.Save, Color = Color.Success, Text = "State persistence on failure" },
            new() { Icon = Icons.Material.Filled.Feedback, Color = Color.Primary, Text = "User-friendly error messages" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "WithAsyncValidator", Usage = "Handle server-side validation errors", Example = ".WithAsyncValidator(async v => await CheckAvailable(v), \"Already taken\")" },
            new() { Feature = "Try/Catch Submit", Usage = "Wrap submission in try/catch", Example = "try { await Submit(); } catch { ShowError(); }" },
            new() { Feature = "IsSubmitting", Usage = "Disable form during submission", Example = "IsSubmitting=\"@_isSubmitting\"" },
            new() { Feature = "Error State", Usage = "Track and display errors", Example = "@if (_error != null) { <Alert>@_error</Alert> }" },
            new() { Feature = "Retry Pattern", Usage = "Implement retry logic", Example = "while (retryCount < maxRetries) { try {...} catch {...} }" }
        ],
        CodeExamples =
        [
            new() { Title = "Error Handling Patterns", Language = "csharp", CodeProvider = GetErrorHandlingCodeStatic }
        ],
        WhenToUse = "Implement proper error handling for any production form. This includes validation feedback for user input errors, graceful handling of network failures during async validation or submission, retry mechanisms for transient failures, and clear user feedback at all stages. Error handling improves user experience and reduces support requests.",
        CommonPitfalls =
        [
            "Not showing loading state during async operations - users may click multiple times",
            "Generic error messages that don't help users fix the problem",
            "Not preserving form state when submission fails - users lose their input",
            "Missing retry logic for transient network failures",
            "Not disabling the submit button during submission",
            "Failing silently without notifying the user of the error"
        ],
        RelatedDemoIds = ["fluent-validation-demo", "async-value-provider", "complex-dependencies"]
    };

    private List<GuidelineItem> _apiGuidelineTableItems => Documentation.ApiGuidelines
        .Select(g => new GuidelineItem { Feature = g.Feature, Usage = g.Usage, Example = g.Example })
        .ToList();

    protected override void OnInitialized()
    {
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);

        _validationConfig = CreateValidationConfig();
        _asyncConfig = CreateAsyncConfig();
        _submissionConfig = CreateSubmissionConfig();
        _recoveryConfig = CreateRecoveryConfig();
    }

    private static IFormConfiguration<ValidationErrorModel> CreateValidationConfig()
    {
        return FormBuilder<ValidationErrorModel>
            .Create()
            .AddField(x => x.Username, field => field
                .WithLabel("Username")
                .WithPlaceholder("Choose a username")
                .Required("Username is required")
                .WithMinLength(3, "Username must be at least 3 characters")
                .WithMaxLength(20, "Username cannot exceed 20 characters")
                .WithValidator(v => !v.Contains(" "), "Username cannot contain spaces"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithPlaceholder("your@email.com")
                .Required("Email is required")
                .WithEmailValidation("Please enter a valid email address"))
            .AddField(x => x.Age, field => field
                .WithLabel("Age")
                .Required("Age is required")
                .WithRange(18, 120, "Age must be between 18 and 120"))
            .Build();
    }

    private static IFormConfiguration<AsyncValidationModel> CreateAsyncConfig()
    {
        return FormBuilder<AsyncValidationModel>
            .Create()
            .AddField(x => x.Username, field => field
                .WithLabel("Username")
                .WithPlaceholder("Choose a username")
                .Required("Username is required")
                .WithAsyncValidator(async username =>
                {
                    await Task.Delay(500); // Simulate API call
                    var takenUsernames = new[] { "admin", "root", "user", "test" };
                    return !takenUsernames.Contains(username.ToLower());
                }, "This username is already taken"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithPlaceholder("your@email.com")
                .Required("Email is required")
                .WithEmailValidation()
                .WithAsyncValidator(async email =>
                {
                    await Task.Delay(500); // Simulate API call
                    return !email.Equals("taken@email.com", StringComparison.OrdinalIgnoreCase);
                }, "This email is already registered"))
            .Build();
    }

    private static IFormConfiguration<SubmissionModel> CreateSubmissionConfig()
    {
        return FormBuilder<SubmissionModel>
            .Create()
            .AddField(x => x.ProductName, field => field
                .WithLabel("Product Name")
                .WithPlaceholder("Enter product name")
                .Required("Product name is required"))
            .AddField(x => x.Quantity, field => field
                .WithLabel("Quantity")
                .Required("Quantity is required")
                .WithRange(1, 100, "Quantity must be between 1 and 100"))
            .Build();
    }

    private static IFormConfiguration<RecoveryModel> CreateRecoveryConfig()
    {
        return FormBuilder<RecoveryModel>
            .Create()
            .AddField(x => x.Data, field => field
                .WithLabel("Data to Submit")
                .WithPlaceholder("Enter any data")
                .Required("Data is required"))
            .Build();
    }

    private async Task HandleValidationSubmit(ValidationErrorModel model)
    {
        await Task.Delay(500);
        _validationSubmitted = true;
        StateHasChanged();
    }

    private async Task HandleAsyncSubmit(AsyncValidationModel model)
    {
        await Task.Delay(500);
        _asyncSubmitted = true;
        StateHasChanged();
    }

    private async Task HandleSubmissionWithError(SubmissionModel model)
    {
        _isSubmitting = true;
        _submissionError = null;
        _submissionSuccess = false;
        StateHasChanged();

        try
        {
            await Task.Delay(1500); // Simulate API call

            if (_simulateNetworkError)
            {
                throw new HttpRequestException("Network error: Unable to reach the server. Please check your connection.");
            }

            _submissionSuccess = true;
        }
        catch (Exception ex)
        {
            _submissionError = ex.Message;
        }
        finally
        {
            _isSubmitting = false;
            StateHasChanged();
        }
    }

    private void RetrySubmission()
    {
        _submissionError = null;
        _simulateNetworkError = false;
        StateHasChanged();
    }

    private async Task HandleRecoverySubmit(RecoveryModel model)
    {
        _isRecovering = true;
        _recoverySuccess = false;
        _retryCount = 0;
        StateHasChanged();

        while (_retryCount < _maxRetries)
        {
            _retryCount++;
            StateHasChanged();

            try
            {
                await Task.Delay(1000); // Simulate API call

                // Fail first 2 attempts
                if (_retryCount < 3)
                {
                    throw new Exception("Transient failure");
                }

                // Success on 3rd attempt
                _recoverySuccess = true;
                break;
            }
            catch
            {
                if (_retryCount >= _maxRetries)
                {
                    // All retries exhausted - in real app, show final error
                }
                await Task.Delay(500); // Wait before retry
            }
        }

        _isRecovering = false;
        StateHasChanged();
    }

    private void ResetRecovery()
    {
        _recoveryModel = new RecoveryModel();
        _retryCount = 0;
        _recoverySuccess = false;
        StateHasChanged();
    }

    private string GetErrorHandlingCode() => GetErrorHandlingCodeStatic();

    private static string GetErrorHandlingCodeStatic() => """
        // 1. Handle async validation errors
        .AddField(x => x.Username, field => field
            .WithAsyncValidator(async username =>
            {
                try
                {
                    var isAvailable = await _api.CheckUsernameAsync(username);
                    return isAvailable;
                }
                catch (HttpRequestException)
                {
                    // Validation service unavailable - allow submission
                    // and validate on server
                    return true;
                }
            }, "This username is already taken"))

        // 2. Handle form submission errors
        private async Task HandleSubmit(MyModel model)
        {
            _isSubmitting = true;
            _error = null;
            StateHasChanged();

            try
            {
                await _api.SubmitAsync(model);
                _success = true;
            }
            catch (HttpRequestException ex)
            {
                _error = "Network error. Please try again.";
            }
            catch (ValidationException ex)
            {
                _error = ex.Message;
            }
            catch (Exception ex)
            {
                _error = "An unexpected error occurred.";
                _logger.LogError(ex, "Form submission failed");
            }
            finally
            {
                _isSubmitting = false;
                StateHasChanged();
            }
        }

        // 3. Implement retry pattern
        private async Task SubmitWithRetry(MyModel model, int maxRetries = 3)
        {
            int retryCount = 0;
            Exception? lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    await _api.SubmitAsync(model);
                    return; // Success
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    retryCount++;

                    if (retryCount < maxRetries)
                    {
                        // Exponential backoff
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                    }
                }
            }

            throw new Exception($"Failed after {maxRetries} attempts", lastException);
        }

        // 4. Display errors in UI
        <FormCraftComponent TModel="MyModel"
                           Model="@_model"
                           Configuration="@_config"
                           OnValidSubmit="@HandleSubmit"
                           IsSubmitting="@_isSubmitting" />

        @if (_error != null)
        {
            <MudAlert Severity="Severity.Error" ShowCloseIcon="true"
                      CloseIconClicked="() => _error = null">
                @_error
            </MudAlert>
            <MudButton OnClick="HandleSubmit">Retry</MudButton>
        }
        """;

    // Model classes
    public class ValidationErrorModel
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
    }

    public class AsyncValidationModel
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class SubmissionModel
    {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; } = 1;
    }

    public class RecoveryModel
    {
        public string Data { get; set; } = "";
    }
}
