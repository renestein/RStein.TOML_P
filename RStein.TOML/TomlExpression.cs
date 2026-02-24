namespace RStein.TOML
{
  /// <summary>
  /// Serves as the abstract base class for all TOML expressions.
  /// </summary>
  public abstract class TomlExpression : TomlToken
  {
    /// <summary>
    /// Initializes a new instance of <see cref="TomlExpression"/> with the specified token type.
    /// </summary>
    protected TomlExpression(TomlTokenType tokenType) : base(tokenType)
    {
    }
  }
}