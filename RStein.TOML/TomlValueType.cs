namespace RStein.TOML
{
  /// <summary>
  /// Enumeration of TOML value types.
  /// </summary>
  /// <remarks>
  /// <see cref="TomlValueType"/> classifies the semantic type of <see cref="TomlPrimitiveValue"/>.
  /// Each primitive value has a <c>Type</c> property indicating its value type.
  /// </remarks>
  public enum TomlValueType
  {
    /// <summary>
    /// Unknown or unspecified value type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// String value.
    /// </summary>
    String,

    /// <summary>
    /// Boolean value (true or false).
    /// </summary>
    Boolean,

    /// <summary>
    /// DateTime value (offset datetime, local datetime, local date, or local time).
    /// </summary>
    DateTime,

    /// <summary>
    /// Floating-point number (including special values like inf and nan).
    /// </summary>
    Float,

    /// <summary>
    /// Integer number.
    /// </summary>
    Integer
  }
}