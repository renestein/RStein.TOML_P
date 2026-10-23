using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RStein.TOML
{
  internal class TomlWriter
  {
    private readonly TextWriter _textWriter;
    private int _inlineTableDepth;
    private int _tableScopeDepth;
#if DEBUG
    private readonly Stack<TomlWriterState> _tomlWriterState;
#endif

    public TomlWriter(TextWriter streamWriter)
    {
      _textWriter = streamWriter ?? throw new ArgumentNullException(nameof(streamWriter));
#if DEBUG
      _tomlWriterState = new Stack<TomlWriterState>();
#endif
      pushState(TomlWriterState.Idle);
    }

    public bool IsInsideInlineTable => _inlineTableDepth > 0;

    public bool IsAtDocumentLevel
    {
      get
      {
        assertIsAtDocumentLevelMatchesState();
        return _tableScopeDepth == 0;
      }
    }

#if DEBUG
    public TomlWriterState LastState => _tomlWriterState.Peek();

    public bool HasState(TomlWriterState tomlWriterState)
    {
      return _tomlWriterState.Any(state => state == tomlWriterState);
    }
#endif

    #region Async methods

    public ValueTask<TomlWriter> BeginTomlDocumentAsync()
    {
      assertState(TomlWriterState.Idle);
      pushState(TomlWriterState.WritingTomlDocument);
      return new ValueTask<TomlWriter>(this);
    }

    public async ValueTask<TomlWriter> BeginWriteKeyAsync(TomlKey tomlKey)
    {
      Debug.Assert(tomlKey != null);
      assertStateIsOneOf(TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingTable,
                         TomlWriterState.WritingInlineTableItem,
                         TomlWriterState.WritingArrayOfTables);
      assertIsInsideInlineTableMatchesState();

      if (!IsInsideInlineTable)
      {
        await _textWriter.WriteLineAsync().ConfigureAwait(false);
      }

      await writeKeyAsync(tomlKey).ConfigureAwait(false);
      await _textWriter.WriteAsync(" = ").ConfigureAwait(false);
      pushState(TomlWriterState.WritingKey);
      return this;
    }

    public ValueTask<TomlWriter> EndWriteKeyAsync(TomlKey tomlKey)
    {
      Debug.Assert(tomlKey != null);
      assertState(TomlWriterState.WritingKey);
      popState();
      return new ValueTask<TomlWriter>(this);
    }

    public async ValueTask<TomlWriter> BeginWritePrimitiveValueAsync(TomlPrimitiveValue tomlPrimitiveValue)
    {
      Debug.Assert(tomlPrimitiveValue != null);
      assertStateIsOneOf(TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingTable,
                         TomlWriterState.WritingInlineTableItem,
                         TomlWriterState.WritingArrayItem,
                         TomlWriterState.WritingArrayOfTables);

      switch (tomlPrimitiveValue.Type)
      {
        case TomlValueType.String:
          await writeStringValueAsync(tomlPrimitiveValue).ConfigureAwait(false);
          break;
        case TomlValueType.Boolean:
          await writeBooleanValueAsync(tomlPrimitiveValue).ConfigureAwait(false);
          break;
        case TomlValueType.DateTime:
          await writeDateTimeValueAsync(tomlPrimitiveValue).ConfigureAwait(false);
          break;
        case TomlValueType.Float:
          await writeFloatValueAsync(tomlPrimitiveValue).ConfigureAwait(false);
          break;
        case TomlValueType.Integer:
          await writeIntegerValueAsync(tomlPrimitiveValue).ConfigureAwait(false);
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      pushState(TomlWriterState.WritingPrimitiveValue);
      return this;
    }

    public ValueTask<TomlWriter> EndWritePrimitiveValueAsync(TomlPrimitiveValue tomlPrimitiveValue)
    {
      assertState(TomlWriterState.WritingPrimitiveValue);
      popState();
      return new ValueTask<TomlWriter>(this);
    }

    public async ValueTask<TomlWriter> BeginWriteTableAsync(TomlTable tomlTable,
                                                            TomlKey tableKey)
    {
      Debug.Assert(tableKey != null);
      assertStateIsOneOf(TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingArrayOfTables);
      await _textWriter.WriteLineAsync().ConfigureAwait(false);
      await _textWriter.WriteAsync('[').ConfigureAwait(false);
      await writeKeyAsync(tableKey).ConfigureAwait(false);
      await _textWriter.WriteAsync(']').ConfigureAwait(false);
      _tableScopeDepth++;
      pushState(TomlWriterState.WritingTable);
      return this;
    }

    public async ValueTask<TomlWriter> EndWriteTableAsync(TomlTable tomlTable)
    {
      assertState(TomlWriterState.WritingTable);
      await _textWriter.WriteLineAsync().ConfigureAwait(false);
      _tableScopeDepth--;
      popState();
      return this;
    }

    public async ValueTask<TomlWriter> BeginWriteInlineTableAsync(TomlTable tomlTable)
    {
      assertStateIsOneOf(TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingTable,
                         TomlWriterState.WritingInlineTableItem,
                         TomlWriterState.WritingArrayItem,
                         TomlWriterState.WritingArrayOfTables);
      await _textWriter.WriteAsync('{').ConfigureAwait(false);
      _inlineTableDepth++;
      pushState(TomlWriterState.WritingInlineTable);
      return this;
    }

    public async ValueTask<TomlWriter> EndWriteInlineTableAsync(TomlTable tomlTable)
    {
      assertState(TomlWriterState.WritingInlineTable);
      await _textWriter.WriteAsync('}').ConfigureAwait(false);
      _inlineTableDepth--;
      popState();
      return this;
    }

    public async ValueTask<TomlWriter> BeginWriteArrayAsync(TomlArray tomlArray)
    {
      assertStateIsOneOf(TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingTable,
                         TomlWriterState.WritingArrayItem,
                         TomlWriterState.WritingInlineTableItem);
      await _textWriter.WriteAsync('[').ConfigureAwait(false);
      pushState(TomlWriterState.WritingArray);
      return this;
    }

    public async ValueTask<TomlWriter> EndWriteArrayAsync(TomlArray tomlArray)
    {
      assertState(TomlWriterState.WritingArray);
      await _textWriter.WriteAsync(']').ConfigureAwait(false);
      popState();
      return this;
    }

    public async ValueTask<TomlWriter> WriteAsync(string text)
    {
      assertStateIsNot(TomlWriterState.Idle);
      await _textWriter.WriteAsync(text).ConfigureAwait(false);
      return this;
    }

    public async ValueTask<TomlWriter> WriteLineAsync(string text)
    {
      assertStateIsNot(TomlWriterState.Idle);
      await _textWriter.WriteLineAsync(text).ConfigureAwait(false);
      return this;
    }

    public async ValueTask<TomlWriter> WriteLineAsync()
    {
      assertStateIsNot(TomlWriterState.Idle);
      await _textWriter.WriteLineAsync().ConfigureAwait(false);
      return this;
    }

    public async ValueTask<TomlWriter> BeginWriteArrayOfTablesAsync(TomlArrayOfTables table,
                                                                    TomlKey fullKey)
    {
      Debug.Assert(fullKey != null);
      assertStateIsOneOf(TomlWriterState.Idle,
                         TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingTable,
                         TomlWriterState.WritingArrayOfTables);

      await _textWriter.WriteLineAsync().ConfigureAwait(false);
      await _textWriter.WriteAsync("[[").ConfigureAwait(false);
      await writeKeyAsync(fullKey).ConfigureAwait(false);
      await _textWriter.WriteAsync("]]").ConfigureAwait(false);
      _tableScopeDepth++;
      pushState(TomlWriterState.WritingArrayOfTables);
      return this;
    }

    public async ValueTask<TomlWriter> EndWriteArrayOfTablesAsync(TomlArrayOfTables table)
    {
      assertState(TomlWriterState.WritingArrayOfTables);
      await _textWriter.WriteLineAsync().ConfigureAwait(false);
      _tableScopeDepth--;
      popState();
      return this;
    }

    public ValueTask<TomlWriter> EndTomlDocumentAsync()
    {
      assertState(TomlWriterState.WritingTomlDocument);
      popState();
      return new ValueTask<TomlWriter>(this);
    }

    public ValueTask<TomlWriter> BeginWriteArrayItemAsync(TomlValue item)
    {
      assertState(TomlWriterState.WritingArray);
      pushState(TomlWriterState.WritingArrayItem);
      return new ValueTask<TomlWriter>(this);
    }

    public async ValueTask<TomlWriter> EndWriteArrayItemAsync(TomlValue item)
    {
      assertState(TomlWriterState.WritingArrayItem);
      await _textWriter.WriteAsync(", ").ConfigureAwait(false);
      popState();
      return this;
    }

    public async ValueTask<TomlWriter> BeginWriteInlineTableItemAsync(KeyValuePair<TomlKey, TomlToken> item, bool isFirstItem = false)
    {
      assertState(TomlWriterState.WritingInlineTable);
      pushState(TomlWriterState.WritingInlineTableItem);
      if (!isFirstItem)
      {
        await _textWriter.WriteAsync(", ").ConfigureAwait(false);
      }
      return this;
    }

    public ValueTask<TomlWriter> EndWriteInlineTableItemAsync(KeyValuePair<TomlKey, TomlToken> item,
                                                              bool isFirstItem = false)
    {
      assertState(TomlWriterState.WritingInlineTableItem);
      popState();
      return new ValueTask<TomlWriter>(this);
    }

    public async ValueTask BeginWriteCommentAsync(TomlComment tomlComment)
    {
      Debug.Assert(tomlComment != null);
      assertCanWriteComment();
      pushState(TomlWriterState.WritingComment);
      await _textWriter.WriteAsync($"# {tomlComment.Value}").ConfigureAwait(false);
    }

    public ValueTask EndWriteCommentAsync(TomlComment tomlComment)
    {
      Debug.Assert(tomlComment != null);
      assertState(TomlWriterState.WritingComment);
      popState();
      return default;
    }

    #endregion

    #region Sync methods

    public void BeginTomlDocument()
    {
      assertState(TomlWriterState.Idle);
      pushState(TomlWriterState.WritingTomlDocument);
    }

    public void BeginWriteKey(TomlKey tomlKey)
    {
      Debug.Assert(tomlKey != null);
      assertStateIsOneOf(TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingTable,
                         TomlWriterState.WritingInlineTableItem,
                         TomlWriterState.WritingArrayOfTables);
      assertIsInsideInlineTableMatchesState();

      if (!IsInsideInlineTable)
      {
        _textWriter.WriteLine();
      }

      writeKey(tomlKey);
      _textWriter.Write(" = ");
      pushState(TomlWriterState.WritingKey);
    }

    public void EndWriteKey(TomlKey tomlKey)
    {
      Debug.Assert(tomlKey != null);
      assertState(TomlWriterState.WritingKey);
      popState();
    }

    public void BeginWritePrimitiveValue(TomlPrimitiveValue tomlPrimitiveValue)
    {
      Debug.Assert(tomlPrimitiveValue != null);
      assertStateIsOneOf(TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingTable,
                         TomlWriterState.WritingInlineTableItem,
                         TomlWriterState.WritingArrayItem,
                         TomlWriterState.WritingArrayOfTables);

      switch (tomlPrimitiveValue.Type)
      {
        case TomlValueType.String:
          writeStringValue(tomlPrimitiveValue);
          break;
        case TomlValueType.Boolean:
          writeBooleanValue(tomlPrimitiveValue);
          break;
        case TomlValueType.DateTime:
          writeDateTimeValue(tomlPrimitiveValue);
          break;
        case TomlValueType.Float:
          writeFloatValue(tomlPrimitiveValue);
          break;
        case TomlValueType.Integer:
          writeIntegerValue(tomlPrimitiveValue);
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      pushState(TomlWriterState.WritingPrimitiveValue);
    }

    public void EndWritePrimitiveValue(TomlPrimitiveValue tomlPrimitiveValue)
    {
      assertState(TomlWriterState.WritingPrimitiveValue);
      popState();
    }

    public void BeginWriteTable(TomlTable tomlTable, TomlKey tableKey)
    {
      Debug.Assert(tableKey != null);
      assertStateIsOneOf(TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingArrayOfTables);
      _textWriter.WriteLine();
      _textWriter.Write('[');
      writeKey(tableKey);
      _textWriter.Write(']');
      _tableScopeDepth++;
      pushState(TomlWriterState.WritingTable);
    }

    public void EndWriteTable(TomlTable tomlTable)
    {
      assertState(TomlWriterState.WritingTable);
      _textWriter.WriteLine();
      _tableScopeDepth--;
      popState();
    }

    public void BeginWriteInlineTable(TomlTable tomlTable)
    {
      assertStateIsOneOf(TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingTable,
                         TomlWriterState.WritingInlineTableItem,
                         TomlWriterState.WritingArrayItem,
                         TomlWriterState.WritingArrayOfTables);
      _textWriter.Write('{');
      _inlineTableDepth++;
      pushState(TomlWriterState.WritingInlineTable);
    }

    public void EndWriteInlineTable(TomlTable tomlTable)
    {
      assertState(TomlWriterState.WritingInlineTable);
      _textWriter.Write('}');
      _inlineTableDepth--;
      popState();
    }

    public void BeginWriteArray(TomlArray tomlArray)
    {
      assertStateIsOneOf(TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingTable,
                         TomlWriterState.WritingArrayItem,
                         TomlWriterState.WritingInlineTableItem);
      _textWriter.Write('[');
      pushState(TomlWriterState.WritingArray);
    }

    public void EndWriteArray(TomlArray tomlArray)
    {
      assertState(TomlWriterState.WritingArray);
      _textWriter.Write(']');
      popState();
    }

    public void Write(string text)
    {
      assertStateIsNot(TomlWriterState.Idle);
      _textWriter.Write(text);
    }

    public void WriteLine(string text)
    {
      assertStateIsNot(TomlWriterState.Idle);
      _textWriter.WriteLine(text);
    }

    public void WriteLine()
    {
      assertStateIsNot(TomlWriterState.Idle);
      _textWriter.WriteLine();
    }

    public void BeginWriteArrayOfTables(TomlArrayOfTables table, TomlKey fullKey)
    {
      Debug.Assert(fullKey != null);
      assertStateIsOneOf(TomlWriterState.Idle,
                         TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingTable,
                         TomlWriterState.WritingArrayOfTables);

      _textWriter.WriteLine();
      _textWriter.Write("[[");
      writeKey(fullKey);
      _textWriter.Write("]]");
      _tableScopeDepth++;
      pushState(TomlWriterState.WritingArrayOfTables);
    }

    public void EndWriteArrayOfTables(TomlArrayOfTables table)
    {
      assertState(TomlWriterState.WritingArrayOfTables);
      _textWriter.WriteLine();
      _tableScopeDepth--;
      popState();
    }

    public void EndTomlDocument()
    {
      assertState(TomlWriterState.WritingTomlDocument);
      popState();
    }

    public void BeginWriteArrayItem(TomlValue item)
    {
      assertState(TomlWriterState.WritingArray);
      pushState(TomlWriterState.WritingArrayItem);
    }

    public void EndWriteArrayItem(TomlValue item)
    {
      assertState(TomlWriterState.WritingArrayItem);
      _textWriter.Write(", ");
      popState();
    }

    public void BeginWriteInlineTableItem(KeyValuePair<TomlKey, TomlToken> item, bool isFirstItem = false)
    {
      assertState(TomlWriterState.WritingInlineTable);
      pushState(TomlWriterState.WritingInlineTableItem);
      if (!isFirstItem)
      {
        _textWriter.Write(", ");
      }
    }

    public void EndWriteInlineTableItem(KeyValuePair<TomlKey, TomlToken> item, bool isFirstItem = false)
    {
      assertState(TomlWriterState.WritingInlineTableItem);
      popState();
    }

    public void BeginWriteComment(TomlComment tomlComment)
    {
      Debug.Assert(tomlComment != null);
      assertCanWriteComment();
      pushState(TomlWriterState.WritingComment);
      _textWriter.Write($"# {tomlComment.Value}");
    }

    public void EndWriteComment(TomlComment tomlComment)
    {
      Debug.Assert(tomlComment != null);
      assertState(TomlWriterState.WritingComment);
      popState();
    }

    #endregion

    #region Private async helpers

    private async ValueTask writeKeyAsync(TomlKey key)
    {
      Debug.Assert(key != null);
      var currentKey = key;
      do
      {
        await writeKeyPartAsync(currentKey).ConfigureAwait(false);
        currentKey = currentKey.NextKeyPart;

      } while (currentKey != null);
    }

    private async ValueTask writeKeyPartAsync(TomlKey key)
    {
      await writeStringAsync(key.RawKey, (TomlDataType) key.Type).ConfigureAwait(false);
      if (requiresDotAfterKeyPart(key))
      {
        await _textWriter.WriteAsync(".").ConfigureAwait(false);
      }
    }

    private async ValueTask writeStringAsync(string stringValue, TomlDataType tomlDataType)
    {
      if (stringValue == null)
      {
        throw new ArgumentNullException(nameof(stringValue));
      }

      switch (tomlDataType)
      {
        case TomlDataType.BasicString:
          await _textWriter.WriteAsync($"\"{stringValue.ToEscapedTomlString()}\"").ConfigureAwait(false);
          break;
        case TomlDataType.BasicMlString:
          await _textWriter.WriteAsync($"\"\"\"{stringValue.ToEscapedTomlMlString()}\"\"\"").ConfigureAwait(false);
          break;
        case TomlDataType.LiteralString:
          await _textWriter.WriteAsync($"'{stringValue}'").ConfigureAwait(false);
          break;
        case TomlDataType.LiteralMlString:
          await _textWriter.WriteAsync($"'''{stringValue}'''").ConfigureAwait(false);
          break;
        case TomlDataType.RawString:
          await _textWriter.WriteAsync($"{stringValue}").ConfigureAwait(false);
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(tomlDataType));
      }
    }

    private async ValueTask writeIntegerValueAsync(TomlPrimitiveValue tomlPrimitiveValue)
    {
      await _textWriter.WriteAsync(tomlPrimitiveValue.Value).ConfigureAwait(false);
    }

    private async ValueTask writeFloatValueAsync(TomlPrimitiveValue tomlPrimitiveValue)
    {
      if (double.IsPositiveInfinity(((double) tomlPrimitiveValue!)))
      {
        await _textWriter.WriteAsync(TomlPrimitiveValue.SPECIAL_FLOAT_INF_LITERAL).ConfigureAwait(false);
      }
      else if (double.IsNegativeInfinity(((double) tomlPrimitiveValue!)))
      {
        await _textWriter.WriteAsync(TomlPrimitiveValue.SPECIAL_FLOAT_MINUS_INF_LITERAL).ConfigureAwait(false);
      }
      else if (double.IsNaN(((double) tomlPrimitiveValue!)))
      {
        await _textWriter.WriteAsync(TomlPrimitiveValue.SPECIAL_FLOAT_NAN_LITERAL).ConfigureAwait(false);
      }
      else
      {
        await _textWriter.WriteAsync(formatFloat((double) tomlPrimitiveValue!)).ConfigureAwait(false);
      }
    }

    private static string formatFloat(double value)
    {
      var text = value.ToString("R", CultureInfo.InvariantCulture);
      // TOML requires a fractional part and/or exponent; "3" would read back as an integer.
      return text.IndexOfAny(new[] { '.', 'E', 'e' }) < 0 ? text + ".0" : text;
    }

    private async ValueTask writeDateTimeValueAsync(TomlPrimitiveValue tomlPrimitiveValue)
    {
      await _textWriter.WriteAsync(tomlPrimitiveValue.Value).ConfigureAwait(false);
    }

    private async ValueTask writeBooleanValueAsync(TomlPrimitiveValue tomlPrimitiveValue)
    {
      await _textWriter.WriteAsync(tomlPrimitiveValue.Value).ConfigureAwait(false);
    }

    private async ValueTask writeStringValueAsync(TomlPrimitiveValue tomlPrimitiveValue)
    {
      switch (tomlPrimitiveValue.SubType)
      {
        case TomlDataType.LiteralMlString:
        case TomlDataType.LiteralString:
        case TomlDataType.BasicMlString:
        case TomlDataType.BasicString:
        case TomlDataType.RawString:
          await writeStringAsync(tomlPrimitiveValue.Value, tomlPrimitiveValue.SubType).ConfigureAwait(false);
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    #endregion

    #region Private sync helpers

    private void writeKey(TomlKey key)
    {
      Debug.Assert(key != null);
      var currentKey = key;
      do
      {
        writeKeyPart(currentKey);
        currentKey = currentKey.NextKeyPart;
      } while (currentKey != null);
    }

    private void writeKeyPart(TomlKey key)
    {
      writeString(key.RawKey, (TomlDataType) key.Type);
      if (requiresDotAfterKeyPart(key))
      {
        _textWriter.Write('.');
      }
    }

    private void writeString(string stringValue, TomlDataType tomlDataType)
    {
      if (stringValue == null)
      {
        throw new ArgumentNullException(nameof(stringValue));
      }

      switch (tomlDataType)
      {
        case TomlDataType.BasicString:
          _textWriter.Write($"\"{stringValue.ToEscapedTomlString()}\"");
          break;
        case TomlDataType.BasicMlString:
          _textWriter.Write($"\"\"\"{stringValue.ToEscapedTomlMlString()}\"\"\"");
          break;
        case TomlDataType.LiteralString:
          _textWriter.Write($"'{stringValue}'");
          break;
        case TomlDataType.LiteralMlString:
          _textWriter.Write($"'''{stringValue}'''");
          break;
        case TomlDataType.RawString:
          _textWriter.Write($"{stringValue}");
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(tomlDataType));
      }
    }

    private void writeIntegerValue(TomlPrimitiveValue tomlPrimitiveValue)
    {
      _textWriter.Write(tomlPrimitiveValue.Value);
    }

    private void writeFloatValue(TomlPrimitiveValue tomlPrimitiveValue)
    {
      if (double.IsPositiveInfinity(((double) tomlPrimitiveValue!)))
      {
        _textWriter.Write(TomlPrimitiveValue.SPECIAL_FLOAT_INF_LITERAL);
      }
      else if (double.IsNegativeInfinity(((double) tomlPrimitiveValue!)))
      {
        _textWriter.Write(TomlPrimitiveValue.SPECIAL_FLOAT_MINUS_INF_LITERAL);
      }
      else if (double.IsNaN(((double) tomlPrimitiveValue!)))
      {
        _textWriter.Write(TomlPrimitiveValue.SPECIAL_FLOAT_NAN_LITERAL);
      }
      else
      {
        _textWriter.Write(formatFloat((double) tomlPrimitiveValue!));
      }
    }

    private void writeDateTimeValue(TomlPrimitiveValue tomlPrimitiveValue)
    {
      _textWriter.Write(tomlPrimitiveValue.Value);
    }

    private void writeBooleanValue(TomlPrimitiveValue tomlPrimitiveValue)
    {
      _textWriter.Write(tomlPrimitiveValue.Value);
    }

    private void writeStringValue(TomlPrimitiveValue tomlPrimitiveValue)
    {
      switch (tomlPrimitiveValue.SubType)
      {
        case TomlDataType.LiteralMlString:
        case TomlDataType.LiteralString:
        case TomlDataType.BasicMlString:
        case TomlDataType.BasicString:
        case TomlDataType.RawString:
          writeString(tomlPrimitiveValue.Value, tomlPrimitiveValue.SubType);
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    #endregion

    #region Private helpers

    [Conditional("DEBUG")]
    private void pushState(TomlWriterState tomlWriterState)
    {
#if DEBUG
      _tomlWriterState.Push(tomlWriterState);
#endif
    }

    [Conditional("DEBUG")]
    private void popState()
    {
#if DEBUG
      _tomlWriterState.Pop();
#endif
    }

    [Conditional("DEBUG")]
    private void assertState(TomlWriterState expectedState)
    {
#if DEBUG
      Debug.Assert(_tomlWriterState.Peek() == expectedState);
#endif
    }

    [Conditional("DEBUG")]
    private void assertStateIsNot(TomlWriterState unexpectedState)
    {
#if DEBUG
      Debug.Assert(_tomlWriterState.Peek() != unexpectedState);
#endif
    }

    [Conditional("DEBUG")]
    private void assertStateIsOneOf(params TomlWriterState[] allowedStates)
    {
#if DEBUG
      Debug.Assert(Array.IndexOf(allowedStates, _tomlWriterState.Peek()) >= 0);
#endif
    }

    [Conditional("DEBUG")]
    private void assertCanWriteComment()
    {
      assertStateIsOneOf(TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingTable,
                         TomlWriterState.WritingArrayOfTables,
                         TomlWriterState.WritingInlineTable,
                         TomlWriterState.WritingInlineTableItem,
                         TomlWriterState.WritingArray,
                         TomlWriterState.WritingArrayItem);
    }

    [Conditional("DEBUG")]
    private void assertIsInsideInlineTableMatchesState()
    {
#if DEBUG
      Debug.Assert(IsInsideInlineTable == HasState(TomlWriterState.WritingInlineTable));
      Debug.Assert(IsInsideInlineTable == (_tomlWriterState.Peek() == TomlWriterState.WritingInlineTableItem));
#endif
    }

    [Conditional("DEBUG")]
    private void assertIsAtDocumentLevelMatchesState()
    {
#if DEBUG
      assertStateIsOneOf(TomlWriterState.Idle,
                         TomlWriterState.WritingTomlDocument,
                         TomlWriterState.WritingTable,
                         TomlWriterState.WritingArrayOfTables);
      var isDocumentLevelState = _tomlWriterState.Peek() == TomlWriterState.Idle ||
                                 _tomlWriterState.Peek() == TomlWriterState.WritingTomlDocument;
      Debug.Assert((_tableScopeDepth == 0) == isDocumentLevelState);
#endif
    }

    private static bool requiresDotAfterKeyPart(TomlKey key)
    {
      return key.NextKeyPart != null;
    }

    #endregion
  }

  internal enum TomlWriterState
  {
    Idle = 0,
    WritingTomlDocument,
    WritingKey,
    WritingPrimitiveValue,
    WritingArray,
    WritingTable,
    WritingArrayOfTables,
    WritingInlineTable,
    WritingComment,
    WritingArrayItem,
    WritingInlineTableItem
  }
}
