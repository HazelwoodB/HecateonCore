namespace Hecateon.Client.Services.Foundation;

/// <summary>
/// Retro-Futuristic Theme Service - 1950s-inspired design system
/// Features: Dynamic theming, vintage aesthetics, customizable palettes
/// </summary>
public class RetroThemeService
{
    private readonly StorageService _storage;
    private const string ThemeKey = "aria_theme_settings";
    private ThemeSettings _currentTheme;

    public RetroThemeService(StorageService storage)
    {
        _storage = storage;
        _currentTheme = RetroThemes["Classic1950s"]; // Default theme
    }

    public event EventHandler<ThemeSettings>? ThemeChanged;

    /// <summary>
    /// Gets the current theme settings
    /// </summary>
    public async Task<ThemeSettings> GetCurrentThemeAsync()
    {
        var saved = await _storage.LoadAsync<ThemeSettings>(ThemeKey);
        if (saved != null)
        {
            _currentTheme = saved;
        }

        return _currentTheme;
    }

    /// <summary>
    /// Sets a new theme
    /// </summary>
    public async Task SetThemeAsync(string themeName)
    {
        if (RetroThemes.TryGetValue(themeName, out var theme))
        {
            _currentTheme = theme;
            await _storage.SaveAsync(ThemeKey, theme);
            ThemeChanged?.Invoke(this, theme);
        }
    }

    /// <summary>
    /// Creates custom theme
    /// </summary>
    public async Task SetCustomThemeAsync(ThemeSettings customTheme)
    {
        _currentTheme = customTheme;
        await _storage.SaveAsync(ThemeKey, customTheme);
        ThemeChanged?.Invoke(this, customTheme);
    }

    /// <summary>
    /// Gets all available preset themes
    /// </summary>
    public Dictionary<string, ThemeSettings> GetAvailableThemes()
    {
        return RetroThemes;
    }

