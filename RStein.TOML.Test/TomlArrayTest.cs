namespace RStein.TOML.Test;

[TestFixture]
internal class TomlArrayTest
{
  [Test]
  public void Ctor_When_Default_Then_Creates_Empty_Anonymous_Array()
  {
    var array = new TomlArray();

    Assert.That(array.TokenType, Is.EqualTo(TomlTokenType.Array));
    Assert.That(array.Name, Is.EqualTo("AnonymousArray"));
    Assert.That(array.Count, Is.EqualTo(0));
  }

  [Test]
  public void Ctor_When_Key_Is_Specified_Then_Uses_It_As_FullName()
  {
    var key = new TomlKey("servers");

    var array = new TomlArray(key);

    Assert.That(array.FullName, Is.SameAs(key));
    Assert.That(array.Name, Is.EqualTo("servers"));
  }

  [Test]
  public void Name_When_Key_Is_Dotted_Then_Returns_Last_Key_Part()
  {
    var key = new TomlKey("a")
    {
      NextKeyPart = new TomlKey("b")
    };

    var array = new TomlArray(key);

    Assert.That(array.Name, Is.EqualTo("b"));
  }

  [Test]
  public void IsReadOnly_When_Called_Then_Returns_False()
  {
    Assert.That(new TomlArray().IsReadOnly, Is.False);
  }

  [Test]
  public void Add_When_String_Then_Appends_String_Value()
  {
    var array = new TomlArray();

    array.Add("text");

    Assert.That(array[0], Is.InstanceOf<TomlPrimitiveValue>());
    Assert.That(((TomlPrimitiveValue) array[0]).Type, Is.EqualTo(TomlValueType.String));
    Assert.That((string?) array[0], Is.EqualTo("text"));
  }

  [Test]
  public void Add_When_Bool_Then_Appends_Boolean_Value()
  {
    var array = new TomlArray();

    array.Add(true);

    Assert.That(((TomlPrimitiveValue) array[0]).Type, Is.EqualTo(TomlValueType.Boolean));
    Assert.That((bool?) array[0], Is.True);
  }

  [Test]
  public void Add_When_Long_Then_Appends_Integer_Value()
  {
    var array = new TomlArray();

    array.Add(long.MaxValue);

    Assert.That(((TomlPrimitiveValue) array[0]).Type, Is.EqualTo(TomlValueType.Integer));
    Assert.That((long?) array[0], Is.EqualTo(long.MaxValue));
  }

  [Test]
  public void Add_When_Int_Then_Appends_Integer_Value()
  {
    var array = new TomlArray();

    array.Add(42);

    Assert.That(((TomlPrimitiveValue) array[0]).Type, Is.EqualTo(TomlValueType.Integer));
    Assert.That((int?) array[0], Is.EqualTo(42));
  }

  [Test]
  public void Add_When_Short_Then_Appends_Integer_Value()
  {
    var array = new TomlArray();

    array.Add((short) -7);

    Assert.That(((TomlPrimitiveValue) array[0]).Type, Is.EqualTo(TomlValueType.Integer));
    Assert.That((short?) array[0], Is.EqualTo((short) -7));
  }

  [Test]
  public void Add_When_Byte_Then_Appends_Integer_Value()
  {
    var array = new TomlArray();

    array.Add((byte) 200);

    Assert.That(((TomlPrimitiveValue) array[0]).Type, Is.EqualTo(TomlValueType.Integer));
    Assert.That((byte?) array[0], Is.EqualTo((byte) 200));
  }

  [Test]
  public void Add_When_SByte_Then_Appends_Integer_Value()
  {
    var array = new TomlArray();

    array.Add((sbyte) -100);

    Assert.That(((TomlPrimitiveValue) array[0]).Type, Is.EqualTo(TomlValueType.Integer));
    Assert.That((sbyte?) array[0], Is.EqualTo((sbyte) -100));
  }

  [Test]
  public void Add_When_Guid_Then_Appends_String_Value()
  {
    var array = new TomlArray();
    var guid = Guid.NewGuid();

    array.Add(guid);

    Assert.That(((TomlPrimitiveValue) array[0]).Type, Is.EqualTo(TomlValueType.String));
    Assert.That((Guid?) array[0], Is.EqualTo(guid));
  }

  [Test]
  public void Add_When_Float_Then_Appends_Float_Value()
  {
    var array = new TomlArray();

    array.Add(1.5f);

    Assert.That(((TomlPrimitiveValue) array[0]).Type, Is.EqualTo(TomlValueType.Float));
    Assert.That((float?) array[0], Is.EqualTo(1.5f));
  }

