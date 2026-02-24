namespace RStein.TOML
{
  /// <summary>
  /// Configuration settings for TOML serialization and deserialization operations.
  /// </summary>
  /// <remarks>
  /// <see cref="TomlSettings"/> allows you to configure TOML version compliance and other parsing/serialization behaviors.
  /// Use the static <see cref="Default"/> property to get settings with TOML 1.1.0 specification.
  /// Create a new instance with a specific <see cref="TomlVersion"/> for version-specific behavior.
  /// </remarks>
  /// <example>
  /// <code>
  /// // Use default settings (TOML 1.1.0)
  /// var defaultSettings = TomlSettings.Default;
  /// 
  /// // Create settings for TOML 1.0.0
  /// var toml10Settings = new TomlSettings(TomlVersion.Toml10);
  /// 
  /// // Use settings in deserialization
  /// var table = await TomlSerializer.DeserializeAsync(tomlContent, toml10Settings);
  /// </code>
  /// </example>
  public class TomlSettings
  {
    /// <summary>
    /// Gets the default TOML settings using TOML 1.1.0 specification.
    /// </summary>
    public static readonly TomlSettings Default = new TomlSettings(TomlVersion.Toml11);

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSettings"/> class with the specified TOML version.
    /// </summary>
    /// <param name="tomlVersion">The TOML specification version to use.</param>
    public TomlSettings(TomlVersion tomlVersion)
    {
      TomlVersion = tomlVersion;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSettings"/> class with default TOML 1.1.0 settings.
    /// </summary>
    public TomlSettings()
    {

    }

    /// <summary>
    /// Gets or sets the TOML specification version for parsing and serialization.
    /// </summary>
    /// <remarks>
    /// The default is <see cref="TomlVersion.Toml11"/>.
    /// </remarks>
    public TomlVersion TomlVersion
    {
      get;
      set;
    } = TomlVersion.Toml11;
  }
}