    /// <summary>
    /// Generates CSS variables for current theme
    /// </summary>
    public string GenerateThemeCSS(ThemeSettings? theme = null)
    {
        theme ??= _currentTheme;

        return $@":root {{
    /* Primary Colors */
    --aria-primary: {theme.PrimaryColor};
    --aria-secondary: {theme.SecondaryColor};
    --aria-accent: {theme.AccentColor};
    
    /* Background Colors */
    --aria-bg-primary: {theme.BackgroundPrimary};
    --aria-bg-secondary: {theme.BackgroundSecondary};
    --aria-bg-card: {theme.CardBackground};
    
    /* Text Colors */
    --aria-text-primary: {theme.TextPrimary};
    --aria-text-secondary: {theme.TextSecondary};
    --aria-text-muted: {theme.TextMuted};
    
    /* UI Elements */
    --aria-border: {theme.BorderColor};
    --aria-shadow: {theme.ShadowColor};
    --aria-glow: {theme.GlowColor};
    
    /* Typography */
    --aria-font-primary: {theme.FontPrimary};
    --aria-font-display: {theme.FontDisplay};
    --aria-font-mono: {theme.FontMono};
    
    /* Effects */
    --aria-scan-line-color: {theme.ScanLineColor};
    --aria-scan-line-opacity: {theme.ScanLineOpacity};
    --aria-crt-effect: {theme.EnableCRTEffect};
}}";
    }

    #region Retro Theme Presets

    private static readonly Dictionary<string, ThemeSettings> RetroThemes = new()
    {
        ["Classic1950s"] = new ThemeSettings
        {
            Name = "Classic 1950s",
            Description = "Warm amber on dark, like vintage CRT monitors",
            
            PrimaryColor = "#ff9500",      // Warm amber
            SecondaryColor = "#ffb84d",    // Light amber
            AccentColor = "#ff6b00",       // Deep orange
            
            BackgroundPrimary = "#0a0a0f", // Near black
            BackgroundSecondary = "#1a1a2e", // Dark blue-grey
            CardBackground = "#16213e",    // Card background
            
            TextPrimary = "#fff4e6",       // Warm white
            TextSecondary = "#ffb84d",     // Amber
            TextMuted = "#7f8c8d",         // Muted grey
            
            BorderColor = "#ff9500",       // Amber border
            ShadowColor = "rgba(255, 149, 0, 0.3)",
            GlowColor = "rgba(255, 149, 0, 0.6)",
            
            FontPrimary = "'Courier New', 'Courier', monospace",
            FontDisplay = "'Orbitron', 'Arial Narrow', sans-serif",
            FontMono = "'Courier New', 'Courier', monospace",
            
            ScanLineColor = "#ff9500",
            ScanLineOpacity = "0.1",
            EnableCRTEffect = true
        },

        // === WANDAVISION THEMES === //

        ["WandaVision1950s"] = new ThemeSettings
        {
            Name = "WandaVision: 1950s Sitcom",
            Description = "Black & white with crimson accents - 'Filmed before a live studio audience'",
            
            PrimaryColor = "#dc143c",      // Crimson red (Wanda's magic)
            SecondaryColor = "#ffffff",    // Pure white
            AccentColor = "#8b0000",       // Dark red
            
            BackgroundPrimary = "#0a0a0a", // Deep black
            BackgroundSecondary = "#1a1a1a", // Charcoal
            CardBackground = "#2a2a2a",    // Dark grey
            
            TextPrimary = "#f5f5f5",       // Off-white
            TextSecondary = "#dc143c",     // Crimson
            TextMuted = "#808080",         // Grey
            
            BorderColor = "#dc143c",
            ShadowColor = "rgba(220, 20, 60, 0.4)",
            GlowColor = "rgba(220, 20, 60, 0.8)",
            
            FontPrimary = "'Courier New', monospace",
            FontDisplay = "'Times New Roman', serif",
            FontMono = "'Courier New', monospace",
            
            ScanLineColor = "#ffffff",
            ScanLineOpacity = "0.05",
            EnableCRTEffect = true
        },

        ["WandaVision1960s"] = new ThemeSettings
        {
            Name = "WandaVision: 1960s Color TV",
            Description = "Vibrant colors burst onto screen - Bewitched meets reality",
            
            PrimaryColor = "#ff1744",      // Bright red (Wanda's power)
            SecondaryColor = "#ff80ab",    // Pink
            AccentColor = "#c51162",       // Deep pink
            
            BackgroundPrimary = "#0d1117", // Dark blue-black
            BackgroundSecondary = "#1c2128", // Dark grey-blue
            CardBackground = "#2d333b",    // Card grey
            
            TextPrimary = "#ffffff",
            TextSecondary = "#ff80ab",
            TextMuted = "#8b949e",
            
            BorderColor = "#ff1744",
            ShadowColor = "rgba(255, 23, 68, 0.4)",
            GlowColor = "rgba(255, 128, 171, 0.6)",
            
            FontPrimary = "'Comic Sans MS', 'Arial Rounded', sans-serif",
            FontDisplay = "'Impact', 'Arial Black', sans-serif",
            FontMono = "'Consolas', monospace",
            
            ScanLineColor = "#ff80ab",
            ScanLineOpacity = "0.08",
            EnableCRTEffect = true
        },

        ["WandaVision1970s"] = new ThemeSettings
        {
            Name = "WandaVision: 1970s Groovy",
            Description = "Warm earth tones with psychedelic vibes - Far out!",
            
            PrimaryColor = "#e91e63",      // Hot pink (Wanda's magic)
            SecondaryColor = "#ffa726",    // Orange
            AccentColor = "#7e57c2",       // Purple
            
            BackgroundPrimary = "#1a0f0a", // Warm dark brown
            BackgroundSecondary = "#2d1f1a", // Brown
            CardBackground = "#3d2f2a",    // Light brown
            
            TextPrimary = "#fff8e7",       // Warm white
            TextSecondary = "#ffa726",     // Orange
            TextMuted = "#8d6e63",         // Muted brown
            
            BorderColor = "#e91e63",
            ShadowColor = "rgba(233, 30, 99, 0.4)",
            GlowColor = "rgba(255, 167, 38, 0.6)",
            
            FontPrimary = "'Brush Script MT', cursive",
            FontDisplay = "'Cooper Black', 'Arial Black', sans-serif",
            FontMono = "'Courier New', monospace",
            
            ScanLineColor = "#ffa726",
            ScanLineOpacity = "0.06",
            EnableCRTEffect = true
        },

        ["WandaVision1980s"] = new ThemeSettings
        {
            Name = "WandaVision: 1980s Neon",
            Description = "Electric neon meets reality glitches - Totally radical!",
            
            PrimaryColor = "#f50057",      // Neon pink (Wanda)
            SecondaryColor = "#00e5ff",    // Cyan
            AccentColor = "#d500f9",       // Purple
            
            BackgroundPrimary = "#0a0014", // Deep purple-black
            BackgroundSecondary = "#1a0a2e", // Dark purple
            CardBackground = "#2d1b4e",    // Purple card
            
            TextPrimary = "#ffffff",
            TextSecondary = "#00e5ff",
            TextMuted = "#9c27b0",
            
            BorderColor = "#f50057",
            ShadowColor = "rgba(245, 0, 87, 0.5)",
            GlowColor = "rgba(0, 229, 255, 0.7)",
            
            FontPrimary = "'Arial', sans-serif",
            FontDisplay = "'Impact', 'Arial Black', sans-serif",
            FontMono = "'Consolas', 'Courier New', monospace",
            
            ScanLineColor = "#00e5ff",
            ScanLineOpacity = "0.1",
            EnableCRTEffect = true
        },

        ["WandaVisionHex"] = new ThemeSettings
        {
            Name = "WandaVision: The Hex",
            Description = "Reality itself bends - Scarlet magic and hexagonal barriers",
            
            PrimaryColor = "#e91e63",      // Scarlet (Wanda's true power)
            SecondaryColor = "#ff6090",    // Light scarlet
            AccentColor = "#880e4f",       // Dark scarlet
            
            BackgroundPrimary = "#0d0208", // Near black
            BackgroundSecondary = "#1a0a14", // Dark magenta
            CardBackground = "#2d1420",    // Magenta card
            
            TextPrimary = "#fff0f5",       // Lavender white
            TextSecondary = "#ff6090",     // Light scarlet
            TextMuted = "#8e4585",         // Purple-pink
            
            BorderColor = "#e91e63",
            ShadowColor = "rgba(233, 30, 99, 0.6)",
            GlowColor = "rgba(255, 96, 144, 0.8)",
            
            FontPrimary = "'Segoe UI', 'Arial', sans-serif",
            FontDisplay = "'Montserrat', 'Arial', sans-serif",
            FontMono = "'Fira Code', 'Consolas', monospace",
            
            ScanLineColor = "#e91e63",
            ScanLineOpacity = "0.12",
            EnableCRTEffect = false // Modern look, no CRT
        },

        // === ORIGINAL THEMES === //

        ["GreenScreen"] = new ThemeSettings
        {
            Name = "Green Screen",
            Description = "Classic green phosphor terminal",
            
            PrimaryColor = "#00ff41",
            SecondaryColor = "#39ff14",
            AccentColor = "#00cc33",
            
            BackgroundPrimary = "#000000",
            BackgroundSecondary = "#001a00",
            CardBackground = "#002200",
            
            TextPrimary = "#00ff41",
            TextSecondary = "#39ff14",
            TextMuted = "#006622",
            
            BorderColor = "#00ff41",
            ShadowColor = "rgba(0, 255, 65, 0.3)",
            GlowColor = "rgba(0, 255, 65, 0.6)",
            
            FontPrimary = "'Courier New', monospace",
            FontDisplay = "'VT323', 'Courier New', monospace",
            FontMono = "'Courier New', monospace",
            
            ScanLineColor = "#00ff41",
            ScanLineOpacity = "0.15",
            EnableCRTEffect = true
        },

        ["CyberPunk"] = new ThemeSettings
        {
            Name = "Cyber Punk",
            Description = "Neon pink and cyan retro-futurism",
            
            PrimaryColor = "#ff006e",
            SecondaryColor = "#00f5ff",
            AccentColor = "#8338ec",
            
            BackgroundPrimary = "#0a0a0f",
            BackgroundSecondary = "#1a0a1e",
            CardBackground = "#2e1a3e",
            
            TextPrimary = "#ffffff",
            TextSecondary = "#00f5ff",
            TextMuted = "#8338ec",
            
            BorderColor = "#ff006e",
            ShadowColor = "rgba(255, 0, 110, 0.4)",
            GlowColor = "rgba(255, 0, 110, 0.7)",
            
            FontPrimary = "'Rajdhani', 'Arial Narrow', sans-serif",
            FontDisplay = "'Orbitron', sans-serif",
            FontMono = "'Fira Code', 'Courier New', monospace",
            
            ScanLineColor = "#ff006e",
            ScanLineOpacity = "0.08",
            EnableCRTEffect = false
        }
    };

    #endregion
}