  [Test]
  public void Add_When_Double_Then_Appends_Float_Value()
  {
    var array = new TomlArray();

    array.Add(3.25);

    Assert.That(((TomlPrimitiveValue) array[0]).Type, Is.EqualTo(TomlValueType.Float));
    Assert.That((double?) array[0], Is.EqualTo(3.25));
  }

  [Test]
  public void Add_When_Decimal_Then_Appends_Float_Value()
  {
    var array = new TomlArray();

    array.Add(12.75m);

    Assert.That(((TomlPrimitiveValue) array[0]).Type, Is.EqualTo(TomlValueType.Float));
    Assert.That((decimal?) array[0], Is.EqualTo(12.75m));
  }

  [Test]
  public void Add_When_DateTime_Then_Appends_DateTime_Value()
  {
    var array = new TomlArray();
    var dateTime = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

    array.Add(dateTime);

    Assert.That(((TomlPrimitiveValue) array[0]).Type, Is.EqualTo(TomlValueType.DateTime));
    Assert.That((DateTime?) array[0], Is.EqualTo(dateTime));
  }

  [Test]
  public void Add_When_Table_Then_Appends_Same_Instance()
  {
    var array = new TomlArray();
    var table = new TomlTable("t");

    array.Add(table);

    Assert.That(array[0], Is.SameAs(table));
  }

  [Test]
  public void Add_When_Array_Then_Appends_Same_Instance()
  {
    var array = new TomlArray();
    var nestedArray = new TomlArray();

    array.Add(nestedArray);

    Assert.That(array[0], Is.SameAs(nestedArray));
  }

  [Test]
  public void Add_When_Token_Then_Appends_Same_Instance()
  {
    var array = new TomlArray();
    TomlToken token = new TomlPrimitiveValue(1);

    array.Add(token);

    Assert.That(array[0], Is.SameAs(token));
  }

  [Test]
  public void Add_When_Token_Is_Null_Then_Throws_ArgumentNullException()
  {
    var array = new TomlArray();

    Assert.Throws<ArgumentNullException>(() => array.Add((TomlToken) null!));
  }

  [Test]
  public void Add_When_Table_Is_Null_Then_Throws_ArgumentNullException()
  {
    var array = new TomlArray();

    Assert.Throws<ArgumentNullException>(() => array.Add((TomlTable) null!));
  }

  [Test]
  public void Add_When_Array_Is_Null_Then_Throws_ArgumentNullException()
  {
    var array = new TomlArray();

    Assert.Throws<ArgumentNullException>(() => array.Add((TomlArray) null!));
  }

  [Test]
  public void Add_When_Called_Multiple_Times_Then_Keeps_Insertion_Order()
  {
    var array = new TomlArray();

    array.Add(1);
    array.Add("two");
    array.Add(true);

    Assert.That(array.Count, Is.EqualTo(3));
    Assert.That((int?) array[0], Is.EqualTo(1));
    Assert.That((string?) array[1], Is.EqualTo("two"));
    Assert.That((bool?) array[2], Is.True);
  }

  [Test]
  public void Indexer_When_Index_Is_Out_Of_Range_Then_Throws_ArgumentOutOfRangeException()
  {
    var array = new TomlArray { 1 };

    Assert.Throws<ArgumentOutOfRangeException>(() => _ = array[1]);
    Assert.Throws<ArgumentOutOfRangeException>(() => _ = array[-1]);
  }

  [Test]
  public void Indexer_When_Set_Then_Replaces_Element()
  {
    var array = new TomlArray { 1, 2 };

    array[1] = "replaced";

    Assert.That(array.Count, Is.EqualTo(2));
    Assert.That((string?) array[1], Is.EqualTo("replaced"));
  }

  [Test]
  public void Indexer_When_Set_To_Null_Then_Throws_ArgumentNullException()
  {
    var array = new TomlArray { 1 };

    Assert.Throws<ArgumentNullException>(() => array[0] = null!);
  }

  [Test]
  public void StringIndexer_When_Get_Then_Throws_InvalidOperationException()
  {
    var array = new TomlArray();

    Assert.Throws<InvalidOperationException>(() => _ = array["key"]);
  }

  [Test]
  public void StringIndexer_When_Set_Then_Throws_InvalidOperationException()
  {
    var array = new TomlArray();

    Assert.Throws<InvalidOperationException>(() => array["key"] = 1);
  }

  [Test]
  public void KeyIndexer_When_Used_Then_Throws_InvalidOperationException()
  {
    var array = new TomlArray();

    Assert.Throws<InvalidOperationException>(() => _ = array[new TomlKey("key")]);
  }

  [Test]
  public void Insert_When_Called_Then_Inserts_On_Index()
  {
    var array = new TomlArray { 1, 3 };

    array.Insert(1, new TomlPrimitiveValue(2));

    Assert.That(array.Count, Is.EqualTo(3));
    Assert.That((int?) array[1], Is.EqualTo(2));
    Assert.That((int?) array[2], Is.EqualTo(3));
  }

