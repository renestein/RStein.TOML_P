namespace RStein.TOML.Test;

[TestFixture]
internal class TomlTokenTest
{

  [Test]
  public void Ctor_When_Token_Type_Is_Undefined_Then_Throws_ArgumentException()
  {
    Assert.Throws<ArgumentException>(() => _ = new UndefinedToken());
  }

  [TestCaseSource(nameof(leafTokens))]
  public void IntIndexer_When_Token_Has_No_Children_Then_Throws_InvalidOperationException(TomlToken token)
  {
    Assert.Throws<InvalidOperationException>(() => _ = token[0]);
    Assert.Throws<InvalidOperationException>(() => token[0] = 1);
  }

  [TestCaseSource(nameof(leafTokens))]
  public void StringIndexer_When_Token_Has_No_Children_Then_Throws_InvalidOperationException(TomlToken token)
  {
    Assert.Throws<InvalidOperationException>(() => _ = token["key"]);
    Assert.Throws<InvalidOperationException>(() => token["key"] = 1);
  }

  [TestCaseSource(nameof(leafTokens))]
  public void KeyIndexer_When_Token_Has_No_Children_Then_Throws_InvalidOperationException(TomlToken token)
  {
    var key = new TomlKey("key");

    Assert.Throws<InvalidOperationException>(() => _ = token[key]);
    Assert.Throws<InvalidOperationException>(() => token[key] = 1);
  }

  [TestCaseSource(nameof(leafTokens))]
  public void Tokens_When_Token_Has_No_Children_Then_Is_Empty(TomlToken token)
  {
    Assert.That(token.Tokens, Is.Empty);
    Assert.That(token.ToList(), Is.Empty);
  }

  [Test]
  public void Tokens_When_Array_Then_Returns_Elements()
  {
    var array = new TomlArray {1, "two"};

    Assert.That(array.Tokens.Count(), Is.EqualTo(2));
    Assert.That(array.Tokens.First(), Is.SameAs(array[0]));
  }

  [Test]
  public void Tokens_When_Table_Then_Returns_Values()
  {
    var table = new TomlTable {{"a", 1}, {"b", 2}};

    Assert.That(table.Tokens.Count(), Is.EqualTo(2));
  }

  [Test]
  public void ImplicitOperator_When_String_Then_Creates_String_Value()
  {
    TomlToken token = "text";

    Assert.That(((TomlPrimitiveValue) token).Type, Is.EqualTo(TomlValueType.String));
    Assert.That((string?) token, Is.EqualTo("text"));
  }

