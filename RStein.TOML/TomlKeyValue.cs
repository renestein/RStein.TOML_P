using System;

namespace RStein.TOML
{
  internal class TomlKeyValue : TomlExpression, IEquatable<TomlKeyValue>
  {
    public TomlKeyValue(TomlKey key,
                        TomlToken value,
                        TomlComment? tomlComment = null) : base(TomlTokenType.KeyValue)
    {
      Key = key;
      Value = value;
    }

    public TomlKey Key
    {
      get;
    }

    public TomlToken Value
    {
      get;
    }

    public TomlComment? Comment
    {
      get;
    }

    public bool Equals(TomlKeyValue? other)
    {
      if (other is null)
      {
        return false;
      }

      if (ReferenceEquals(this, other))
      {
        return true;
      }

      return Equals(Key, other.Key) && Equals(Value, other.Value) && Equals(Comment, other.Comment);
    }

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

      return Equals((TomlKeyValue) obj);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        var hashCode = (Key != null ? Key.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
        hashCode = (hashCode * 397) ^ (Comment != null ? Comment.GetHashCode() : 0);
        return hashCode;
      }
    }

    public override string ToString()
    {
      return $"{nameof(Key)}: {Key}, {nameof(Value)}: {Value}";
    }
  }
}