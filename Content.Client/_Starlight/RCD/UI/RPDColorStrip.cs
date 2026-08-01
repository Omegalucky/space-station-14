using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Starlight.RCD.UI;

/// <summary>
/// Horizontal strip of pipe colour swatches, shown along the bottom of the RPD's radial menu.
/// </summary>
/// <remarks>
/// The swatches are deliberately not radial menu buttons: picking a colour leaves the menu open,
/// so a colour and a recipe can both be chosen in one visit.
/// </remarks>
public sealed partial class RPDColorStrip : PanelContainer
{
    /// <summary>
    /// Prefix shared with the spray painter, so a colour named in one tool is named in the other.
    /// Colours with no entry fall back to their palette key.
    /// </summary>
    private const string ColorLocKeyPrefix = "pipe-painter-color-";

    private const int SwatchSize = 22;
    private const int SwatchSeparation = 2;
    private const int BorderWidth = 2;

    private static readonly Color PanelColor = new(70, 73, 102, 128);
    private static readonly Color SelectedBorderColor = new(173, 216, 230);
    private static readonly Color UnpaintedSwatchColor = new(70, 73, 102, 200);

    [Dependency] private ILocalizationManager _loc = default!;

    private readonly BoxContainer _swatches;

    /// <summary> Palette key of each swatch, against the button showing it. </summary>
    private readonly List<(string? Key, PanelContainer Fill)> _entries = new();

    /// <summary> Raised with the palette key picked, or null for the "leave it unpainted" swatch. </summary>
    public event Action<string?>? ColorSelected;

    public RPDColorStrip()
    {
        IoCManager.InjectDependencies(this);

        HorizontalAlignment = HAlignment.Center;
        VerticalAlignment = VAlignment.Bottom;

        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = PanelColor,
            ContentMarginLeftOverride = 4,
            ContentMarginRightOverride = 4,
            ContentMarginTopOverride = 4,
            ContentMarginBottomOverride = 4,
        };

        _swatches = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = SwatchSeparation,
        };

        AddChild(_swatches);
    }

    /// <summary>
    /// Rebuilds the strip from a device's palette and highlights the colour it currently has set.
    /// </summary>
    public void Populate(IReadOnlyDictionary<string, Color> palette, string? selected)
    {
        _swatches.RemoveAllChildren();
        _entries.Clear();

        // Leading swatch opts out, leaving what gets built in its prototype's own colour.
        AddSwatch(null, UnpaintedSwatchColor, _loc.GetString("rpd-color-unpainted"));

        foreach (var (key, color) in palette)
            AddSwatch(key, color, GetColorName(key));

        SetSelected(selected);
    }

    /// <summary>
    /// Moves the selection highlight, without raising <see cref="ColorSelected"/>.
    /// </summary>
    public void SetSelected(string? selected)
    {
        foreach (var (key, fill) in _entries)
        {
            if (fill.PanelOverride is not StyleBoxFlat style)
                continue;

            // Borders are drawn on every swatch; only the selected one gets a visible colour.
            style.BorderColor = key == selected ? SelectedBorderColor : Color.Transparent;
        }
    }

    private void AddSwatch(string? key, Color color, string name)
    {
        var fill = new PanelContainer
        {
            MinSize = new Vector2(SwatchSize, SwatchSize),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = color,
                BorderColor = Color.Transparent,
                BorderThickness = new Thickness(BorderWidth),
            },
        };

        var button = new Button
        {
            ToolTip = name,
            // The fill panel supplies the whole look, so the button itself draws nothing.
            StyleClasses = { "ButtonSquare" },
            ModulateSelfOverride = Color.Transparent,
        };

        button.AddChild(fill);
        button.OnPressed += _ =>
        {
            SetSelected(key);
            ColorSelected?.Invoke(key);
        };

        _swatches.AddChild(button);
        _entries.Add((key, fill));
    }

    private string GetColorName(string key)
    {
        // Same fallback the spray painter uses: an unlocalised colour shows its raw palette key
        // rather than a missing-string error, so adding a colour in YAML alone still works.
        if (!_loc.TryGetString(ColorLocKeyPrefix + key, out var name))
            name = key;

        return name;
    }
}