  [Test]
  public void ImplicitOperator_When_String_Is_Null_Then_Throws_ArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(() =>
    {
      TomlToken _ = (string) null!;
    });
  }

  [Test]
  public void ImplicitOperator_When_Integer_Types_Then_Creates_Integer_Values()
  {
    TomlToken fromLong = 1L;
    TomlToken fromInt = 2;
    TomlToken fromShort = (short) 3;
    TomlToken fromByte = (byte) 4;
    TomlToken fromSByte = (sbyte) 5;

    Assert.Multiple(() =>
    {
      Assert.That(((TomlPrimitiveValue) fromLong).Type, Is.EqualTo(TomlValueType.Integer));
      Assert.That(((TomlPrimitiveValue) fromInt).Type, Is.EqualTo(TomlValueType.Integer));
      Assert.That(((TomlPrimitiveValue) fromShort).Type, Is.EqualTo(TomlValueType.Integer));
      Assert.That(((TomlPrimitiveValue) fromByte).Type, Is.EqualTo(TomlValueType.Integer));
      Assert.That(((TomlPrimitiveValue) fromSByte).Type, Is.EqualTo(TomlValueType.Integer));
      Assert.That((long?) fromLong, Is.EqualTo(1L));
      Assert.That((int?) fromInt, Is.EqualTo(2));
      Assert.That((short?) fromShort, Is.EqualTo((short) 3));
      Assert.That((byte?) fromByte, Is.EqualTo((byte) 4));
      Assert.That((sbyte?) fromSByte, Is.EqualTo((sbyte) 5));
    });
  }

  [Test]
  public void ImplicitOperator_When_Bool_Then_Creates_Boolean_Value()
  {
    TomlToken token = true;

    Assert.That(((TomlPrimitiveValue) token).Type, Is.EqualTo(TomlValueType.Boolean));
    Assert.That((bool?) token, Is.True);
  }

  [Test]
  public void ImplicitOperator_When_Floating_Point_Types_Then_Creates_Float_Values()
  {
    TomlToken fromDouble = 1.5;
    TomlToken fromDecimal = 2.5m;

    Assert.That(((TomlPrimitiveValue) fromDouble).Type, Is.EqualTo(TomlValueType.Float));
    Assert.That(((TomlPrimitiveValue) fromDecimal).Type, Is.EqualTo(TomlValueType.Float));
    Assert.That((double?) fromDouble, Is.EqualTo(1.5));
    Assert.That((decimal?) fromDecimal, Is.EqualTo(2.5m));
  }

  [Test]
  public void ImplicitOperator_When_Guid_Then_Creates_String_Value()
  {
    var guid = Guid.NewGuid();

    TomlToken token = guid;

    Assert.That(((TomlPrimitiveValue) token).Type, Is.EqualTo(TomlValueType.String));
    Assert.That((Guid?) token, Is.EqualTo(guid));
  }

  [Test]
  public void ImplicitOperator_When_DateTime_Then_Creates_DateTime_Value()
  {
    var dateTime = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

    TomlToken token = dateTime;

    Assert.That(((TomlPrimitiveValue) token).Type, Is.EqualTo(TomlValueType.DateTime));
    Assert.That((DateTime?) token, Is.EqualTo(dateTime));
  }

  [Test]
  public void ImplicitOperator_When_DateOnly_Then_Creates_Local_Date_Value()
  {
    var date = new DateOnly(2024, 5, 6);

    TomlToken token = date;

    Assert.That(((TomlPrimitiveValue) token).Type, Is.EqualTo(TomlValueType.DateTime));
    Assert.That((DateOnly?) token, Is.EqualTo(date));
  }

  [Test]
  public void ImplicitOperator_When_TimeOnly_Then_Creates_Local_Time_Value()
  {
    var time = new TimeOnly(7, 8, 9);

    TomlToken token = time;

    Assert.That(((TomlPrimitiveValue) token).Type, Is.EqualTo(TomlValueType.DateTime));
    Assert.That((TimeOnly?) token, Is.EqualTo(time));
  }

  [Test]
  public void ExplicitOperator_When_Token_Is_Null_Then_Returns_Null()
  {
    TomlToken? token = null;

    Assert.Multiple(() =>
    {
      Assert.That((string?) token, Is.Null);
      Assert.That((int?) token, Is.Null);
      Assert.That((short?) token, Is.Null);
      Assert.That((ushort?) token, Is.Null);
      Assert.That((long?) token, Is.Null);
      Assert.That((uint?) token, Is.Null);
      Assert.That((ulong?) token, Is.Null);
      Assert.That((double?) token, Is.Null);
      Assert.That((float?) token, Is.Null);
      Assert.That((decimal?) token, Is.Null);
      Assert.That((byte?) token, Is.Null);
      Assert.That((sbyte?) token, Is.Null);
      Assert.That((char?) token, Is.Null);
      Assert.That((bool?) token, Is.Null);
      Assert.That((Guid?) token, Is.Null);
      Assert.That((DateTime?) token, Is.Null);
      Assert.That((DateOnly?) token, Is.Null);
      Assert.That((TimeOnly?) token, Is.Null);
    });
  }

  [Test]
  public void ExplicitOperator_When_Token_Is_Array_Then_Throws_InvalidCastException()
  {
    assertAllExplicitCastsThrowInvalidCastException(new TomlArray {1});
  }

  [Test]
  public void ExplicitOperator_When_Token_Is_Table_Then_Throws_InvalidCastException()
  {
    assertAllExplicitCastsThrowInvalidCastException(new TomlTable {{"a", 1}});
  }

  [Test]
  public void ExplicitOperator_When_Token_Is_Comment_Then_Throws_InvalidCastException()
  {
    assertAllExplicitCastsThrowInvalidCastException(new TomlComment("c"));
  }

  [Test]
  public void ExplicitOperator_When_Ulong_Value_Is_Greater_Than_UInt_MaxValue_Then_Returns_Exact_Value()
  {
    TomlToken token = new TomlPrimitiveValue(ulong.MaxValue.ToString(), TomlValueType.Integer, TomlDataType.IntegerDec);

    Assert.That((ulong?) token, Is.EqualTo(ulong.MaxValue));
  }

  [Test]
  public void ExplicitOperator_When_Ulong_Value_Is_Hex_Then_Returns_Exact_Value()
  {
    TomlToken token = new TomlPrimitiveValue("0xFFFFFFFFFFFFFFFF", TomlValueType.Integer, TomlDataType.IntegerHex);

    Assert.That((ulong?) token, Is.EqualTo(ulong.MaxValue));
  }

  [Test]
  public async Task DeepEqualsAsync_When_Other_Is_Null_Then_Returns_False()
  {
    TomlToken token = new TomlPrimitiveValue(1);

    var result = await token.DeepEqualsAsync(null).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Primitive_Values_Are_Equal_Then_Returns_True()
  {
    TomlToken first = new TomlPrimitiveValue("text");
    TomlToken second = new TomlPrimitiveValue("text");

    var result = await first.DeepEqualsAsync(second).ConfigureAwait(false);

    Assert.That(result, Is.True);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Primitive_Values_Differ_Then_Returns_False()
  {
    TomlToken first = new TomlPrimitiveValue("text");
    TomlToken second = new TomlPrimitiveValue("other");

    var result = await first.DeepEqualsAsync(second).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Primitive_Values_Have_Different_Types_Then_Returns_False()
  {
    TomlToken first = new TomlPrimitiveValue(1);
    TomlToken second = new TomlPrimitiveValue("1");

    var result = await first.DeepEqualsAsync(second).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Comments_Are_Equal_Then_Returns_True()
  {
    var result = await new TomlComment("a").DeepEqualsAsync(new TomlComment("a")).ConfigureAwait(false);

    Assert.That(result, Is.True);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Comments_Differ_Then_Returns_False()
  {
    var result = await new TomlComment("a").DeepEqualsAsync(new TomlComment("b")).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Keys_Are_Equal_Then_Returns_True()
  {
    var result = await new TomlKey("a").DeepEqualsAsync(new TomlKey("a")).ConfigureAwait(false);

    Assert.That(result, Is.True);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Keys_Differ_Then_Returns_False()
  {
    var result = await new TomlKey("a").DeepEqualsAsync(new TomlKey("b")).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Tables_Are_Equal_Then_Returns_True()
  {
    var result = await createSampleTable().DeepEqualsAsync(createSampleTable()).ConfigureAwait(false);

    Assert.That(result, Is.True);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Table_Is_Compared_With_Itself_Then_Returns_True()
  {
    var table = createSampleTable();

    var result = await table.DeepEqualsAsync(table).ConfigureAwait(false);

    Assert.That(result, Is.True);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Table_Value_Differs_Then_Returns_False()
  {
    var other = createSampleTable();
    other["count"] = 4;

    var result = await createSampleTable().DeepEqualsAsync(other).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Table_Has_Different_Key_Then_Returns_False()
  {
    var first = new TomlTable {{"a", 1}};
    var second = new TomlTable {{"b", 1}};

    var result = await first.DeepEqualsAsync(second).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Table_Has_Additional_Key_Then_Returns_False()
  {
    var first = new TomlTable {{"a", 1}};
    var second = new TomlTable {{"a", 1}, {"b", 2}};

    Assert.That(await first.DeepEqualsAsync(second).ConfigureAwait(false), Is.False);
    Assert.That(await second.DeepEqualsAsync(first).ConfigureAwait(false), Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Nested_Table_Value_Differs_Then_Returns_False()
  {
    var other = createSampleTable();
    ((TomlTable) other["owner"])["name"] = "Different";

    var result = await createSampleTable().DeepEqualsAsync(other).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Nested_Array_Element_Differs_Then_Returns_False()
  {
    var other = createSampleTable();
    other["numbers"][2] = 99;

    var result = await createSampleTable().DeepEqualsAsync(other).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Inline_Table_Value_Differs_Then_Returns_False()
  {
    var other = createSampleTable();
    other["point"]["x"] = 100;

    var result = await createSampleTable().DeepEqualsAsync(other).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Array_Of_Tables_Element_Differs_Then_Returns_False()
  {
    var other = createSampleTable();
    other["products"][1]["id"] = 100;

    var result = await createSampleTable().DeepEqualsAsync(other).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Arrays_Are_Equal_Then_Returns_True()
  {
    var result = await new TomlArray {1, "two", true}.DeepEqualsAsync(new TomlArray {1, "two", true}).ConfigureAwait(false);

    Assert.That(result, Is.True);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Arrays_Have_Different_Order_Then_Returns_False()
  {
    var result = await new TomlArray {1, 2}.DeepEqualsAsync(new TomlArray {2, 1}).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Arrays_Have_Different_Count_Then_Returns_False()
  {
    var first = new TomlArray {1, 2};
    var second = new TomlArray {1, 2, 3};

    Assert.That(await first.DeepEqualsAsync(second).ConfigureAwait(false), Is.False);
    Assert.That(await second.DeepEqualsAsync(first).ConfigureAwait(false), Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Tokens_Have_Different_Kinds_Then_Returns_False()
  {
    var table = new TomlTable {{"a", 1}};
    var array = new TomlArray {1};
    var primitive = new TomlPrimitiveValue(1);

    Assert.That(await table.DeepEqualsAsync(array).ConfigureAwait(false), Is.False);
    Assert.That(await array.DeepEqualsAsync(table).ConfigureAwait(false), Is.False);
    Assert.That(await primitive.DeepEqualsAsync(array).ConfigureAwait(false), Is.False);
    Assert.That(await array.DeepEqualsAsync(primitive).ConfigureAwait(false), Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Table_Is_Compared_With_Inline_Table_Having_Same_Content_Then_Returns_False()
  {
    var table = new TomlTable("t") {{"x", 1}};
    var inlineTable = new TomlInlineTable("t") {{"x", 1}};

    var result = await table.DeepEqualsAsync(inlineTable).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Inline_Table_Is_Compared_With_Table_Having_Same_Content_Then_Returns_False()
  {
    var table = new TomlTable("t") {{"x", 1}};
    var inlineTable = new TomlInlineTable("t") {{"x", 1}};

    var result = await inlineTable.DeepEqualsAsync(table).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Array_Is_Compared_With_Array_Of_Tables_Having_Same_Content_Then_Returns_False()
  {
    var array = new TomlArray(new TomlKey("a"));
    var arrayOfTables = new TomlArrayOfTables("a");
    array.Add(new TomlTable("t") {{"x", 1}});
    arrayOfTables.Add(new TomlTable("t") {{"x", 1}});

    var result = await array.DeepEqualsAsync(arrayOfTables).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Array_Of_Tables_Is_Compared_With_Array_Having_Same_Content_Then_Returns_False()
  {
    var array = new TomlArray(new TomlKey("a"));
    var arrayOfTables = new TomlArrayOfTables("a");
    array.Add(new TomlTable("t") {{"x", 1}});
    arrayOfTables.Add(new TomlTable("t") {{"x", 1}});

    var result = await arrayOfTables.DeepEqualsAsync(array).ConfigureAwait(false);

    Assert.That(result, Is.False);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Nesting_Is_Deeper_Than_Default_Maximum_Then_Throws_TomlSerializerException()
  {
    var first = createNestedArrays(TomlSettings.Default.MaxNestingDepth + 50);
    var second = createNestedArrays(TomlSettings.Default.MaxNestingDepth + 50);

    await Assert.ThatAsync(async () => await first.DeepEqualsAsync(second).ConfigureAwait(false), Throws.TypeOf<TomlSerializerException>()).ConfigureAwait(false);
  }

  [Test]
  public async Task DeepEqualsAsync_When_Nesting_Is_Within_Default_Maximum_Then_Returns_True()
  {
    var first = createNestedArrays(TomlSettings.Default.MaxNestingDepth - 10);
    var second = createNestedArrays(TomlSettings.Default.MaxNestingDepth - 10);

    var result = await first.DeepEqualsAsync(second).ConfigureAwait(false);

    Assert.That(result, Is.True);
  }

  [Test]
  public async Task SerializeToStringAsync_When_Called_Then_Returns_Same_Result_As_TomlSerializer()
  {
    var table = createSampleTable();

    var fromToken = await table.SerializeToStringAsync().ConfigureAwait(false);
    var fromSerializer = await TomlSerializer.SerializeToStringAsync(table).ConfigureAwait(false);

    Assert.That(fromToken, Is.EqualTo(fromSerializer));
  }

  [Test]
  public async Task SerializeToStringAsync_When_Table_Then_Result_Can_Be_Deserialized_To_Equal_Table()
  {
    var table = createSampleTable();

    var toml = await table.SerializeToStringAsync().ConfigureAwait(false);
    var deserialized = await TomlSerializer.DeserializeAsync(toml).ConfigureAwait(false);

    Assert.That(await table.DeepEqualsAsync(deserialized).ConfigureAwait(false), Is.True, toml);
  }

  [Test]
  public async Task SerializeToStringAsync_When_Nesting_Is_Deeper_Than_Default_Maximum_Then_Throws_TomlSerializerException()
  {
    var table = new TomlTable {{"deep", createNestedArrays(TomlSettings.Default.MaxNestingDepth + 50)}};

    await Assert.ThatAsync(async () => await table.SerializeToStringAsync().ConfigureAwait(false), Throws.TypeOf<TomlSerializerException>()).ConfigureAwait(false);
  }


  private sealed class UndefinedToken : TomlToken
  {
    public UndefinedToken() : base(TomlTokenType.Undefined)
    {
    }
  }

  private static IEnumerable<TomlToken> leafTokens()
  {
    yield return new TomlPrimitiveValue(1);
    yield return new TomlPrimitiveValue("text");
    yield return new TomlComment("comment");
    yield return new TomlKey("key");
  }

  private static TomlTable createSampleTable()
  {
    var root = new TomlTable
    {
      {"title", "TOML"},
      {"count", 3},
      {"flag", true},
      {"numbers", new TomlArray(new TomlKey("numbers")) {1, 2, 3}}
    };

    root.Add(new TomlTable("owner") { { "name", "Tom" } });
    root.Add(new TomlInlineTable("point") { { "x", 1 }, { "y", 2 } });
    var products = new TomlArrayOfTables("products");
    products.Add(new TomlTable("p1") { { "id", 1 } });
    products.Add(new TomlTable("p2") { { "id", 2 } });
    root.Add(products);
    return root;
  }

  private static TomlArray createNestedArrays(int depth)
  {
    var root = new TomlArray();
    var current = root;
    for (var i = 1; i < depth; i++)
    {
      var next = new TomlArray();
      current.Add(next);
      current = next;
    }

    return root;
  }

  private static void assertAllExplicitCastsThrowInvalidCastException(TomlToken token)
  {
    Assert.Multiple(() =>
    {
      Assert.Throws<InvalidCastException>(() => _ = (string?)token);
      Assert.Throws<InvalidCastException>(() => _ = (int?)token);
      Assert.Throws<InvalidCastException>(() => _ = (short?)token);
      Assert.Throws<InvalidCastException>(() => _ = (ushort?)token);
      Assert.Throws<InvalidCastException>(() => _ = (long?)token);
      Assert.Throws<InvalidCastException>(() => _ = (uint?)token);
      Assert.Throws<InvalidCastException>(() => _ = (ulong?)token);
      Assert.Throws<InvalidCastException>(() => _ = (double?)token);
      Assert.Throws<InvalidCastException>(() => _ = (float?)token);
      Assert.Throws<InvalidCastException>(() => _ = (decimal?)token);
      Assert.Throws<InvalidCastException>(() => _ = (byte?)token);
      Assert.Throws<InvalidCastException>(() => _ = (sbyte?)token);
      Assert.Throws<InvalidCastException>(() => _ = (char?)token);
      Assert.Throws<InvalidCastException>(() => _ = (bool?)token);
      Assert.Throws<InvalidCastException>(() => _ = (Guid?)token);
      Assert.Throws<InvalidCastException>(() => _ = (DateTime?)token);
      Assert.Throws<InvalidCastException>(() => _ = (DateOnly?)token);
      Assert.Throws<InvalidCastException>(() => _ = (TimeOnly?)token);
    });
  }
}
