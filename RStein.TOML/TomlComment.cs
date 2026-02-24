using System;

namespace RStein.TOML
{
  /// <summary>
  /// Represents a TOML comment expression (e.g., <c># This is a comment</c>).
  /// </summary>
  public class TomlComment : TomlExpression, IEquatable<TomlComment>
  {
    /// <summary>
    /// Initializes a new instance of <see cref="TomlComment"/> with the specified comment text.
    /// </summary>
    /// <param name="value">The comment text, excluding the leading <c>#</c> character.
    /// If <see langword="null"/>, defaults to <see cref="string.Empty"/>.</param>
    public TomlComment(string value) : base(TomlTokenType.Comment)
    {
      Value = value ?? string.Empty;
    }

    /// <summary>
    /// Gets the text content of the comment.
    /// </summary>
    public string Value
    {
      get;
    }

    /// <inheritdoc />
    public bool Equals(TomlComment? other)
    {
      if (other is null) return false;
      if (ReferenceEquals(this, other)) return true;
      return Value == other.Value;
    }


    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
      if (obj is null) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != GetType()) return false;
      return Equals((TomlComment)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      return Value.GetHashCode();
    }
  }
}