public class ThemeSettings
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Colors
    public string PrimaryColor { get; set; } = "#ff9500";
    public string SecondaryColor { get; set; } = "#ffb84d";
    public string AccentColor { get; set; } = "#ff6b00";
    
    public string BackgroundPrimary { get; set; } = "#0a0a0f";
    public string BackgroundSecondary { get; set; } = "#1a1a2e";
    public string CardBackground { get; set; } = "#16213e";
    
    public string TextPrimary { get; set; } = "#fff4e6";
    public string TextSecondary { get; set; } = "#ffb84d";
    public string TextMuted { get; set; } = "#7f8c8d";
    
    public string BorderColor { get; set; } = "#ff9500";
    public string ShadowColor { get; set; } = "rgba(255, 149, 0, 0.3)";
    public string GlowColor { get; set; } = "rgba(255, 149, 0, 0.6)";
    
    // Typography
    public string FontPrimary { get; set; } = "'Courier New', monospace";
    public string FontDisplay { get; set; } = "'Orbitron', sans-serif";
    public string FontMono { get; set; } = "'Courier New', monospace";
    
    // Effects
    public string ScanLineColor { get; set; } = "#ff9500";
    public string ScanLineOpacity { get; set; } = "0.1";
    public bool EnableCRTEffect { get; set; } = true;
}
