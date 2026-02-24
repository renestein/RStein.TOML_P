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
    private readonly Stack<TomlWriterState> _tomlWriterState;

    public TomlWriter(TextWriter streamWriter)
    {
      _textWriter = streamWriter ?? throw new ArgumentNullException(nameof(streamWriter));
      _tomlWriterState = new Stack<TomlWriterState>();
      _tomlWriterState.Push(TomlWriterState.Idle);
    }

    public TomlWriterState LastState => _tomlWriterState.Peek();

    public bool HasState(TomlWriterState tomlWriterState)
    {
      return _tomlWriterState.Any(state => state == tomlWriterState);
    }

    public ValueTask<TomlWriter> BeginTomlDocumentAsync()
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.Idle);
      _tomlWriterState.Push(TomlWriterState.WritingTomlDocument);
      return new ValueTask<TomlWriter>(this);
    }

    public async ValueTask<TomlWriter> BeginWriteKeyAsync(TomlKey tomlKey)
    {
      var tomlWriterState = _tomlWriterState.Peek();
      Debug.Assert(tomlWriterState == TomlWriterState.WritingTomlDocument ||
                  tomlWriterState == TomlWriterState.WritingTable ||
                  tomlWriterState == TomlWriterState.WritingInlineTableItem ||
                  tomlWriterState == TomlWriterState.WritingArrayOfTables);

      if (tomlWriterState != TomlWriterState.WritingInlineTableItem)
      {
        await _textWriter.WriteLineAsync().ConfigureAwait(false);
      }

      await writeKeyAsync(tomlKey).ConfigureAwait(false);
      await _textWriter.WriteAsync(" = ").ConfigureAwait(false);
      _tomlWriterState.Push(TomlWriterState.WritingKey);
      return this;
    }

    public ValueTask<TomlWriter> EndWriteKeyAsync(TomlKey tomlKey)
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.WritingKey);
      _tomlWriterState.Pop();
      return new ValueTask<TomlWriter>(this);
    }

    public async ValueTask<TomlWriter> BeginWriteKeyAsync(string tomlKey, TomlDataType tomlDataType)
    {
      var tomlWriterState = _tomlWriterState.Peek();
      Debug.Assert(tomlWriterState == TomlWriterState.WritingTomlDocument ||
                   tomlWriterState == TomlWriterState.WritingTable ||
                   tomlWriterState == TomlWriterState.WritingInlineTableItem);

      if (tomlWriterState != TomlWriterState.WritingInlineTableItem)
      {
        await _textWriter.WriteLineAsync().ConfigureAwait(false);
      }


      await writeStringAsync(tomlKey, tomlDataType).ConfigureAwait(false);
      await _textWriter.WriteAsync(" = ").ConfigureAwait(false);
      _tomlWriterState.Push(TomlWriterState.WritingKey);
      return this;
    }

    public ValueTask<TomlWriter> EndWriteKeyAsync(string tomlKey)
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.WritingKey);
      _tomlWriterState.Pop();
      return new ValueTask<TomlWriter>(this);
    }

    public async ValueTask<TomlWriter> BeginWritePrimitiveValueAsync(TomlPrimitiveValue tomlPrimitiveValue)
    {
      var tomlWriterState = _tomlWriterState.Peek();
      Debug.Assert(tomlWriterState == TomlWriterState.WritingTomlDocument ||
                   tomlWriterState == TomlWriterState.WritingTable ||
                   tomlWriterState == TomlWriterState.WritingInlineTableItem ||
                   tomlWriterState == TomlWriterState.WritingArrayItem ||
                   tomlWriterState == TomlWriterState.WritingArrayOfTables);

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
      _tomlWriterState.Push(TomlWriterState.WritingPrimitiveValue);
      return this;
    }

    public ValueTask<TomlWriter> EndWritePrimitiveValueAsync(TomlPrimitiveValue  tomlPrimitiveValue)
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.WritingPrimitiveValue);
      _tomlWriterState.Pop();
      return new ValueTask<TomlWriter>(this);
    }

    public async ValueTask<TomlWriter> BeginWriteTableAsync(TomlTable tomlTable,
                                                            TomlKey tableKey)
    {
      var tomlWriterState = _tomlWriterState.Peek();
      Debug.Assert(tomlWriterState == TomlWriterState.WritingTomlDocument ||
                   tomlWriterState == TomlWriterState.WritingArrayOfTables);
      await _textWriter.WriteLineAsync().ConfigureAwait(false);
      await _textWriter.WriteAsync('[').ConfigureAwait(false);
      await writeKeyAsync(tableKey).ConfigureAwait(false);
      await _textWriter.WriteAsync(']').ConfigureAwait(false);
      _tomlWriterState.Push(TomlWriterState.WritingTable);
      return this;
    }

    public async ValueTask<TomlWriter> EndWriteTableAsync(TomlTable tomlTable)
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.WritingTable);
      await _textWriter.WriteLineAsync().ConfigureAwait(false);
      _tomlWriterState.Pop();
      return this;
    }

    public async ValueTask<TomlWriter> BeginWriteInlineTableAsync(TomlTable tomlTable)
    {
      var tomlWriterState = _tomlWriterState.Peek();
      Debug.Assert(tomlWriterState == TomlWriterState.WritingTomlDocument ||
                   tomlWriterState == TomlWriterState.WritingTable ||
                   tomlWriterState == TomlWriterState.WritingInlineTableItem ||
                   tomlWriterState == TomlWriterState.WritingArrayItem ||
                   tomlWriterState == TomlWriterState.WritingArrayOfTables);
      await _textWriter.WriteAsync('{').ConfigureAwait(false);
      _tomlWriterState.Push(TomlWriterState.WritingInlineTable);
      return this;
    }

    public async ValueTask<TomlWriter> EndWriteInlineTableAsync(TomlTable tomlTable)
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.WritingInlineTable);
      await _textWriter.WriteAsync('}').ConfigureAwait(false);
      _tomlWriterState.Pop();
      return this;
    }

    public async ValueTask<TomlWriter> BeginWriteArrayAsync(TomlArray tomlArray)
    {
      var tomlWriterState = _tomlWriterState.Peek();
      Debug.Assert(tomlWriterState == TomlWriterState.WritingTomlDocument || tomlWriterState == TomlWriterState.WritingTable || tomlWriterState == TomlWriterState.WritingArrayItem || tomlWriterState == TomlWriterState.WritingInlineTableItem);
      await _textWriter.WriteAsync('[').ConfigureAwait(false);
      _tomlWriterState.Push(TomlWriterState.WritingArray);
      return this;
    }

    public async ValueTask<TomlWriter> EndWriteArrayAsync(TomlArray tomlArray)
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.WritingArray);
      await _textWriter.WriteAsync(']').ConfigureAwait(false);
      _tomlWriterState.Pop();
      return this;
    }

    public async ValueTask<TomlWriter> WriteAsync(string text)
    {
      await _textWriter.WriteAsync(text).ConfigureAwait(false);
      return this;
    }

    public async ValueTask<TomlWriter> WriteLineAsync(string text)
    {
      await _textWriter.WriteLineAsync(text).ConfigureAwait(false);
      return this;
    }

    public async ValueTask<TomlWriter> WriteLineAsync()
    {
      await _textWriter.WriteLineAsync().ConfigureAwait(false);
      return this;
    }

    public async ValueTask<TomlWriter> BeginWriteArrayOfTablesAsync(TomlArrayOfTables table,
                                                                    TomlKey fullKey)
    {
      var tomlWriterState = _tomlWriterState.Peek();
      Debug.Assert(tomlWriterState == TomlWriterState.WritingTomlDocument || tomlWriterState == TomlWriterState.WritingTable || tomlWriterState == TomlWriterState.WritingArrayOfTables);

      if (tomlWriterState != TomlWriterState.Idle)
      {
        await _textWriter.WriteLineAsync().ConfigureAwait(false);
      }

      await _textWriter.WriteAsync("[[").ConfigureAwait(false);
      await writeKeyAsync(fullKey).ConfigureAwait(false);
      await _textWriter.WriteAsync("]]").ConfigureAwait(false);
      _tomlWriterState.Push(TomlWriterState.WritingArrayOfTables);
      return this;
    }

    public async ValueTask<TomlWriter> EndWriteArrayOfTablesAsync(TomlArrayOfTables table)
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.WritingArrayOfTables);
      await _textWriter.WriteLineAsync().ConfigureAwait(false);
      _tomlWriterState.Pop();
      return this;
    }

    public ValueTask<TomlWriter> EndTomlDocumentAsync()
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.WritingTomlDocument);
      _tomlWriterState.Pop();
      return new ValueTask<TomlWriter>(this);
    }

    private async ValueTask writeKeyAsync(TomlKey key)
    {
      var currentKey = key;
      do
      {
        await writeKeyPartAsync(currentKey).ConfigureAwait(false);
        currentKey = currentKey.NextKeyPart;

      } while (currentKey != null);
    }

    private async Task writeKeyPartAsync(TomlKey key)
    {
      await writeStringAsync(key.RawKey, (TomlDataType) key.Type).ConfigureAwait(false);
      if (requiresDotAfterKeyPart(key))
      {
        await _textWriter.WriteAsync(".").ConfigureAwait(false);
      }
    }

    private static bool requiresDotAfterKeyPart(TomlKey key)
    {
      return key.NextKeyPart != null;
    }

    private static (string? escaped, bool needsEscape) tryEscapeChar(char c)
    {
      if (!shouldEscapeChar(c))
      {
        return (null, false);
      }

      switch (c)
      {
        case '\t':
          return (@"\t", true);
        case '\n':
          return (@"\n", true);
        case '\f':
          return (@"\f", true);
        case '\r':
          return (@"\r", true);
        case '"':
          return (@"\""", true);
        case '\\':
          return (@"\\", true);
        case '\b':
          return (@"\b", true);
        default:
          return ($"\\u{(ushort)c:X4}", true);
      }
    }

    private static bool shouldEscapeChar(char c) => char.IsControl(c) || c < ' ' || c == '"' || c == '\\';

    private async ValueTask writeStringAsync(string stringValue,
                                             TomlDataType tomlDataType)
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
          await _textWriter.WriteAsync($"\"\"\"{stringValue.ToEscapedTomlString()}\"\"\"").ConfigureAwait(false);
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
        await _textWriter.WriteAsync($"{((double)tomlPrimitiveValue!).ToString("e15", CultureInfo.InvariantCulture)}").ConfigureAwait(false);
      }
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

    public async ValueTask BeginWriteCommentAsync(TomlComment tomlComment)
    {
      _tomlWriterState.Push(TomlWriterState.WritingComment);
      await _textWriter.WriteAsync($"# {tomlComment.Value}").ConfigureAwait(false);
    }

    public async ValueTask EndWriteCommentAsync(TomlComment tomlComment)
    {
      Debug.Assert(_tomlWriterState.Pop() == TomlWriterState.WritingComment);
      await _textWriter.WriteAsync($"# {tomlComment.Value}").ConfigureAwait(false);
      _tomlWriterState.Pop();
    }

    public ValueTask<TomlWriter> BeginWriteArrayItemAsync(TomlValue item)
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.WritingArray);
      _tomlWriterState.Push(TomlWriterState.WritingArrayItem);
      return new ValueTask<TomlWriter>(this);
    }

    public async ValueTask<TomlWriter> EndWriteArrayItemAsync(TomlValue item)
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.WritingArrayItem);
      await _textWriter.WriteAsync(", ").ConfigureAwait(false);
      _tomlWriterState.Pop();
      return this;
    }

    public async ValueTask<TomlWriter> BeginWriteInlineTableItemAsync(KeyValuePair<TomlKey, TomlToken> item, bool isFirstItem = false)
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.WritingInlineTable);
      _tomlWriterState.Push(TomlWriterState.WritingInlineTableItem);
      if (!isFirstItem)
      {
        await _textWriter.WriteAsync(", ").ConfigureAwait(false);
      }
      return this;
    }

    public ValueTask<TomlWriter> EndWriteInlineTableItemAsync(KeyValuePair<TomlKey, TomlToken> item,
                                                                         bool isFirstItem = false)
    {
      Debug.Assert(_tomlWriterState.Peek() == TomlWriterState.WritingInlineTableItem);
      _tomlWriterState.Pop();
      return new ValueTask<TomlWriter>(this);
    }
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