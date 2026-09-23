using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Layout;

/// <summary>
/// The demo site's MudBlazor theme.
/// </summary>
/// <remarks>
/// Light only, on purpose. The site's own chrome is a dark stage drawn by
/// <c>wwwroot/css/tokens.css</c>; MudBlazor renders the forms, and they sit on light panels so a
/// visitor sees them the way they would look in their own app. Primary is MudBlazor's default
/// violet (<c>#594AE2</c>), which the stage's accent (<c>--fc-violet</c>) matches. MudBlazor
/// components that land on the dark stage read a scoped dark palette instead (<c>.fc-mud-dark</c>).
/// </remarks>
public static class FormCraftTheme
{
    private static readonly string[] BodyFont =
        ["Onest", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"];

    private static readonly string[] DisplayFont =
        ["Sora", "Onest", "Helvetica Neue", "sans-serif"];

    public static MudTheme Build() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#594AE2",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#2C1866",
            Tertiary = "#14A06B",

            Success = "#14A06B",
            Info = "#594AE2",
            Warning = "#B4690E",
            Error = "#C0304A",

            Background = "#F5F4F8",
            BackgroundGray = "#EDEBF3",
            Surface = "#FFFFFF",

            AppbarBackground = "#16141F",
            AppbarText = "#F5F4F8",

            DrawerBackground = "#FFFFFF",
            DrawerText = "#2A2637",
            DrawerIcon = "#6A6580",

            TextPrimary = "#16141F",
            TextSecondary = "#6A6580",
            TextDisabled = "#A5A0B8",

            ActionDefault = "#6A6580",
            Divider = "#E1DFEA",
            DividerLight = "#EDEBF3",
            LinesDefault = "#E1DFEA",
            LinesInputs = "#CFCBDF",
            TableLines = "#E1DFEA"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
            DrawerWidthLeft = "290px",
            AppbarHeight = "64px"
        },
        Typography = new Typography
        {
            // Body copy and every MudBlazor control: Onest.
            Default = new DefaultTypography
            {
                FontFamily = BodyFont,
                FontSize = "0.9375rem",
                FontWeight = "400",
                LineHeight = "1.55",
                LetterSpacing = "0"
            },
            // Headings step up to the display face, Sora.
            H1 = new H1Typography
            {
                FontFamily = DisplayFont,
                FontSize = "3.4rem",
                FontWeight = "800",
                LineHeight = "1.02",
                LetterSpacing = "-0.03em"
            },
            H2 = new H2Typography
            {
                FontFamily = DisplayFont,
                FontSize = "2.6rem",
                FontWeight = "800",
                LineHeight = "1.06",
                LetterSpacing = "-0.028em"
            },
            H3 = new H3Typography
            {
                FontFamily = DisplayFont,
                FontSize = "2.1rem",
                FontWeight = "700",
                LineHeight = "1.1",
                LetterSpacing = "-0.024em"
            },
            H4 = new H4Typography
            {
                FontFamily = DisplayFont,
                FontSize = "1.65rem",
                FontWeight = "700",
                LineHeight = "1.15",
                LetterSpacing = "-0.02em"
            },
            H5 = new H5Typography
            {
                FontFamily = DisplayFont,
                FontSize = "1.3rem",
                FontWeight = "700",
                LineHeight = "1.2",
                LetterSpacing = "-0.015em"
            },
            H6 = new H6Typography
            {
                FontFamily = DisplayFont,
                FontSize = "1.075rem",
                FontWeight = "600",
                LineHeight = "1.3",
                LetterSpacing = "-0.01em"
            },
            Subtitle1 = new Subtitle1Typography
            {
                FontFamily = BodyFont,
                FontSize = "1rem",
                FontWeight = "500",
                LineHeight = "1.5"
            },
            Subtitle2 = new Subtitle2Typography
            {
                FontFamily = BodyFont,
                FontSize = "0.875rem",
                FontWeight = "600",
                LineHeight = "1.45"
            },
            Body1 = new Body1Typography
            {
                FontFamily = BodyFont,
                FontSize = "0.9375rem",
                FontWeight = "400",
                LineHeight = "1.6"
            },
            Body2 = new Body2Typography
            {
                FontFamily = BodyFont,
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.55"
            },
            // Buttons keep sentence case: a control is named for what it does, and
            // SHOUTING CAPS makes long labels ("Read the documentation") hard to scan.
            Button = new ButtonTypography
            {
                FontFamily = BodyFont,
                FontSize = "0.875rem",
                FontWeight = "600",
                LineHeight = "1.75",
                LetterSpacing = "0.01em",
                TextTransform = "none"
            },
            Caption = new CaptionTypography
            {
                FontFamily = BodyFont,
                FontSize = "0.78rem",
                FontWeight = "400",
                LineHeight = "1.4"
            },
            // Overline is the eyebrow role, and every eyebrow on this site is mono.
            Overline = new OverlineTypography
            {
                FontFamily = ["IBM Plex Mono", "ui-monospace", "monospace"],
                FontSize = "0.72rem",
                FontWeight = "500",
                LineHeight = "1.6",
                LetterSpacing = "0.16em",
                TextTransform = "uppercase"
            }
        }
    };
}
