using System;

namespace RStein.TOML
{
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