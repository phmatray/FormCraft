using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Shared.Navigation;

public partial class DemoPrevNextNavigation
{
    [Parameter, EditorRequired]
    public string DemoId { get; set; } = "";

    private DemoMetadata? _current;
    private DemoMetadata? _previous;
    private DemoMetadata? _next;
    private bool _isLevelTransition;
    private (string Name, string Icon, Color Color)? _nextLevelInfo;

    protected override void OnParametersSet()
    {
        _current = null;
        _previous = null;
        _next = null;
        _isLevelTransition = false;
        _nextLevelInfo = null;

        if (string.IsNullOrEmpty(DemoId))
            return;

        _current = DemoRegistry.GetDemo(DemoId);
        if (_current == null)
            return;

        // Use learning path navigation for form examples
        if (_current.Category == "form-examples")
        {
            (_previous, _next) = DemoRegistry.GetLearningPathAdjacentDemos(DemoId);

            // Check if we're transitioning to a new level
            if (_next != null && !string.IsNullOrEmpty(_next.Level) && _next.Level != _current.Level)
            {
                _isLevelTransition = true;
                _nextLevelInfo = DemoRegistry.GetLevelInfo(_next.Level);
            }
        }
        else
        {
            // Use category-based navigation for documentation
            (_previous, _next) = DemoRegistry.GetAdjacentDemos(DemoId);
        }
    }
}
