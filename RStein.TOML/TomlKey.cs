using System;
using System.Collections.Generic;
using System.Linq;

namespace RStein.TOML
{
  /// <summary>
  /// Represents a TOML key
  /// </summary>
 
  public class TomlKey : TomlToken, IEquatable<TomlKey>
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlKey"/> class with the specified raw key string.
    /// </summary>
    /// <param name="rawKey">The raw key string.</param>
    /// <param name="type">The key type. If <see cref="TomlKeyType.Unknown"/>, the type is automatically determined based on the key content.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rawKey"/> is <c>null</c>.</exception>
    public TomlKey(string rawKey, TomlKeyType type = TomlKeyType.Unknown) : base(TomlTokenType.Key)
    {
      RawKey = rawKey ?? throw new ArgumentNullException(nameof(rawKey));
      if (type == TomlKeyType.Unknown)
      {
        Type = needsQuotedKey() ? TomlKeyType.SimpleQuotedBasicString : TomlKeyType.SimpleUnquoted;
      }
      else
      {
        Type = type;
      }

    }

    private TomlKey(TomlKey tomlKey) : base(tomlKey.TokenType)
    {
      NextKeyPart = tomlKey.NextKeyPart == null
        ? null
        : new TomlKey(tomlKey.NextKeyPart);
      RawKey = tomlKey.RawKey;
      Type = tomlKey.Type;
    }

    /// <summary>
    /// Gets or sets the next part of this key.
    /// </summary>
    public TomlKey? NextKeyPart
    {
      get;
      set;
    }

    /// <summary>
    /// Gets the raw key string as specified when the key was created.
    /// </summary>
    public string RawKey
    {
      get;
    }

    /// <summary>
    /// Gets the key type (unquoted or quoted with specific string syntax).
    /// </summary>
    public TomlKeyType Type
    {
      get;
    }

    /// <summary>
    /// Gets a value indicating whether this is a dotted key.
    /// </summary>
    public bool IsDottedKey => NextKeyPart != null;

    /// <summary>
    /// Gets a value indicating whether this key uses quoted syntax (either basic or literal string).
    /// </summary>
    public bool IsQuotedKey => IsQuotedBasicStringKey || IsQuotedLiteralStringKey;

    /// <summary>
    /// Gets a value indicating whether this key uses literal string quotation (no escape sequences).
    /// </summary>
    public bool IsQuotedLiteralStringKey => Type == TomlKeyType.SimpleQuotedLiteralString;

    /// <summary>
    /// Gets a value indicating whether this key uses basic string quotation (with escape sequences).
    /// </summary>
    public bool IsQuotedBasicStringKey => Type == TomlKeyType.SimpleQuotedBasicString;

    /// <summary>
    /// Gets a value indicating whether this key is unquoted.
    /// </summary>
    public bool IsUnquotedKey => !IsQuotedKey;

    /// <summary>
    /// Gets the last key part in this dotted key sequence.
    /// </summary>
    public TomlKey LastKeyPart => NextKeyPart == null ? this : getLastKeyPart();

    /// <summary>
    /// Creates a deep copy of this key.
    /// </summary>
    /// <returns>A new <see cref="TomlKey"/> instance that is a copy of this key.</returns>
    public TomlKey Clone()
    {
      return new TomlKey(this);
    }

    /// <summary>
    /// Determines whether this key is equal to another key.
    /// </summary>
    /// <param name="other">The key to compare with.</param>
    /// <returns><c>true</c> if the keys are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(TomlKey? other)
    {
      if (other is null)
      {
        return false;
      }

      if (ReferenceEquals(this, other))
      {
        return true;
      }

      return (NextKeyPart == null && other.NextKeyPart == null) || (NextKeyPart?.Equals(other.NextKeyPart) ?? false);
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

      return Equals((TomlKey) obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      unchecked
      {
        return ((RawKey.GetHashCode()) * 397) ^ (NextKeyPart != null ? NextKeyPart.GetHashCode() : 0);
      }
    }

    /// <inheritdoc />
    public override string ToString()
    {
      return $"{nameof(RawKey)}: {RawKey}, {nameof(Type)}: {Type}, {nameof(IsDottedKey)}: {IsDottedKey}, {nameof(IsQuotedKey)}: {IsQuotedKey}, {nameof(IsQuotedLiteralStringKey)}: {IsQuotedLiteralStringKey}, {nameof(IsQuotedBasicStringKey)}: {IsQuotedBasicStringKey}, {nameof(IsUnquotedKey)}: {IsUnquotedKey}";
    }

    private TomlKey getLastKeyPart()
    {

      var lastPart = this;
      while (lastPart.NextKeyPart != null)
      {
        lastPart = lastPart.NextKeyPart;
      }

      return lastPart;
    }

    private bool needsQuotedKey()
    {
      return RawKey == string.Empty || RawKey.HasEscapedChar() || RawKey.Any(c => !char.IsLetterOrDigit(c) || !c.IsAsciiChar());
    }

    private sealed class FirstKeyPartEqualityComparer : IEqualityComparer<TomlKey>
    {
      public bool Equals(TomlKey? x,
                         TomlKey? y)
      {
        if (ReferenceEquals(x, y))
        {
          return true;
        }

        if (x is null)
        {
          return false;
        }

        if (y is null)
        {
          return false;
        }

        if (x.GetType() != y.GetType())
        {
          return false;
        }

        return x.RawKey.Equals(y.RawKey, StringComparison.OrdinalIgnoreCase);
      }

      public int GetHashCode(TomlKey obj)
      {
        return obj.RawKey.GetHashCode();
      }
    }

    internal static IEqualityComparer<TomlKey> FirstKeyPartComparer
    {
      get;
    } = new FirstKeyPartEqualityComparer();
  }
}