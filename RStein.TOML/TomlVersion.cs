namespace RStein.TOML
{
  /// <summary>
  /// Enumeration of supported TOML specification versions.
  /// </summary>
  /// <remarks>
  /// <see cref="TomlVersion"/> specifies which TOML specification version should be used for parsing and serialization.
  /// Pass a version to <see cref="TomlSettings"/> to configure version-specific behavior.
  /// </remarks>
  public enum TomlVersion
  {
    /// <summary>
    /// TOML version not specified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// TOML 1.0.0 specification.
    /// </summary>
    Toml10,

    /// <summary>
    /// TOML 1.1.0 specification (default).
    /// </summary>
    Toml11
  }
}