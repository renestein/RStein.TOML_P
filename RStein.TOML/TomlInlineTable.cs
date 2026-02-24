namespace RStein.TOML
{
  /// <summary>
  /// Represents a TOML inline table, which is a table defined inline using brace notation.
  /// </summary>
  /// <remarks>
  /// <see cref="TomlInlineTable"/> is a specialized subclass of <see cref="TomlTable"/> that represents inline table syntax in TOML.
  /// Functionally, inline tables and regular tables are equivalent; this class distinction is maintained for accurate serialization.
  /// </remarks>
  /// <example>
  /// <code>
  /// // TOML inline table syntax
  /// point = { x = 1, y = 2 }
  /// </code>
  /// </example>
  public class TomlInlineTable : TomlTable
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlInlineTable"/>.
    /// </summary>
    public TomlInlineTable() : base(new TomlKey(TomlTable.ANONYMOUS_TABLE_NAME), TomlTokenType.InlineTable)
    {
      HasFromInlineTableDefinition = true;
      HasFromKeyDefinition = true;
      HasTopLevelDeclaration = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlInlineTable"/> class with the specified key.
    /// </summary>
    /// <param name="fulName">The fully qualified key name for this inline table.</param>
    internal TomlInlineTable(TomlKey fulName) : base(fulName, TomlTokenType.InlineTable)
    {
      HasFromInlineTableDefinition = true;
      HasFromKeyDefinition = true;
      HasTopLevelDeclaration = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlInlineTable"/> class with the specified name string.
    /// </summary>
    /// <param name="name">The name of the inline table. A <see cref="TomlKey"/> will be created from this string.</param>
    internal TomlInlineTable(string name) : this(new TomlKey(name))
    {

    }

    /// <inheritdoc />
    public override string ToString()
    {
      return $"TomlInlineTable - {base.ToString()}";
    }
  }
}