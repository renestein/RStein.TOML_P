using System;
using System.Globalization;
using System.Xml;

namespace RStein.TOML
{
  /// <summary>
  /// Represents a TOML primitive value (string, integer, float, boolean, or datetime).
  /// </summary>
  /// <remarks>
  /// <see cref="TomlPrimitiveValue"/> is the container for all simple TOML value types.
  /// It provides implicit cast operators to common .NET types for convenient conversion (string, int, long, bool, DateTime, double, decimal, etc.). 
  /// </remarks>
  /// <example>
  /// <code>
  /// // Create primitive values
  /// var stringValue = new TomlPrimitiveValue("hello");
  /// var intValue = new TomlPrimitiveValue(42);
  /// var boolValue = new TomlPrimitiveValue(true);
  ///
  /// // Use implicit casts for convenient access
  /// string str = (string?)stringValue ?? "default";
  /// int num = ((int?)intValue) ?? 0;
  /// </code>
  /// </example>
  public class TomlPrimitiveValue : TomlValue, IEquatable<TomlPrimitiveValue>
  {
    /// <summary>
    /// Prefix for octal number literals (e.g., <c>0o17</c>).
    /// </summary>
    public const string OCT_NUMBER_PREFIX = "0o";

    /// <summary>
    /// Prefix for binary number literals (e.g., <c>0b1101</c>).
    /// </summary>
    public const string BIN_NUMBER_PREFIX = "0b";

    /// <summary>
    /// TOML literal for the boolean value <see langword="true"/>.
    /// </summary>
    public const string TRUE_VALUE_LITERAL = "true";

    /// <summary>
    /// TOML literal for the boolean value <see langword="false"/>.
    /// </summary>
    public const string FALSE_VALUE_LITERAL = "false";

    /// <summary>
    /// TOML special float literal representing positive or negative infinity (<c>inf</c>).
    /// </summary>
    public const string SPECIAL_FLOAT_INF_LITERAL = "inf";

    /// <summary>
    /// TOML special float literal representing positive infinity (<c>+inf</c>).
    /// </summary>
    public const string SPECIAL_FLOAT_PLUS_INF_LITERAL = "+inf";

    /// <summary>
    /// TOML special float literal representing negative infinity (<c>-inf</c>).
    /// </summary>
    public const string SPECIAL_FLOAT_MINUS_INF_LITERAL = "-inf";

