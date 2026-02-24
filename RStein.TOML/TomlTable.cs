using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RStein.TOML
{
  /// <summary>
  /// Represents a TOML table (section), which is a collection of key-value pairs and nested tables.
  /// </summary>
  /// <remarks>
  /// <see cref="TomlTable"/> implements <see cref="IDictionary{TKey, TValue}"/> to provide dictionary-like access to its contents.
  /// Keys are represented as <see cref="TomlKey"/> objects and values are <see cref="TomlToken"/> instances.
  /// Nested tables can be accessed using the indexer with either string keys or <see cref="TomlKey"/> objects.
  /// </remarks>
  public class TomlTable : TomlValue, IDictionary<TomlKey, TomlToken>
  {
    internal const string ROOT_TABLE_NAME = "Root table";
    internal const string ANONYMOUS_TABLE_NAME = "Anonymous table";
    private readonly IDictionary<TomlKey, TomlToken> _innerDictionary  = new Dictionary<TomlKey, TomlToken>(TomlKey.FirstKeyPartComparer);
    private IDictionary<TomlKey, TomlTable>? _unresolvedInnerTables;
    private bool _hasTopLevelDefinition;
    private bool _hasTopLevelDeclaration;
    private bool _hasFromKeyDefinition;
    private bool _hasFromInlineTableDefinition;
    private bool _isArrayOfTablesMember;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlTable"/> class with the specified key.
    /// </summary>
    /// <param name="fullName">The fully qualified key name for this table.</param>
    public TomlTable(TomlKey fullName) : this(fullName, TomlTokenType.Table)
    {

    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlTable"/> class with the specified name string.
    /// </summary>
    /// <param name="name">The name of the table. A <see cref="TomlKey"/> will be created from this string.</param>
    public TomlTable(string name) : this(new TomlKey(name), TomlTokenType.Table)
    {

    }

    /// <summary>
    /// Initializes a TOML root table.
    /// </summary>
    public TomlTable() : this(new TomlKey(ROOT_TABLE_NAME, TomlKeyType.SimpleUnquoted))
    {

    }

    /// <summary>
    /// Initializes a new instance of the  <see cref="TomlTable"/>
    /// </summary>
    protected TomlTable(TomlKey fulName, TomlTokenType tomlTokenType = TomlTokenType.Table) : base(tomlTokenType)
    {
      FullName = fulName ?? throw new ArgumentNullException(nameof(fulName));
    }

    /// <summary>
    /// Gets the simple name of this table (last part of the fully qualified name).
    /// </summary>
    public string Name => FullName.RawKey;

    /// <summary>
    /// Gets the fully qualified key name for this table.
    /// </summary>
    public TomlKey FullName
    {
      get;
    }

    internal bool HasTopLevelDeclaration
    {
      get => _hasTopLevelDeclaration;
      set
      {
        if (!value)
        {
          _hasTopLevelDefinition = false;
        }

        _hasTopLevelDeclaration = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this table has a top-level definition in the TOML document.
    /// </summary>
    public bool HasTopLevelDefinition
    {
      get => _hasTopLevelDefinition;
      set
      {
        if (value)
        {
          _hasTopLevelDeclaration = true;
        }

        _hasTopLevelDefinition = value;
        if (value && _unresolvedInnerTables != null)
        {
          _hasFromKeyDefinition = _hasFromInlineTableDefinition = false;
          PropagateTagsToUnresolvedTables();
        }
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this table is defined via a key in another table.
    /// </summary>
    public bool HasFromKeyDefinition
    {
      get => _hasFromKeyDefinition;
      set
      {
        _hasFromKeyDefinition = value;
        if (value && _unresolvedInnerTables != null)
        {
          _hasTopLevelDefinition = false;
          PropagateTagsToUnresolvedTables();

        }
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this table is an inline table defined using brace notation.
    /// </summary>
    public bool HasFromInlineTableDefinition
    {
      get => _hasFromInlineTableDefinition;
      set
      {
        _hasFromInlineTableDefinition = value;
        if (value && _unresolvedInnerTables != null)
        {
          _hasTopLevelDefinition = false;
          PropagateTagsToUnresolvedTables();
        }
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this table is a member of an array of tables.
    /// </summary>
    public bool IsArrayOfTablesMember
    {
      get => _isArrayOfTablesMember;
      set
      {
        _isArrayOfTablesMember = value;
        if (value && _unresolvedInnerTables != null)
        {
          PropagateTagsToUnresolvedTables();
        }
      }
    }

    /// <inheritdoc />
    public new IEnumerator<KeyValuePair<TomlKey, TomlToken>> GetEnumerator()
    {
      return _innerDictionary.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
      return ((IEnumerable) _innerDictionary).GetEnumerator();
    }

    /// <inheritdoc />
    public void Clear()
    {
      _innerDictionary.Clear();
    }

    /// <inheritdoc />
    public bool Contains(KeyValuePair<TomlKey, TomlToken> item)
    {
      return _innerDictionary.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TomlKey, TomlToken>[] array,
                       int arrayIndex)
    {
      _innerDictionary.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TomlKey, TomlToken> item)
    {
      return _innerDictionary.Remove(item);
    }

    /// <inheritdoc />
    public int Count => _innerDictionary.Count;

    /// <inheritdoc />
    public bool IsReadOnly => _innerDictionary.IsReadOnly;

    /// <inheritdoc />
    public void Add(KeyValuePair<TomlKey, TomlToken> item)
    {
      if (item.Value is TomlTable tomlTable && tomlTable.IsUntaggedTable())
      {
        if (Name.Equals(ROOT_TABLE_NAME, StringComparison.OrdinalIgnoreCase))
        {
          tomlTable.HasTopLevelDefinition = true;
        }
        else
        {
          if (!IsUntaggedTable())
          {
            PropagateTagsToInnerTable(tomlTable);
          }
          else
          {
            _unresolvedInnerTables = _unresolvedInnerTables ?? new Dictionary<TomlKey, TomlTable>();
            _unresolvedInnerTables.Add(tomlTable.FullName, tomlTable);
          }
        }
      }

      _innerDictionary.Add(item);
    }

    /// <summary>
    /// Adds a key-value pair to the table.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The value as a <see cref="TomlToken"/>.</param>
    public void Add(TomlKey key, TomlToken value)
    {
      Add(new KeyValuePair<TomlKey, TomlToken>(key, value));
    }

    /// <summary>
    /// Adds a key-value pair to the table using a string key.
    /// </summary>
    /// <param name="key">The key as a string. A <see cref="TomlKey"/> will be created from this string.</param>
    /// <param name="value">The value as a <see cref="TomlToken"/>.</param>
    public void Add(string key, TomlToken value)
    {
      Add(new TomlKey(key), value);
    }

    /// <summary>
    /// Adds a nested table to this table.
    /// </summary>
    /// <param name="tomlTable">The table to add.</param>
    public void Add(TomlTable tomlTable)
    {
      Add(tomlTable.FullName, tomlTable);
    }

    /// <summary>
    /// Adds a nested array to this table.
    /// </summary>
    /// <param name="tomlArray">The array to add.</param>
    public void Add(TomlArray tomlArray)
    {
      Add(tomlArray.FullName, tomlArray);
    }

    /// <summary>
    /// Adds a string value to the table.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <param name="value">The string value.</param>
    public void Add(string key, string value)
    {
      Add(new TomlKey(key), new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a boolean value to the table.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <param name="value">The boolean value.</param>
    public void Add(string key, bool value)
    {
      Add(new TomlKey(key), new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a 64-bit integer value to the table.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <param name="value">The long integer value.</param>
    public void Add(string key, long value)
    {
      Add(new TomlKey(key), new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a 32-bit integer value to the table.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <param name="value">The integer value.</param>
    public void Add(string key, int value)
    {
      Add(new TomlKey(key), new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a 16-bit integer value to the table.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <param name="value">The short integer value.</param>
    public void Add(string key, short value)
    {
      Add(new TomlKey(key), new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds an 8-bit unsigned integer value to the table.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <param name="value">The byte value.</param>
    public void Add(string key, byte value)
    {
      Add(new TomlKey(key), new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds an 8-bit signed integer value to the table.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <param name="value">The signed byte value.</param>
    public void Add(string key, sbyte value)
    {
      Add(new TomlKey(key), new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a GUID value to the table (stored as a string).
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <param name="value">The GUID value.</param>
    public void Add(string key, Guid value)
    {
      Add(new TomlKey(key), new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a single-precision floating-point value to the table.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <param name="value">The float value.</param>
    public void Add(string key, float value)
    {
      Add(new TomlKey(key), new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a double-precision floating-point value to the table.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <param name="value">The double value.</param>
    public void Add(string key, double value)
    {
      Add(new TomlKey(key), new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a decimal value to the table.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <param name="value">The decimal value.</param>
    public void Add(string key, decimal value)
    {
      Add(new TomlKey(key), new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a datetime value to the table.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <param name="value">The datetime value.</param>
    public void Add(string key, DateTime value)
    {
      Add(new TomlKey(key), new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a string value to the table using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The string value.</param>
    public void Add(TomlKey key, string value)
    {
      Add(key, new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a boolean value to the table using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The boolean value.</param>
    public void Add(TomlKey key, bool value)
    {
      Add(key, new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a 64-bit integer value to the table using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The long integer value.</param>
    public void Add(TomlKey key, long value)
    {
      Add(key, new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a 32-bit integer value to the table using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The integer value.</param>
    public void Add(TomlKey key, int value)
    {
      Add(key, new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a 16-bit integer value to the table using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The short integer value.</param>
    public void Add(TomlKey key, short value)
    {
      Add(key, new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds an 8-bit unsigned integer value to the table using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The byte value.</param>
    public void Add(TomlKey key, byte value)
    {
      Add(key, new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds an 8-bit signed integer value to the table using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The signed byte value.</param>
    public void Add(TomlKey key, sbyte value)
    {
      Add(key, new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a GUID value to the table using a <see cref="TomlKey"/> (stored as a string).
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The GUID value.</param>
    public void Add(TomlKey key, Guid value)
    {
      Add(key, new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a single-precision floating-point value to the table using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The float value.</param>
    public void Add(TomlKey key, float value)
    {
      Add(key, new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a double-precision floating-point value to the table using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The double value.</param>
    public void Add(TomlKey key, double value)
    {
      Add(key, new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a decimal value to the table using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The decimal value.</param>
    public void Add(TomlKey key, decimal value)
    {
      Add(key, new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Adds a datetime value to the table using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The datetime value.</param>
    public void Add(TomlKey key, DateTime value)
    {
      Add(key, new TomlPrimitiveValue(value));
    }

    /// <summary>
    /// Determines whether the table contains a key.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <returns><c>true</c> if the table contains the specified key; otherwise, <c>false</c>.</returns>
    public bool ContainsKey(TomlKey key)
    {
      return _innerDictionary.ContainsKey(key);
    }

    /// <summary>
    /// Determines whether the table contains a key.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <returns><c>true</c> if the table contains the specified key; otherwise, <c>false</c>.</returns>
    public bool ContainsKey(string key)
    {
      return ContainsKey(new TomlKey(key));
    }

    /// <summary>
    /// Removes a key-value pair from the table.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <returns><c>true</c> if the key was found and removed; otherwise, <c>false</c>.</returns>
    public bool Remove(TomlKey key)
    {
      return _innerDictionary.Remove(key);
    }

    /// <summary>
    /// Removes a key-value pair from the table.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <returns><c>true</c> if the key was found and removed; otherwise, <c>false</c>.</returns>
    public bool Remove(string key)
    {
      return _innerDictionary.Remove(new TomlKey(key));
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <param name="value">The value associated with the key, if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the key is found; otherwise, <c>false</c>.</returns>
    public bool TryGetValue(TomlKey key,
#if NET9_0_OR_GREATER
                          [MaybeNullWhen(false)]
#endif //NET9_0_OR_GREATER
                            out TomlToken value)
    {
      return _innerDictionary.TryGetValue(key,  out value);
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key using a <see cref="TomlKey"/>.
    /// </summary>
    /// <param name="key">The key as a <see cref="TomlKey"/> object.</param>
    /// <returns>The value associated with the key.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the key is not found when getting.</exception>
    public override TomlToken this[TomlKey key]
    {
      get => _innerDictionary[key];
      set => _innerDictionary[key] = value;
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key using a string key.
    /// </summary>
    /// <param name="key">The key as a string.</param>
    /// <returns>The value associated with the key.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the key is not found when getting.</exception>
    public override TomlToken this[string key]
    {
      get => _innerDictionary[new TomlKey(key)];
      set => _innerDictionary[new TomlKey(key)] = value;
    }

    /// <summary>
    /// Gets the collection of keys in the table.
    /// </summary>
    public ICollection<TomlKey> Keys => _innerDictionary.Keys;

    /// <summary>
    /// Gets the collection of values in the table.
    /// </summary>
    public ICollection<TomlToken> Values => _innerDictionary.Values;

    /// <inheritdoc />
    protected override IEnumerable<TomlToken> GetChildren()
    {
      return Values;
    }

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element.</param>
    /// <returns>The value at the specified index.</returns>
    public override TomlToken this[int index]
    {
      get => _innerDictionary.Values.ElementAt(index);
      set => _innerDictionary[_innerDictionary.Keys.ElementAt(index)] = value;
    }

    /// <inheritdoc />
    public override string ToString()
    {
      return $"{nameof(Name)}: {Name}, {nameof(HasTopLevelDeclaration)}: {HasTopLevelDeclaration}, {nameof(HasTopLevelDefinition)}: {HasTopLevelDefinition}, {nameof(HasFromKeyDefinition)}: {HasFromKeyDefinition}, {nameof(HasFromInlineTableDefinition)}: {HasFromInlineTableDefinition}, {nameof(IsArrayOfTablesMember)}: {IsArrayOfTablesMember}, {nameof(Count)}: {Count}";
    }

    internal void AddInternal(TomlKey key, TomlToken value)
    {
      AddInternal(new KeyValuePair<TomlKey, TomlToken>(key, value));
    }

    internal void AddInternal(TomlTable tomlTable)
    {
      AddInternal(tomlTable.FullName, tomlTable);
    }

    internal void AddInternal(KeyValuePair<TomlKey, TomlToken> keyValue)
    {
      _innerDictionary.Add(keyValue);
    }

    internal bool IsUntaggedTable() => !HasTopLevelDefinition && !HasFromKeyDefinition && !HasFromInlineTableDefinition && !IsArrayOfTablesMember;

    /// <summary>
    /// Propagates <see cref="HasFromKeyDefinition"/>, <see cref="HasTopLevelDefinition"/> and <see cref="HasFromInlineTableDefinition"/>
    /// tags to <paramref name="tomlTable"/>.
    /// </summary>
    protected virtual void PropagateTagsToInnerTable(TomlTable tomlTable)
    {
      tomlTable.HasFromKeyDefinition = HasFromKeyDefinition;
      tomlTable.HasTopLevelDefinition = HasTopLevelDefinition;
      tomlTable.HasFromInlineTableDefinition = HasFromInlineTableDefinition;
    }

    /// <summary>
    /// Propagates <see cref="HasFromKeyDefinition"/>, <see cref="HasTopLevelDefinition"/> and <see cref="HasFromInlineTableDefinition"/>
    /// tags to all currently 'unresolved' tables.
    /// </summary>
    protected virtual void PropagateTagsToUnresolvedTables()
    {
      if ((_unresolvedInnerTables == null || _unresolvedInnerTables.Count == 0))
      {
        return;
      }

      foreach (var unresolvedInnerTable in _unresolvedInnerTables)
      {
        PropagateTagsToInnerTable(unresolvedInnerTable.Value);
      }

      _unresolvedInnerTables.Clear();
    }
  }
}