  [Test]
  public void Insert_When_Token_Is_Null_Then_Throws_ArgumentNullException()
  {
    var array = new TomlArray { 1 };

    Assert.Throws<ArgumentNullException>(() => array.Insert(0, null!));
  }

  [Test]
  public void RemoveAt_When_Called_Then_Removes_Element()
  {
    var array = new TomlArray { 1, 2, 3 };

    array.RemoveAt(1);

    Assert.That(array.Count, Is.EqualTo(2));
    Assert.That((int?) array[1], Is.EqualTo(3));
  }

  [Test]
  public void Remove_When_Item_Exists_Then_Removes_It_And_Returns_True()
  {
    var array = new TomlArray();
    var item = new TomlPrimitiveValue(5);
    array.Add(item);

    var removed = array.Remove(item);

    Assert.That(removed, Is.True);
    Assert.That(array.Count, Is.EqualTo(0));
  }

  [Test]
  public void Remove_When_Item_Does_Not_Exist_Then_Returns_False()
  {
    var array = new TomlArray { 1 };

    var removed = array.Remove(new TomlPrimitiveValue(2));

    Assert.That(removed, Is.False);
    Assert.That(array.Count, Is.EqualTo(1));
  }

  [Test]
  public void Clear_When_Called_Then_Removes_All_Elements()
  {
    var array = new TomlArray { 1, 2, 3 };

    array.Clear();

    Assert.That(array.Count, Is.EqualTo(0));
  }

  [Test]
  public void Contains_When_Item_Exists_Then_Returns_True()
  {
    var array = new TomlArray();
    var item = new TomlPrimitiveValue("x");
    array.Add(item);

    Assert.That(array.Contains(item), Is.True);
  }

  [Test]
  public void Contains_When_Item_Does_Not_Exist_Then_Returns_False()
  {
    var array = new TomlArray { "x" };

    Assert.That(array.Contains(new TomlPrimitiveValue("y")), Is.False);
  }

  [Test]
  public void IndexOf_When_Item_Exists_Then_Returns_Its_Index()
  {
    var array = new TomlArray();
    var item = new TomlPrimitiveValue(3);
    array.Add(1);
    array.Add(2);
    array.Add(item);

    Assert.That(array.IndexOf(item), Is.EqualTo(2));
  }

  [Test]
  public void IndexOf_When_Item_Does_Not_Exist_Then_Returns_Minus_One()
  {
    var array = new TomlArray { 1 };

    Assert.That(array.IndexOf(new TomlPrimitiveValue(99)), Is.EqualTo(-1));
  }

  [Test]
  public void CopyTo_When_Called_Then_Copies_Elements_From_Array_Index()
  {
    var array = new TomlArray { 1, 2 };
    var target = new TomlToken[3];

    array.CopyTo(target, 1);

    Assert.That(target[0], Is.Null);
    Assert.That(target[1], Is.SameAs(array[0]));
    Assert.That(target[2], Is.SameAs(array[1]));
  }

  [Test]
  public void Tokens_When_Called_Then_Returns_Elements_In_Order()
  {
    var array = new TomlArray { 1, 2, 3 };

    var numbers = array.Tokens.Select(token => (int?) token).ToList();

    Assert.That(numbers, Is.EqualTo(new int?[] {1, 2, 3}));
  }

  [Test]
  public void GetEnumerator_When_Called_Then_Enumerates_All_Elements()
  {
    var array = new TomlArray { "a", "b" };

    var values = new List<string?>();
    foreach (var token in array)
    {
      values.Add((string?) token);
    }

    Assert.That(values, Is.EqualTo(["a", "b"]));
  }

  [Test]
  public void Add_When_Nested_Arrays_And_Tables_Then_Keeps_Structure()
  {
    var innerArray = new TomlArray { 1, 2 };
    var innerTable = new TomlTable("t") { { "key", "value" } };
    var array = new TomlArray();

    array.Add(innerArray);
    array.Add(innerTable);

    Assert.That(array[0], Is.SameAs(innerArray));
    Assert.That(array[0][1], Is.Not.Null);
    Assert.That((int?) array[0][1], Is.EqualTo(2));
    Assert.That((string?) array[1]["key"], Is.EqualTo("value"));
  }

  [Test]
  public void ToString_When_Called_Then_Contains_Name_And_Count()
  {
    var array = new TomlArray(new TomlKey("servers")) { 1, 2 };

    var text = array.ToString();

    Assert.That(text, Does.Contain("servers"));
    Assert.That(text, Does.Contain("Count: 2"));
    Assert.That(text, Does.Not.Contain("System.Collections.Generic.List"));
  }
}
