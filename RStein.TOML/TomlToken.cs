using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace RStein.TOML
{
  /// <summary>
  /// Abstract base class for all TOML tokens (tables, arrays, keys, values, etc.).
  /// </summary>
  /// <remarks>
  /// <see cref="TomlToken"/> is the root of the TOML object model hierarchy. All TOML elements (tables, arrays, keys, values) derive from this class.
  /// It implements <see cref="IEnumerable{T}"/> to provide iteration over child tokens.
  /// The <see cref="TokenType"/> property indicates the specific kind of token.
  /// Supports indexed access via both integer indexing (for <see cref="TomlArray"/> and <see cref="TomlTable"/>) and string/key indexing (for <see cref="TomlTable"/>).
  /// Provides explicit cast operators to common .NET types for type conversion of primitive values.
  /// </remarks>
  public abstract class TomlToken : IEnumerable<TomlToken>
  {
    private const string DOES_NOT_CONTAIN_CHILDREN_ERROR = "This TomlToken does not contain  children.";
    private const string CAST_NOT_SUPPORTED_ERROR = "Cast not supported.";

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlToken"/>.
    /// </summary>
    protected TomlToken(TomlTokenType tokenType)
    {
      if (tokenType == TomlTokenType.Undefined)
      {
        throw new ArgumentException(nameof(tokenType));
      }

      TokenType = tokenType;
    }

    /// <summary>
    /// Gets the enumerator that enumerates child <see cref="TomlToken"/> instances belonging to the current token.
    /// </summary>
    public IEnumerable<TomlToken> Tokens => GetChildren();

    /// <summary>
    /// Gets the <see cref="TomlToken"/> type.
    /// </summary>
    public TomlTokenType TokenType
    {
      get;
    }

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element.</param>
    /// <returns>The value at the specified index.</returns>
    /// <exception cref="InvalidOperationException">The <see cref="TomlToken"/> does not support the operation.</exception>
    public virtual TomlToken this[int index]
    {
      get => throw new InvalidOperationException(DOES_NOT_CONTAIN_CHILDREN_ERROR);
      set => throw new InvalidOperationException(DOES_NOT_CONTAIN_CHILDREN_ERROR);
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key using a string key.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <returns>The value associated with the key.</returns>
    /// <exception cref="InvalidOperationException">The <see cref="TomlToken"/> does not support the operation.</exception>
    public virtual TomlToken this[string key]
    {
      get => throw new InvalidOperationException(DOES_NOT_CONTAIN_CHILDREN_ERROR);
      set => throw new InvalidOperationException(DOES_NOT_CONTAIN_CHILDREN_ERROR);
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <returns>The value associated with the key.</returns>
    /// <exception cref="InvalidOperationException">The <see cref="TomlToken"/> does not support the operation.</exception>
    public virtual TomlToken this[TomlKey key]
    {
      get => throw new InvalidOperationException(DOES_NOT_CONTAIN_CHILDREN_ERROR);
      set => throw new InvalidOperationException(DOES_NOT_CONTAIN_CHILDREN_ERROR);
    }


    /// <inheritdoc />
    public IEnumerator<TomlToken> GetEnumerator()
    {
      return GetChildren().GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    /// <summary>
    /// Enumerates all children of this <see cref="TomlToken"/>.
    /// </summary>
    protected virtual IEnumerable<TomlToken> GetChildren() => Enumerable.Empty<TomlToken>();

    internal async ValueTask AcceptVisitorAsync<TContext>(ITomlVisitor<TContext> tomlVisitor, TContext context)
    {
      if (tomlVisitor == null)
      {
        throw new ArgumentNullException(nameof(tomlVisitor));
      }

      switch (TokenType)
      {
        case TomlTokenType.Table:
          await tomlVisitor.Visit((TomlTable)this, context).ConfigureAwait(false);
          break;
        case TomlTokenType.KeyValue:
          await tomlVisitor.Visit((TomlKeyValue)this, context).ConfigureAwait(false);
          break;
        case TomlTokenType.Comment:
          await tomlVisitor.Visit((TomlComment)this, context).ConfigureAwait(false);
          break;
        case TomlTokenType.PrimitiveValue:
          await tomlVisitor.Visit((TomlPrimitiveValue)this, context).ConfigureAwait(false);
          break;
        case TomlTokenType.Array:
          await tomlVisitor.Visit((TomlArray)this, context).ConfigureAwait(false);
          break;
        case TomlTokenType.Key:
          await tomlVisitor.Visit((TomlKey)this, context).ConfigureAwait(false);
          break;
        case TomlTokenType.ArrayOfTables:
          await tomlVisitor.Visit((TomlArrayOfTables)this, context).ConfigureAwait(false);
          break;
        case TomlTokenType.InlineTable:
          await tomlVisitor.Visit((TomlInlineTable)this, context).ConfigureAwait(false);
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    /// <summary>
    /// Asynchronously determines whether the current <see cref="TomlToken"/> instance is deeply equal
    /// to another <see cref="TomlToken"/> instance, performing a recursive structural comparison
    /// of all nested elements and values.
    /// </summary>
    /// <param name="other">
    /// The <see cref="TomlToken"/> instance to compare with the current instance.
    /// Can be <see langword="null"/>, in which case the method returns <see langword="false"/>
    /// unless the current instance is also <see langword="null"/>.
    /// </param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> of <see cref="bool"/> that represents the asynchronous
    /// comparison operation. The task result is <see langword="true"/> if the current instance
    /// is structurally and deeply equal to <paramref name="other"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Unlike a shallow equality check, this method traverses the entire token tree recursively,
    /// comparing each child token, key, and value to ensure complete structural equivalence.
    /// </para>
    /// </remarks>
    public virtual async Task<bool> DeepEqualsAsync(TomlToken? other)
    {
      if (other == null)
      {
        return false;
      }

      var deepEqualsContext = new DeepEqualsVisitorContext()
      {
        OtherToken = other,
        DeepEqualsResult = true
      };

      var deepEqualsVisitor = new DeepEqualsVisitor();
      await AcceptVisitorAsync(deepEqualsVisitor, deepEqualsContext).ConfigureAwait(false);
      return deepEqualsContext.DeepEqualsResult;
    }

    /// <summary>
    /// Asynchronously serializes the current <see cref="TomlToken"/> instance into its
    /// TOML-compliant string representation.
    /// </summary>
    /// <returns>
    /// A <see cref="Task{TResult}"/> of <see cref="string"/> representing the asynchronous
    /// serialization operation. The task result contains the full TOML-formatted string
    /// of the current token and all its nested elements.
    /// </returns>
    public virtual async Task<string> SerializeToStringAsync()
    {
      return await TomlSerializer.SerializeToStringAsync(this).ConfigureAwait(false);
    }

    #region Implicit conversions

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to its <see cref="string"/> representation.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// A <see cref="string"/> containing the TOML representation of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> cannot be represented as a valid TOML string.
    /// </exception>
    public static explicit operator string?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (tomlToken is TomlPrimitiveValue tomlPrimitiveValue)
      {
        return tomlPrimitiveValue.Value;
      }

      throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="int"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="int"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid integer value.
    /// </exception>
    public static explicit operator int?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue tomlPrimitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      switch (tomlPrimitiveValue.Type)
      {
        case TomlValueType.Integer:
          {
            switch (tomlPrimitiveValue.SubType)
            {
              case TomlDataType.Unspecified:
                break;
              case TomlDataType.IntegerDec:
                return int.Parse(tomlPrimitiveValue.Value, CultureInfo.InvariantCulture);
              case TomlDataType.IntegerHex:
                return Convert.ToInt32(tomlPrimitiveValue.Value, fromBase: 16);
              case TomlDataType.IntegerOct:
                return Convert.ToInt32(tomlPrimitiveValue.Value.Substring(TomlPrimitiveValue.OCT_NUMBER_PREFIX.Length), fromBase: 8);
              case TomlDataType.IntegerBin:
                return Convert.ToInt32(tomlPrimitiveValue.Value.Substring(TomlPrimitiveValue.BIN_NUMBER_PREFIX.Length), fromBase: 2);
              default:
                throw new ArgumentOutOfRangeException();
            }
            break;
          }
        case TomlValueType.Float:
          {
            return (int?)(float?)tomlPrimitiveValue;
          }
        case TomlValueType.Boolean:
          {
            return Convert.ToInt32((bool?)tomlPrimitiveValue);
          }
      }

      throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="short"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="short"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid short value.
    /// </exception>
    public static explicit operator short?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue tomlPrimitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      switch (tomlPrimitiveValue.Type)
      {
        case TomlValueType.Integer:
          {
            switch (tomlPrimitiveValue.SubType)
            {
              case TomlDataType.Unspecified:
                break;
              case TomlDataType.IntegerDec:
                return short.Parse(tomlPrimitiveValue.Value, CultureInfo.InvariantCulture);
              case TomlDataType.IntegerHex:
                return Convert.ToInt16(tomlPrimitiveValue.Value, fromBase: 16);
              case TomlDataType.IntegerOct:
                return Convert.ToInt16(tomlPrimitiveValue.Value.Substring(TomlPrimitiveValue.OCT_NUMBER_PREFIX.Length), fromBase: 8);
              case TomlDataType.IntegerBin:
                return Convert.ToInt16(tomlPrimitiveValue.Value.Substring(TomlPrimitiveValue.BIN_NUMBER_PREFIX.Length), fromBase: 2);
              default:
                throw new ArgumentOutOfRangeException();
            }
            break;
          }
        case TomlValueType.Float:
          {
            return (short?)(float?)tomlPrimitiveValue;
          }
        case TomlValueType.Boolean:
          {
            return Convert.ToInt16((bool?)tomlPrimitiveValue);
          }
      }

      throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="ushort"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="ushort"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid ushort value.
    /// </exception>
    public static explicit operator ushort?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue tomlPrimitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      switch (tomlPrimitiveValue.Type)
      {
        case TomlValueType.Integer:
          {
            switch (tomlPrimitiveValue.SubType)
            {
              case TomlDataType.Unspecified:
                break;
              case TomlDataType.IntegerDec:
                return ushort.Parse(tomlPrimitiveValue.Value, CultureInfo.InvariantCulture);
              case TomlDataType.IntegerHex:
                return Convert.ToUInt16(tomlPrimitiveValue.Value, fromBase: 16);
              case TomlDataType.IntegerOct:
                return Convert.ToUInt16(tomlPrimitiveValue.Value.Substring(TomlPrimitiveValue.OCT_NUMBER_PREFIX.Length), fromBase: 8);
              case TomlDataType.IntegerBin:
                return Convert.ToUInt16(tomlPrimitiveValue.Value.Substring(TomlPrimitiveValue.BIN_NUMBER_PREFIX.Length), fromBase: 2);
              default:
                throw new ArgumentOutOfRangeException();
            }
            break;
          }
        case TomlValueType.Float:
          {
            return (ushort?)(float?)tomlPrimitiveValue;
          }
        case TomlValueType.Boolean:
          {
            return Convert.ToUInt16((bool?)tomlPrimitiveValue);
          }
      }

      throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="long"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="long"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid long value.
    /// </exception>
    public static explicit operator long?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }


      if (!(tomlToken is TomlPrimitiveValue primitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      switch (primitiveValue.Type)
      {
        case TomlValueType.Integer:
          {
            switch (primitiveValue.SubType)
            {
              case TomlDataType.Unspecified:
                break;
              case TomlDataType.IntegerDec:
                return long.Parse(primitiveValue.Value, CultureInfo.InvariantCulture);
              case TomlDataType.IntegerHex:
                return Convert.ToInt64(primitiveValue.Value, fromBase: 16);
              case TomlDataType.IntegerOct:
                return Convert.ToInt64(primitiveValue.Value.Substring(TomlPrimitiveValue.OCT_NUMBER_PREFIX.Length), fromBase: 8);
              case TomlDataType.IntegerBin:
                return Convert.ToInt64(primitiveValue.Value.Substring(TomlPrimitiveValue.BIN_NUMBER_PREFIX.Length), fromBase: 2);
              default:
                throw new ArgumentOutOfRangeException();
            }

            break;
          }
        case TomlValueType.Boolean:
          {
            return Convert.ToInt64((bool?)primitiveValue);
          }
        case TomlValueType.Float:
          {
            return (long?)(double?)primitiveValue;
          }
      }

      throw new InvalidCastException();
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="double"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="double"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid double value.
    /// </exception>
    public static explicit operator double?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue primitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      if (primitiveValue.Type != TomlValueType.Integer && primitiveValue.Type != TomlValueType.Float)
      {
        throw new InvalidCastException();
      }

      switch (primitiveValue.Value)
      {
        case TomlPrimitiveValue.SPECIAL_FLOAT_INF_LITERAL:
        case TomlPrimitiveValue.SPECIAL_FLOAT_PLUS_INF_LITERAL:
          {
            return double.PositiveInfinity;
          }
        case TomlPrimitiveValue.SPECIAL_FLOAT_MINUS_INF_LITERAL:
          {
            return double.NegativeInfinity;
          }
        default:
          {
            return double.Parse(primitiveValue.Value, CultureInfo.InvariantCulture);
          }
      }
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="float"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="float"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid float value.
    /// </exception>
    public static explicit operator float?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue primitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      if (primitiveValue.Type != TomlValueType.Integer && primitiveValue.Type != TomlValueType.Float)
      {
        throw new InvalidCastException();
      }

      switch (primitiveValue.Value)
      {
        case TomlPrimitiveValue.SPECIAL_FLOAT_INF_LITERAL:
        case TomlPrimitiveValue.SPECIAL_FLOAT_PLUS_INF_LITERAL:
          {
            return float.PositiveInfinity;
          }
        case TomlPrimitiveValue.SPECIAL_FLOAT_MINUS_INF_LITERAL:
          {
            return float.NegativeInfinity;
          }
        default:
          {
            return float.Parse(primitiveValue.Value, CultureInfo.InvariantCulture);
          }
      }
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="decimal"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="decimal"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid decimal value.
    /// </exception>

    public static explicit operator decimal?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue primitiveValue) || (primitiveValue.Type != TomlValueType.Integer && primitiveValue.Type != TomlValueType.Float))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      return decimal.Parse(primitiveValue.Value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="uint"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="uint"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid uint value.
    /// </exception>
    public static explicit operator uint?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue primitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      switch (primitiveValue.Type)
      {
        case TomlValueType.Integer:
          {
            switch (primitiveValue.SubType)
            {
              case TomlDataType.Unspecified:
                break;
              case TomlDataType.IntegerDec:
                return uint.Parse(primitiveValue.Value, CultureInfo.InvariantCulture);
              case TomlDataType.IntegerHex:
                return Convert.ToUInt32(primitiveValue.Value, fromBase: 16);
              case TomlDataType.IntegerOct:
                return Convert.ToUInt32(primitiveValue.Value.Substring(TomlPrimitiveValue.OCT_NUMBER_PREFIX.Length), fromBase: 8);
              case TomlDataType.IntegerBin:
                return Convert.ToUInt32(primitiveValue.Value.Substring(TomlPrimitiveValue.BIN_NUMBER_PREFIX.Length), fromBase: 2);
              default:
                throw new ArgumentOutOfRangeException();
            }
            break;
          }
        case TomlValueType.Boolean:
          {
            return Convert.ToUInt32((bool?)primitiveValue);
          }
      }

      throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="ulong"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="ulong"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid ulong value.
    /// </exception>
    public static explicit operator ulong?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue primitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      switch (primitiveValue.Type)
      {
        case TomlValueType.Integer:
          {
            switch (primitiveValue.SubType)
            {
              case TomlDataType.Unspecified:
                break;
              case TomlDataType.IntegerDec:
                return uint.Parse(primitiveValue.Value, CultureInfo.InvariantCulture);
              case TomlDataType.IntegerHex:
                return Convert.ToUInt64(primitiveValue.Value, fromBase: 16);
              case TomlDataType.IntegerOct:
                return Convert.ToUInt64(primitiveValue.Value.Substring(TomlPrimitiveValue.OCT_NUMBER_PREFIX.Length), fromBase: 8);
              case TomlDataType.IntegerBin:
                return Convert.ToUInt64(primitiveValue.Value.Substring(TomlPrimitiveValue.BIN_NUMBER_PREFIX.Length), fromBase: 2);
              default:
                throw new ArgumentOutOfRangeException();
            }
            break;
          }
        case TomlValueType.Boolean:
          {
            return Convert.ToUInt64((bool?)primitiveValue);
          }
      }

      throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="byte"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="byte"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid byte value.
    /// </exception>
    public static explicit operator byte?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue primitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      if (primitiveValue.Type != TomlValueType.Integer)
      {
        throw new InvalidCastException();
      }

      return (byte?)(uint?)primitiveValue;
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="sbyte"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="sbyte"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid sbyte value.
    /// </exception>
    public static explicit operator sbyte?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue primitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      if (primitiveValue.Type != TomlValueType.Integer)
      {
        throw new InvalidCastException();
      }

      return (sbyte?)(int?)primitiveValue;
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="char"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="char"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid char value.
    /// </exception>
    public static explicit operator char?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue primitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      if (primitiveValue.Type != TomlValueType.String)
      {
        throw new InvalidCastException();
      }

      if (String.IsNullOrEmpty(primitiveValue.Value))
      {
        return null;
      }

      return primitiveValue.Value[0];
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="bool"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="bool"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid bool value.
    /// </exception>
    public static explicit operator bool?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue primitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      switch (primitiveValue.Type)
      {
        case TomlValueType.Boolean:
          switch (primitiveValue.Value)
          {
            case TomlPrimitiveValue.TRUE_VALUE_LITERAL:
              return true;
            case TomlPrimitiveValue.FALSE_VALUE_LITERAL:
              return false;
          }

          throw new InvalidCastException();
        case TomlValueType.Integer:
          {
            var intValue = (int?)primitiveValue;
            return intValue > 0;
          }
        default:
          throw new InvalidCastException();
      }
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="Guid"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="Guid"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid Guid value.
    /// </exception>
    public static explicit operator Guid?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue primitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      if (primitiveValue.Type != TomlValueType.String)
      {
        throw new InvalidCastException();
      }

      return Guid.TryParse(primitiveValue.Value, out var guid)
        ? guid
        : throw new InvalidCastException();
    }

    /// <summary>
    /// Explicitly converts a <see cref="TomlToken"/> instance to a nullable <see cref="DateTime"/>.
    /// </summary>
    /// <param name="tomlToken">
    /// The <see cref="TomlToken"/> to convert. Can be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// An <see cref="DateTime"/> containing the value of <paramref name="tomlToken"/>,
    /// or <see langword="null"/> if <paramref name="tomlToken"/> is <see langword="null"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if <paramref name="tomlToken"/> does not represent a valid DateTime value.
    /// </exception>
    public static explicit operator DateTime?(TomlToken? tomlToken)
    {
      if (tomlToken == null)
      {
        return null;
      }

      if (!(tomlToken is TomlPrimitiveValue primitiveValue))
      {
        throw new InvalidCastException(CAST_NOT_SUPPORTED_ERROR);
      }

      if (primitiveValue.Type != TomlValueType.DateTime)
      {
        throw new InvalidCastException();
      }

      var toParseValue = primitiveValue.Value;
      if ((primitiveValue.SubType == TomlDataType.LocalDateTime || primitiveValue.SubType == TomlDataType.OffsetDateTime) && toParseValue.IndexOf('T') < 0)
      {
        toParseValue = toParseValue.Replace(' ', 'T');
      }

      return XmlConvert.ToDateTime(toParseValue, XmlDateTimeSerializationMode.Utc);
    }
    #endregion

    #region Explicit conversions

    ///<summary>
    /// Implicitly converts a <see cref="string"/> value to a <see cref="TomlToken"/> instance.
    /// </summary>
    /// <param name="value">
    /// The <see cref="string"/> value to convert.
    /// </param>
    /// <returns>
    /// A <see cref="TomlPrimitiveValue"/> representing <paramref name="value"/>.
    /// </returns>
    public static implicit operator TomlToken(string value)
    {
      if (value == null)
      {
        throw new ArgumentNullException(nameof(value));
      }

      return new TomlPrimitiveValue(value);
    }

    ///<summary>
    /// Implicitly converts a <see cref="long"/> value to a <see cref="TomlToken"/> instance.
    /// </summary>
    /// <param name="value">
    /// The <see cref="long"/> value to convert.
    /// </param>
    /// <returns>
    /// A <see cref="TomlPrimitiveValue"/> representing <paramref name="value"/>.
    /// </returns>
    public static implicit operator TomlToken(long value)
    {
      return new TomlPrimitiveValue(value);
    }

    ///<summary>
    /// Implicitly converts a <see cref="int"/> value to a <see cref="TomlToken"/> instance.
    /// </summary>
    /// <param name="value">
    /// The <see cref="int"/> value to convert.
    /// </param>
    /// <returns>
    /// A <see cref="TomlPrimitiveValue"/> representing <paramref name="value"/>.
    /// </returns>
    public static implicit operator TomlToken(int value)
    {
      return new TomlPrimitiveValue(value);
    }

    ///<summary>
    /// Implicitly converts a <see cref="short"/> value to a <see cref="TomlToken"/> instance.
    /// </summary>
    /// <param name="value">
    /// The <see cref="short"/> value to convert.
    /// </param>
    /// <returns>
    /// A <see cref="TomlPrimitiveValue"/> representing <paramref name="value"/>.
    /// </returns>
    public static implicit operator TomlToken(short value)
    {
      return new TomlPrimitiveValue(value);
    }

    ///<summary>
    /// Implicitly converts a <see cref="byte"/> value to a <see cref="TomlToken"/> instance.
    /// </summary>
    /// <param name="value">
    /// The <see cref="byte"/> value to convert.
    /// </param>
    /// <returns>
    /// A <see cref="TomlPrimitiveValue"/> representing <paramref name="value"/>.
    /// </returns>
    public static implicit operator TomlToken(byte value)
    {
      return new TomlPrimitiveValue(value);
    }

    ///<summary>
    /// Implicitly converts a <see cref="sbyte"/> value to a <see cref="TomlToken"/> instance.
    /// </summary>
    /// <param name="value">
    /// The <see cref="sbyte"/> value to convert.
    /// </param>
    /// <returns>
    /// A <see cref="TomlPrimitiveValue"/> representing <paramref name="value"/>.
    /// </returns>
    public static implicit operator TomlToken(sbyte value)
    {
      return new TomlPrimitiveValue(value);
    }

    ///<summary>
    /// Implicitly converts a <see cref="Guid"/> value to a <see cref="TomlToken"/> instance.
    /// </summary>
    /// <param name="value">
    /// The <see cref="Guid"/> value to convert.
    /// </param>
    /// <returns>
    /// A <see cref="TomlPrimitiveValue"/> representing <paramref name="value"/>.
    /// </returns>
    public static implicit operator TomlToken(Guid value)
    {
      return new TomlPrimitiveValue(value);
    }

    ///<summary>
    /// Implicitly converts a <see cref="bool"/> value to a <see cref="TomlToken"/> instance.
    /// </summary>
    /// <param name="value">
    /// The <see cref="bool"/> value to convert.
    /// </param>
    /// <returns>
    /// A <see cref="TomlPrimitiveValue"/> representing <paramref name="value"/>.
    /// </returns>
    public static implicit operator TomlToken(bool value)
    {
      return new TomlPrimitiveValue(value);
    }

    ///<summary>
    /// Implicitly converts a <see cref="double"/> value to a <see cref="TomlToken"/> instance.
    /// </summary>
    /// <param name="value">
    /// The <see cref="double"/> value to convert.
    /// </param>
    /// <returns>
    /// A <see cref="TomlPrimitiveValue"/> representing <paramref name="value"/>.
    /// </returns>
    public static implicit operator TomlToken(double value)
    {
      return new TomlPrimitiveValue(value);
    }

    ///<summary>
    /// Implicitly converts a <see cref="decimal"/> value to a <see cref="TomlToken"/> instance.
    /// </summary>
    /// <param name="value">
    /// The <see cref="decimal"/> value to convert.
    /// </param>
    /// <returns>
    /// A <see cref="TomlPrimitiveValue"/> representing <paramref name="value"/>.
    /// </returns>
    public static implicit operator TomlToken(decimal value)
    {
      return new TomlPrimitiveValue(value);
    }

    ///<summary>
    /// Implicitly converts a <see cref="DateTime"/> value to a <see cref="TomlToken"/> instance.
    /// </summary>
    /// <param name="value">
    /// The <see cref="DateTime"/> value to convert.
    /// </param>
    /// <returns>
    /// A <see cref="TomlPrimitiveValue"/> representing <paramref name="value"/>.
    /// </returns>
    public static implicit operator TomlToken(DateTime value)
    {
      return new TomlPrimitiveValue(value);
    }

    #endregion

  }
}