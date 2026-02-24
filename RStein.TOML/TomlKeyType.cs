namespace RStein.TOML
{
  /// <summary>
  /// Enumeration of TOML key formats.
  /// </summary>
  /// <remarks>
  /// <see cref="TomlKeyType"/> specifies how a key is formatted in the TOML document.
  /// Keys can be unquoted (for simple identifiers) or quoted using different string syntaxes.
  /// This distinction is important for accurate serialization.
  /// </remarks>
  public enum TomlKeyType
  {
    /// <summary>
    /// Key type not yet determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// Unquoted key (for simple alphanumeric identifiers and underscores).
    /// </summary>
    SimpleUnquoted = TomlDataType.RawString,

    /// <summary>
    /// Key quoted with basic string syntax (allows escape sequences).
    /// </summary>
    SimpleQuotedBasicString = TomlDataType.BasicString,

    /// <summary>
    /// Key quoted with literal string syntax (no escape sequences).
    /// </summary>
    SimpleQuotedLiteralString = TomlDataType.LiteralString,
  }
}