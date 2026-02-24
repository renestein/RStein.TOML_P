namespace RStein.TOML
{
  /// <summary>
  /// Serves as the abstract base class for all TOML value types - <see cref="TomlPrimitiveValue"/>,
  /// <see cref="TomlTable"/>, <see cref="TomlArray"/>.
  /// </summary>
  public abstract class TomlValue : TomlExpression
  {
    /// <summary>
    /// Initializes a new instance of <see cref="TomlValue"/> with the specified token type.
    /// </summary>
    /// <param name="tokenType">The <see cref="TomlTokenType"/> identifying the kind of value.</param>
    protected TomlValue(TomlTokenType tokenType) : base(tokenType)
    {
    }
  }
}