namespace RStein.TOML.Test;

[TestFixture]
internal class TomlWriterTest
{
  private static readonly string NL = Environment.NewLine;

  [Test]
  public void Ctor_When_TextWriter_Is_Null_Then_Throws_ArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(() => new TomlWriter(null!));
  }

  [Test]
  public void LastState_When_Created_Then_Is_Idle()
  {
    var writer = new TomlWriter(new StringWriter());

    Assert.That(writer.LastState, Is.EqualTo(TomlWriterState.Idle));
  }

  [Test]
  public void Comment_When_Written_Sync_Then_Text_Is_Written_Once_And_State_Is_Restored()
  {
    var comment = new TomlComment(" text");
    var stringWriter = new StringWriter();
    var writer = new TomlWriter(stringWriter);
    writer.BeginTomlDocument();

    writer.BeginWriteComment(comment);
    Assert.That(writer.LastState, Is.EqualTo(TomlWriterState.WritingComment));
    writer.EndWriteComment(comment);

    Assert.That(stringWriter.ToString(), Is.EqualTo("#  text"));
    Assert.That(writer.LastState, Is.EqualTo(TomlWriterState.WritingTomlDocument));
  }

  [Test]
  public async Task Comment_When_Written_Async_Then_Text_Is_Written_Once_And_State_Is_Restored()
  {
    var comment = new TomlComment(" text");
    var stringWriter = new StringWriter();
    var writer = new TomlWriter(stringWriter);
    await writer.BeginTomlDocumentAsync().ConfigureAwait(false);

    await writer.BeginWriteCommentAsync(comment).ConfigureAwait(false);
    Assert.That(writer.LastState, Is.EqualTo(TomlWriterState.WritingComment));
    await writer.EndWriteCommentAsync(comment).ConfigureAwait(false);

    Assert.That(stringWriter.ToString(), Is.EqualTo("#  text"));
    Assert.That(writer.LastState, Is.EqualTo(TomlWriterState.WritingTomlDocument));
  }

  [Test]
  public void Key_When_Unquoted_Then_Written_Raw_On_New_Line()
  {
    var text = writeSync(writer => writeKeyValue(writer, new TomlKey("key"), new TomlPrimitiveValue(1)));

    Assert.That(text, Is.EqualTo(NL + "key = 1"));
  }

  [Test]
  public void Key_When_Requires_Quotes_Then_Written_Quoted()
  {
    var text = writeSync(writer => writeKeyValue(writer, new TomlKey("a b"), new TomlPrimitiveValue(1)));

    Assert.That(text, Is.EqualTo(NL + "\"a b\" = 1"));
  }

  [Test]
  public void Key_When_Dotted_Then_Parts_Are_Separated_By_Dot()
  {
    var key = new TomlKey("a")
    {
      NextKeyPart = new TomlKey("b")
    };

    var text = writeSync(writer => writeKeyValue(writer, key, new TomlPrimitiveValue(1)));

    Assert.That(text, Is.EqualTo(NL + "a.b = 1"));
  }

  [TestCase(42L, "42")]
  [TestCase(true, "true")]
  [TestCase(false, "false")]
  [TestCase(1.5d, "1.5")]
  [TestCase(3d, "3.0")]
  [TestCase(double.PositiveInfinity, "inf")]
  [TestCase(double.NegativeInfinity, "-inf")]
  [TestCase(double.NaN, "nan")]
  public async Task PrimitiveValue_When_Written_Then_Sync_And_Async_Output_Is_Expected(object value, string expected)
  {
    var primitive = value switch
    {
      long longValue => new TomlPrimitiveValue(longValue),
      bool boolValue => new TomlPrimitiveValue(boolValue),
      double doubleValue => new TomlPrimitiveValue(doubleValue),
      _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    var syncText = writeSync(writer => writeValue(writer, primitive));
    var asyncText = await writeAsync(writer => writeValueAsync(writer, primitive)).ConfigureAwait(false);

    Assert.That(syncText, Is.EqualTo(expected));
    Assert.That(asyncText, Is.EqualTo(expected));
  }

  [TestCase(TomlStringValueType.BasicString, "a\"b", "\"a\\\"b\"")]
  [TestCase(TomlStringValueType.LiteralString, "a\\b", "'a\\b'")]
  [TestCase(TomlStringValueType.BasicMlString, "ab", "\"\"\"ab\"\"\"")]
  [TestCase(TomlStringValueType.LiteralMlString, "ab", "'''ab'''")]
  public async Task StringValue_When_Written_Then_Sync_And_Async_Output_Is_Expected(TomlStringValueType type,
                                                                                    string value,
                                                                                    string expected)
  {
    var primitive = new TomlPrimitiveValue(value, type);

    var syncText = writeSync(writer => writeValue(writer, primitive));
    var asyncText = await writeAsync(writer => writeValueAsync(writer, primitive)).ConfigureAwait(false);

    Assert.That(syncText, Is.EqualTo(expected));
    Assert.That(asyncText, Is.EqualTo(expected));
  }

  [Test]
  public async Task Table_When_Written_Then_Sync_And_Async_Output_Is_Expected()
  {
    var expected = NL + "[table]" + NL;
    var table = new TomlTable("table");
    var key = new TomlKey("table");

    var syncText = writeSync(writer =>
    {
      writer.BeginWriteTable(table, key);
      writer.EndWriteTable(table);
    });
    var asyncText = await writeAsync(async writer =>
    {
      await writer.BeginWriteTableAsync(table, key).ConfigureAwait(false);
      await writer.EndWriteTableAsync(table).ConfigureAwait(false);
    }).ConfigureAwait(false);

    Assert.That(syncText, Is.EqualTo(expected));
    Assert.That(asyncText, Is.EqualTo(expected));
  }

  [Test]
  public async Task ArrayOfTables_When_Written_Then_Sync_And_Async_Output_Is_Expected()
  {
    var expected = NL + "[[items]]" + NL;
    var arrayOfTables = new TomlArrayOfTables("items");
    var key = new TomlKey("items");

    var syncText = writeSync(writer =>
    {
      writer.BeginWriteArrayOfTables(arrayOfTables, key);
      writer.EndWriteArrayOfTables(arrayOfTables);
    });
    var asyncText = await writeAsync(async writer =>
    {
      await writer.BeginWriteArrayOfTablesAsync(arrayOfTables, key).ConfigureAwait(false);
      await writer.EndWriteArrayOfTablesAsync(arrayOfTables).ConfigureAwait(false);
    }).ConfigureAwait(false);

    Assert.That(syncText, Is.EqualTo(expected));
    Assert.That(asyncText, Is.EqualTo(expected));
  }

  [Test]
  public async Task Array_When_Written_Then_Sync_And_Async_Output_Is_Expected()
  {
    var expected = "[1, 2, ]";
    var array = new TomlArray();
    TomlPrimitiveValue[] items = {new TomlPrimitiveValue(1), new TomlPrimitiveValue(2)};

    var syncText = writeSync(writer =>
    {
      writer.BeginWriteArray(array);
      foreach (var item in items)
      {
        writer.BeginWriteArrayItem(item);
        writer.BeginWritePrimitiveValue(item);
        writer.EndWritePrimitiveValue(item);
        writer.EndWriteArrayItem(item);
      }

      writer.EndWriteArray(array);
    });
    var asyncText = await writeAsync(async writer =>
    {
      await writer.BeginWriteArrayAsync(array).ConfigureAwait(false);
      foreach (var item in items)
      {
        await writer.BeginWriteArrayItemAsync(item).ConfigureAwait(false);
        await writer.BeginWritePrimitiveValueAsync(item).ConfigureAwait(false);
        await writer.EndWritePrimitiveValueAsync(item).ConfigureAwait(false);
        await writer.EndWriteArrayItemAsync(item).ConfigureAwait(false);
      }

      await writer.EndWriteArrayAsync(array).ConfigureAwait(false);
    }).ConfigureAwait(false);

    Assert.That(syncText, Is.EqualTo(expected));
    Assert.That(asyncText, Is.EqualTo(expected));
  }

  [Test]
  public async Task InlineTable_When_Written_Then_Sync_And_Async_Output_Is_Expected()
  {
    var expected = "{a = 1, b = 2}";
    var inlineTable = new TomlInlineTable();
    var first = new KeyValuePair<TomlKey, TomlToken>(new TomlKey("a"), new TomlPrimitiveValue(1));
    var second = new KeyValuePair<TomlKey, TomlToken>(new TomlKey("b"), new TomlPrimitiveValue(2));

    var syncText = writeSync(writer =>
    {
      writer.BeginWriteInlineTable(inlineTable);
      writeInlineItem(writer, first, true);
      writeInlineItem(writer, second, false);
      writer.EndWriteInlineTable(inlineTable);
    });
    var asyncText = await writeAsync(async writer =>
    {
      await writer.BeginWriteInlineTableAsync(inlineTable).ConfigureAwait(false);
      await writeInlineItemAsync(writer, first, true).ConfigureAwait(false);
      await writeInlineItemAsync(writer, second, false).ConfigureAwait(false);
      await writer.EndWriteInlineTableAsync(inlineTable).ConfigureAwait(false);
    }).ConfigureAwait(false);

    Assert.That(syncText, Is.EqualTo(expected));
    Assert.That(asyncText, Is.EqualTo(expected));
  }

  [Test]
  public void HasState_When_State_Is_On_Stack_Then_Returns_True()
  {
    var writer = new TomlWriter(new StringWriter());
    writer.BeginTomlDocument();
    writer.BeginWriteInlineTable(new TomlInlineTable());

    Assert.That(writer.HasState(TomlWriterState.WritingInlineTable), Is.True);
    Assert.That(writer.HasState(TomlWriterState.WritingTomlDocument), Is.True);
    Assert.That(writer.HasState(TomlWriterState.WritingArray), Is.False);
  }

  private static string writeSync(Action<TomlWriter> action)
  {
    var stringWriter = new StringWriter();
    var writer = new TomlWriter(stringWriter);
    writer.BeginTomlDocument();
    action(writer);
    return stringWriter.ToString();
  }

  private static async Task<string> writeAsync(Func<TomlWriter, ValueTask> action)
  {
    var stringWriter = new StringWriter();
    var writer = new TomlWriter(stringWriter);
    await writer.BeginTomlDocumentAsync().ConfigureAwait(false);
    await action(writer).ConfigureAwait(false);
    return stringWriter.ToString();
  }

  private static void writeKeyValue(TomlWriter writer, TomlKey key, TomlPrimitiveValue value)
  {
    writer.BeginWriteKey(key);
    writer.EndWriteKey(key);
    writeValue(writer, value);
  }

  private static void writeValue(TomlWriter writer, TomlPrimitiveValue value)
  {
    writer.BeginWritePrimitiveValue(value);
    writer.EndWritePrimitiveValue(value);
  }

  private static async ValueTask writeValueAsync(TomlWriter writer, TomlPrimitiveValue value)
  {
    await writer.BeginWritePrimitiveValueAsync(value).ConfigureAwait(false);
    await writer.EndWritePrimitiveValueAsync(value).ConfigureAwait(false);
  }

  private static void writeInlineItem(TomlWriter writer, KeyValuePair<TomlKey, TomlToken> item, bool isFirstItem)
  {
    writer.BeginWriteInlineTableItem(item, isFirstItem);
    writer.BeginWriteKey(item.Key);
    writer.EndWriteKey(item.Key);
    writeValue(writer, (TomlPrimitiveValue) item.Value);
    writer.EndWriteInlineTableItem(item, isFirstItem);
  }

  private static async ValueTask writeInlineItemAsync(TomlWriter writer,
                                                      KeyValuePair<TomlKey, TomlToken> item,
                                                      bool isFirstItem)
  {
    await writer.BeginWriteInlineTableItemAsync(item, isFirstItem).ConfigureAwait(false);
    await writer.BeginWriteKeyAsync(item.Key).ConfigureAwait(false);
    await writer.EndWriteKeyAsync(item.Key).ConfigureAwait(false);
    await writeValueAsync(writer, (TomlPrimitiveValue) item.Value).ConfigureAwait(false);
    await writer.EndWriteInlineTableItemAsync(item, isFirstItem).ConfigureAwait(false);
  }
}
