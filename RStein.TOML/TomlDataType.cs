using System;

namespace RStein.TOML
{

  /// <summary>
  /// Specifies the string value type for a TOML string token.
  /// </summary>
  public enum TomlStringValueType
  {
    /// <summary>
    /// No string type has been specified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// A basic string enclosed in double quotes (e.g. <c>"hello"</c>).
    /// Supports escape sequences.
    /// </summary>
    BasicString = TomlDataType.BasicString,

    /// <summary>
    /// A multi-line basic string enclosed in triple double quotes (e.g. <c>"""hello"""</c>).
    /// Supports escape sequences and spans multiple lines.
    /// </summary>
    BasicMlString = TomlDataType.BasicMlString,

    /// <summary>
    /// A literal string enclosed in single quotes (e.g. <c>'hello'</c>).
    /// No escape sequences are processed.
    /// </summary>
    LiteralString = TomlDataType.LiteralString,

    /// <summary>
    /// A multi-line literal string enclosed in triple single quotes (e.g. <c>'''hello'''</c>).
    /// No escape sequences are processed and spans multiple lines.
    /// </summary>
    LiteralMlString = TomlDataType.LiteralMlString
  }

  [Flags]
  internal enum TomlDataType
  {
    Unspecified = 0 ,
    IntegerDec = 1 << 0,
    IntegerHex = 1 << 2,
    IntegerOct = 1 << 3,
    IntegerBin = 1 << 4,
    Float = 1 << 5,
    OffsetDateTime = 1 << 6,
    LocalDateTime = 1 << 7,
    LocalDate = 1 << 8,
    LocalTime = 1 << 9,
    BasicString = 1 << 10,
    BasicMlString = 1 << 11,
    LiteralString = 1 << 12,
    LiteralMlString = 1 << 13,
    RawString = 1 << 14,
    AllNumberTypes = IntegerBin | IntegerDec | IntegerHex | IntegerOct | Float,
    AllDateTimeTypes = OffsetDateTime | LocalDateTime | LocalTime | LocalDate,
    All = ~Unspecified,
  }

  internal static class NumberDateCandidatesExtensions
  {
    public static TomlValueType ToTomlValueType(this TomlDataType tomlDataType)
    {
      switch (tomlDataType)
      {
        case TomlDataType.IntegerDec:
        case TomlDataType.IntegerHex:
        case TomlDataType.IntegerOct:
        case TomlDataType.IntegerBin:
          return TomlValueType.Integer;
        case TomlDataType.Float:
          return TomlValueType.Float;
        case TomlDataType.LocalDate:
        case TomlDataType.LocalDateTime:
        case TomlDataType.LocalTime:
        case TomlDataType.OffsetDateTime:
          return TomlValueType.DateTime;
        default:
          throw new ArgumentOutOfRangeException(nameof(tomlDataType), tomlDataType, null);
      }
    }
  }
}