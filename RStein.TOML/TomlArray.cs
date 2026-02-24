using System;
using System.Collections.Generic;

namespace RStein.TOML
{
  /// <summary>
  /// Represents a TOML array, which is an ordered collection of values.
  /// </summary>
  /// <remarks>
  /// <see cref="TomlArray"/> implements <see cref="IList{T}"/> to provide list-like access to its elements.
  /// All elements in a TOML array are stored as <see cref="TomlToken"/> objects.
  /// Arrays can contain primitive values, nested tables, or other arrays.
  /// </remarks>
  public class TomlArray : TomlValue, IList<TomlToken>
  {
    internal const string ANONYMOUS_ARRAY_NAME = "AnonymousArray";
    private const string CANNOT_BE_INDEXED_BY_THE_KEY = "TomlArray cannot be indexed by the key.";
    private readonly List<TomlToken> _tokens = new List<TomlToken>();

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlArray"/> class with the specified key.
    /// </summary>
    /// <param name="fullName">The fully qualified key name for this array.</param>
    public TomlArray(TomlKey fullName) : this(fullName, TomlTokenType.Array)
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlArray"/> class with an anonymous name.
    /// </summary>
    public TomlArray() : this(new TomlKey(ANONYMOUS_ARRAY_NAME, TomlKeyType.SimpleUnquoted))
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlArray"/>.
    /// </summary>
    protected TomlArray(TomlKey fullName, TomlTokenType tomlTokenType) : base(tomlTokenType)
    {
      FullName = fullName;
    }

    /// <summary>
    /// Gets the simple name of this array (last part of the fully qualified name).
    /// </summary>
    public string Name => FullName.LastKeyPart.RawKey;

    /// <summary>
    /// Gets the fully qualified key name for this array.
    /// </summary>
    public TomlKey FullName
    {
      get;
    }

    /// <summary>
    /// Adds a <see cref="TomlToken"/> to the end of the array.
    /// </summary>
    /// <param name="tomlValue">The token to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tomlValue"/> is <c>null</c>.</exception>
    public void Add(TomlToken tomlValue)
    {
      if (tomlValue == null)
      {
        throw new ArgumentNullException(nameof(tomlValue));
      }

      _tokens.Add(tomlValue);
    }

    /// <summary>
    /// Adds a <see cref="TomlTable"/> to the end of the array.
    /// </summary>
    /// <param name="tomlTable">The table to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tomlTable"/> is <c>null</c>.</exception>
    public void Add(TomlTable tomlTable)
    {
      if (tomlTable == null)
      {
        throw new ArgumentNullException(nameof(tomlTable));
      }

      Add((TomlToken)tomlTable);
    }

    /// <summary>
    /// Adds a <see cref="TomlArray"/> to the end of the array.
    /// </summary>
    /// <param name="tomlArray">The array to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tomlArray"/> is <c>null</c>.</exception>
    public void Add(TomlArray tomlArray)
    {

      if (tomlArray == null)
      {
        throw new ArgumentNullException(nameof(tomlArray));
      }
      Add((TomlToken)tomlArray);
    }

    /// <summary>
    /// Adds a string value to the end of the array.
    /// </summary>
    /// <param name="value">The string value.</param>
    public void Add(string value)
    {
      Add(new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a boolean value to the end of the array.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    public void Add(bool value)
    {
      Add(new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a 64-bit integer value to the end of the array.
    /// </summary>
    /// <param name="value">The long integer value.</param>
    public void Add(long value)
    {
      Add(new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a 32-bit integer value to the end of the array.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public void Add(int value)
    {
      Add(new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a 16-bit integer value to the end of the array.
    /// </summary>
    /// <param name="value">The short integer value.</param>
    public void Add(short value)
    {
      Add(new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds an 8-bit unsigned integer value to the end of the array.
    /// </summary>
    /// <param name="value">The byte value.</param>
    public void Add(byte value)
    {
      Add(new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds an 8-bit signed integer value to the end of the array.
    /// </summary>
    /// <param name="value">The signed byte value.</param>
    public void Add(sbyte value)
    {
      Add(new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a GUID value to the end of the array (stored as a string).
    /// </summary>
    /// <param name="value">The GUID value.</param>
    public void Add(Guid value)
    {
      Add(new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a single-precision floating-point value to the end of the array.
    /// </summary>
    /// <param name="value">The float value.</param>
    public void Add(float value)
    {
      Add(new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a double-precision floating-point value to the end of the array.
    /// </summary>
    /// <param name="value">The double value.</param>
    public void Add(double value)
    {
      Add(new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a decimal value to the end of the array.
    /// </summary>
    /// <param name="value">The decimal value.</param>
    public void Add(decimal value)
    {
      Add(new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a datetime value to the end of the array.
    /// </summary>
    /// <param name="value">The datetime value.</param>
    public void Add(DateTime value)
    {
      Add(new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Removes all elements from the array.
    /// </summary>
    public void Clear()
    {
      _tokens.Clear();
    }

    /// <summary>
    /// Determines whether the array contains a specific element.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns><c>true</c> if the element is found; otherwise, <c>false</c>.</returns>
    public bool Contains(TomlToken item)
    {
      return _tokens.Contains(item);
    }

    /// <summary>
    /// Copies the elements of the array to an array, starting at a specified array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in the destination array where copying begins.</param>
    public void CopyTo(TomlToken[] array,
                       int arrayIndex)
    {
      _tokens.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Removes the first occurrence of a specific element from the array.
    /// </summary>
    /// <param name="item">The element to remove.</param>
    /// <returns><c>true</c> if the element was successfully removed; otherwise, <c>false</c>.</returns>
    public bool Remove(TomlToken item)
    {
      return _tokens.Remove(item);
    }

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Count => _tokens.Count;

    /// <summary>
    /// Gets a value indicating whether the array is read-only. Always returns <c>false</c>.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Determines the index of a specific element in the array.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns>The index of the element, or -1 if not found.</returns>
    public int IndexOf(TomlToken item)
    {
      return _tokens.IndexOf(item);
    }

    /// <inheritdoc />
    public void Insert(int index,
                       TomlToken item)
    {
      _tokens.Insert(index, item);
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
      _tokens.RemoveAt(index);
    }

    /// <summary>
    /// Gets the <see cref="TomlToken"/> on <paramref name="index"/>.
    /// </summary>
    /// <param name="index">Index of the requested <see cref="TomlToken"/>.</param>
    /// <returns><see cref="TomlToken"/> on <paramref name="index"/>.</returns>
    public override TomlToken this[int index]
    {
      get => _tokens[index];
      set => _tokens[index] = value;
    }

    /// <summary>
    /// Always throws <see cref="InvalidOperationException"/> because <see cref="TomlArray"/> does not support access by the key.
    /// </summary>
    public override TomlToken this[string key]
    {
      get => throw new InvalidOperationException(CANNOT_BE_INDEXED_BY_THE_KEY);
      set => throw new InvalidOperationException(CANNOT_BE_INDEXED_BY_THE_KEY);
    }

    /// <inheritdoc />
    protected override IEnumerable<TomlToken> GetChildren()
    {
      return _tokens;
    }

    /// <inheritdoc />
    public override string ToString()
    {
      return $"{nameof(_tokens)}: {_tokens}, {nameof(Name)}: {Name}, {nameof(Count)}: {Count}";
    }
  }
}