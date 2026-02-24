namespace RStein.TOML
{
  /// <summary>
  /// Enumeration of TOML token types.
  /// </summary>
  /// <remarks>
  /// <see cref="TomlTokenType"/> identifies the specific kind of TOML element represented by a <see cref="TomlToken"/>.
  /// Each <see cref="TomlToken"/> instance has a <see cref="TomlToken.TokenType"/> property that indicates its type.
  /// </remarks>
  public enum TomlTokenType
  {
    /// <summary>Undefined or invalid token type.</summary>
    Undefined = 0,

    /// <summary>Regular TOML table (section).</summary>
    Table,

    /// <summary>Inline TOML table defined using brace notation.</summary>
    InlineTable,

    /// <summary>Key-value pair.</summary>
    KeyValue,

    /// <summary>Comment.</summary>
    Comment,

    /// <summary>Primitive value (string, number, boolean, or datetime).</summary>
    PrimitiveValue,

    /// <summary>TOML array.</summary>
    Array,

    /// <summary>TOML key.</summary>
    Key,

    /// <summary>Array of tables.</summary>
    ArrayOfTables
  }
}