    /// <summary>
    /// TOML special float literal representing Not-a-Number (<c>nan</c>).
    /// </summary>
    public const string SPECIAL_FLOAT_NAN_LITERAL = "nan";

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with a string value and explicit type.
    /// </summary>
    /// <param name="value">The string value representation.</param>
    /// <param name="type">The TOML value type.</param>
    public TomlPrimitiveValue(string value, TomlValueType type) : this(value, type, TomlDataType.Unspecified)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    public TomlPrimitiveValue(string value) : this(value, TomlDataType.Unspecified)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with a 64-bit integer value.
    /// </summary>
    /// <param name="value">The long integer value.</param>
    public TomlPrimitiveValue(long value) : this(value, TomlDataType.IntegerDec)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with a 32-bit integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public TomlPrimitiveValue(int value) : this(value, TomlDataType.IntegerDec)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with a 16-bit integer value.
    /// </summary>
    /// <param name="value">The short integer value.</param>
    public TomlPrimitiveValue(short value) : this(value, TomlDataType.IntegerDec)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with an 8-bit unsigned integer value.
    /// </summary>
    /// <param name="value">The byte value.</param>
    public TomlPrimitiveValue(byte value) : this(value, TomlDataType.IntegerDec)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with an 8-bit signed integer value.
    /// </summary>
    /// <param name="value">The signed byte value.</param>
    public TomlPrimitiveValue(sbyte value) : this(value, TomlDataType.IntegerDec)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with a GUID value.
    /// </summary>
    /// <param name="value">The GUID value (stored as a string).</param>
    public TomlPrimitiveValue(Guid value) : base(TomlTokenType.PrimitiveValue)
    {
      Type = TomlValueType.String;
      Value = value.ToString();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with a boolean value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    public TomlPrimitiveValue(bool value) : base(TomlTokenType.PrimitiveValue)
    {
      Type = TomlValueType.Boolean;
      Value = value ? TRUE_VALUE_LITERAL : FALSE_VALUE_LITERAL;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with a single-precision floating-point value.
    /// </summary>
    /// <param name="value">The float value.</param>
    public TomlPrimitiveValue(float value) : base(TomlTokenType.PrimitiveValue)
    {
      Type = TomlValueType.Float;
      SubType = TomlDataType.Float;
      Value = Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with a double-precision floating-point value.
    /// </summary>
    /// <param name="value">The double value.</param>
    public TomlPrimitiveValue(double value) : base(TomlTokenType.PrimitiveValue)
    {
      Type = TomlValueType.Float;
      SubType = TomlDataType.Float;
      Value = Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with a decimal value.
    /// </summary>
    /// <param name="value">The decimal value.</param>
    public TomlPrimitiveValue(decimal value) : base(TomlTokenType.PrimitiveValue)
    {
      Type = TomlValueType.Float;
      SubType = TomlDataType.Float;
      Value = Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlPrimitiveValue"/> class with a datetime value.
    /// </summary>
    /// <param name="value">The datetime value (stored as offset datetime).</param>
    public TomlPrimitiveValue(DateTime value) : this(value, TomlDataType.OffsetDateTime)
    {
    }

    internal TomlPrimitiveValue(string? value,
                                TomlValueType type,
                                TomlDataType subType = TomlDataType.Unspecified) : base(TomlTokenType.PrimitiveValue)
    {
      if (type == TomlValueType.Unknown)
      {
        throw new ArgumentException("valueType");
      }

      //TODO Add more guards?
      Value = value ?? string.Empty;
      Type = type;
      SubType = subType;
      if (value == null && type != TomlValueType.String)
      {
        throw new ArgumentNullException(nameof(value));
      }


      if (Type == TomlValueType.String)
      {
        if (subType == TomlDataType.Unspecified)
        {
          SubType = Value.SelectBestTomlStringType();
        }
      }
    }

    internal TomlPrimitiveValue(string value,
                                TomlDataType stringType = TomlDataType.Unspecified) : base(TomlTokenType.PrimitiveValue)
    {
      Value = value ?? String.Empty;
      Type = TomlValueType.String;
      switch (stringType)
      {
        case TomlDataType.RawString:
        case TomlDataType.Unspecified:
        {
          SubType = Value.SelectBestTomlStringType();
          break;
        }
        case TomlDataType.BasicString:
        {
          SubType = Value.Contains("\n") ? TomlDataType.BasicMlString : stringType;

          break;
        }
        case TomlDataType.LiteralString:
        {
          SubType = Value.Contains("\n") ? TomlDataType.LiteralMlString : stringType;

          break;
        }
        
        case TomlDataType.LiteralMlString:
        case TomlDataType.BasicMlString:
        {
          SubType = stringType;
          break;
        }

        default:
          throw new ArgumentOutOfRangeException(nameof(stringType), stringType, null);
      }
    }

    internal TomlPrimitiveValue(long value,
                                TomlDataType tomlDataType) : base(TomlTokenType.PrimitiveValue)
    {
      Type = TomlValueType.Integer;
      SubType = tomlDataType;

      switch (tomlDataType)
      {
        case TomlDataType.IntegerDec:
          Value = Convert.ToString(value);
          break;
        case TomlDataType.IntegerHex:
          Value = $"0x{Convert.ToString(value, toBase: 16)}";
          break;
        case TomlDataType.IntegerOct:
          Value = $"{OCT_NUMBER_PREFIX}{Convert.ToString(value, toBase: 8)}";
          break;
        case TomlDataType.IntegerBin:
          Value = $"{BIN_NUMBER_PREFIX}{Convert.ToString(value, toBase: 2)}";
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(tomlDataType), tomlDataType, null);
      }
    }

    internal TomlPrimitiveValue(int value, TomlDataType tomlDataType = TomlDataType.IntegerDec) : this((long) value, tomlDataType)
    {
      
    }

    internal TomlPrimitiveValue(short value, TomlDataType tomlDataType = TomlDataType.IntegerDec) : this((long)value, tomlDataType)
    {

    }

    internal TomlPrimitiveValue(byte value, TomlDataType tomlDataType = TomlDataType.IntegerDec) : this((long)value, tomlDataType)
    {

    }

    internal TomlPrimitiveValue(sbyte value, TomlDataType tomlDataType = TomlDataType.IntegerDec) : this((long)value, tomlDataType)
    {

    }

    internal TomlPrimitiveValue(DateTime value,
                                TomlDataType tomlDataType = TomlDataType.OffsetDateTime) : base(TomlTokenType.PrimitiveValue)
    {
      Type = TomlValueType.DateTime;
      SubType = tomlDataType;
      switch (tomlDataType)
      {
        case TomlDataType.OffsetDateTime:
          Value = XmlConvert.ToString(value, XmlDateTimeSerializationMode.Utc);
          break;
        case TomlDataType.LocalDateTime:
          Value = value.ToTomlLocalDateTime();
          break;
        case TomlDataType.LocalDate:
          Value = value.ToTomlLocalDate();
          break;
        case TomlDataType.LocalTime:
          Value = value.ToTomlLocalTime();
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(tomlDataType), tomlDataType, null);
      }
    }

    /// <summary>
    /// Gets raw (string) value.
    /// </summary>
    public string Value
    {
      get;
    }

    /// <summary>
    /// Gets the TOML type of the Value.
    /// </summary>
    public TomlValueType Type
    {
      get;
    }

    internal TomlDataType SubType
    {
      get;
    }

    /// <inheritdoc />
    public bool Equals(TomlPrimitiveValue? other)
    {
      if (other is null)
      {
        return false;
      }

      if (ReferenceEquals(this, other))
      {
        return true;
      }

      if (Type != other.Type)
      {
        return false;
      }

      if (SubType != other.SubType)
      {
        return false;
      }

      switch (Type)
      {
        case TomlValueType.String:
        case TomlValueType.Boolean:
        case TomlValueType.DateTime:
        case TomlValueType.Integer:
        {
          return Value.Equals(other.Value, StringComparison.Ordinal);
        }
        case TomlValueType.Float:
          return (((double?) this).Equals((double?) other));
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      if (obj is null)
      {
        return false;
      }

      if (ReferenceEquals(this, obj))
      {
        return true;
      }

      if (obj.GetType() != GetType())
      {
        return false;
      }

      return Equals((TomlPrimitiveValue) obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      unchecked
      {
        var hashCode = Value.GetHashCode();
        hashCode = (hashCode * 397) ^ (int) Type;
        hashCode = (hashCode * 397) ^ (int) SubType;
        return hashCode;
      }
    }

    /// <inheritdoc />
    public override string ToString()
    {
      return Value;
    }
  }
}