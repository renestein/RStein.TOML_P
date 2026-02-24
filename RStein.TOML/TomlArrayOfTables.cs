namespace RStein.TOML
{
  /// <summary>
  /// Represents a TOML array of tables (e.g., <c>[[table]]</c>).
  /// </summary>
  public class TomlArrayOfTables : TomlArray
  {
    /// <summary>
    /// Initializes a new instance of <see cref="TomlArrayOfTables"/> with a fully qualified <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="fullName">The full dotted key identifying this array of tables.</param>
    public TomlArrayOfTables(TomlKey fullName) : base(fullName, TomlTokenType.ArrayOfTables)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TomlArrayOfTables"/> with a plain string key name.
    /// </summary>
    /// <param name="name">The string key name identifying this array of tables.</param>
    public TomlArrayOfTables(string name) : base(new TomlKey(name), TomlTokenType.ArrayOfTables)
    {
    }

    /// <inheritdoc />
    public override string ToString()
    {
      return $"TomlArrayOfTables - {base.ToString()}";
    }
  }
}