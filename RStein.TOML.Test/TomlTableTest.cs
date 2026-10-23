namespace RStein.TOML.Test;

[TestFixture]
internal class TomlTableTest
{
  [Test]
  public void TryGetValue_When_String_Key_Exists_Then_Returns_True_And_Value()
  {
    var table = new TomlTable();
    table.Add("key", "value");

    var found = table.TryGetValue("key", out var value);

    Assert.That(found, Is.True);
    Assert.That(value, Is.SameAs(table["key"]));
  }

  [Test]
  public void TryGetValue_When_String_Key_Is_Missing_Then_Returns_False_And_Null()
  {
    var table = new TomlTable();

    var found = table.TryGetValue("missing", out var value);

    Assert.That(found, Is.False);
    Assert.That(value, Is.Null);
  }

  [Test]
  public void TryGetValue_When_String_Key_Is_Null_Then_Throws_ArgumentNullException()
  {
    var table = new TomlTable();

    Assert.Throws<ArgumentNullException>(() => table.TryGetValue((string) null!, out _));
  }

  [Test]
  public void TryGetValue_When_String_Key_Needs_Quoting_Then_Matches_TomlKey_Overload()
  {
    var table = new TomlTable();
    table.Add("a b", 1);

    var foundByString = table.TryGetValue("a b", out var byString);
    var foundByKey = table.TryGetValue(new TomlKey("a b"), out var byKey);

    Assert.That(foundByString, Is.True);
    Assert.That(foundByKey, Is.True);
    Assert.That(byString, Is.SameAs(byKey));
  }

  [Test]
  public void TryGetValue_When_TomlKey_Exists_Then_Returns_True_And_Value()
  {
    var table = new TomlTable();
    table.Add("key", "value");

    var found = table.TryGetValue(new TomlKey("key"), out var value);

    Assert.That(found, Is.True);
    Assert.That((string?) value, Is.EqualTo("value"));
  }

  [Test]
  public void TryGetValue_When_TomlKey_Is_Missing_Then_Returns_False_And_Null()
  {
    var table = new TomlTable();

    var found = table.TryGetValue(new TomlKey("missing"), out var value);

    Assert.That(found, Is.False);
    Assert.That(value, Is.Null);
  }

  [Test]
  public void Ctor_When_Default_Then_Creates_Root_Table()
  {
    var table = new TomlTable();

    Assert.That(table.TokenType, Is.EqualTo(TomlTokenType.Table));
    Assert.That(table.Name, Is.EqualTo("Root table"));
    Assert.That(table.Count, Is.EqualTo(0));
  }

  [Test]
  public void Ctor_When_Name_Then_Uses_It_As_Name()
  {
    var table = new TomlTable("server");

    Assert.That(table.Name, Is.EqualTo("server"));
    Assert.That(table.FullName.ToString(), Is.EqualTo("server"));
  }

  [Test]
  public void Ctor_When_Key_Then_Uses_It_As_FullName()
  {
    var key = new TomlKey("server");

    var table = new TomlTable(key);

    Assert.That(table.FullName, Is.SameAs(key));
  }

  [Test]
  public void Ctor_When_Key_Is_Null_Then_Throws_ArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(() => _ = new TomlTable((TomlKey) null!));
  }

  [Test]
  public void Ctor_When_Name_Is_Null_Then_Throws_ArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(() => _ = new TomlTable((string) null!));
  }

  [Test]
  public void Add_When_String_Key_And_Primitive_Values_Then_Stores_Them_With_Expected_Types()
  {
    var table = new TomlTable();
    var guid = Guid.NewGuid();

    table.Add("string", "text");
    table.Add("bool", true);
    table.Add("long", 1L);
    table.Add("int", 2);
    table.Add("short", (short) 3);
    table.Add("byte", (byte) 4);
    table.Add("sbyte", (sbyte) 5);
    table.Add("guid", guid);
    table.Add("float", 1.5f);
    table.Add("double", 2.5);
    table.Add("decimal", 3.5m);
    table.Add("dateTime", new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc));

    Assert.Multiple(() =>
    {
      Assert.That(table.Count, Is.EqualTo(12));
      Assert.That((string?) table["string"], Is.EqualTo("text"));
      Assert.That((bool?) table["bool"], Is.True);
      Assert.That((long?) table["long"], Is.EqualTo(1L));
      Assert.That((int?) table["int"], Is.EqualTo(2));
      Assert.That((short?) table["short"], Is.EqualTo((short) 3));
      Assert.That((byte?) table["byte"], Is.EqualTo((byte) 4));
      Assert.That((sbyte?) table["sbyte"], Is.EqualTo((sbyte) 5));
      Assert.That((Guid?) table["guid"], Is.EqualTo(guid));
      Assert.That((float?) table["float"], Is.EqualTo(1.5f));
      Assert.That((double?) table["double"], Is.EqualTo(2.5));
      Assert.That((decimal?) table["decimal"], Is.EqualTo(3.5m));
      Assert.That(((TomlPrimitiveValue) table["dateTime"]).Type, Is.EqualTo(TomlValueType.DateTime));
    });
  }

  [Test]
  public void Add_When_TomlKey_And_Primitive_Values_Then_Stores_Them_With_Expected_Types()
  {
    var table = new TomlTable();
    var guid = Guid.NewGuid();

    table.Add(new TomlKey("string"), "text");
    table.Add(new TomlKey("bool"), true);
    table.Add(new TomlKey("long"), 1L);
    table.Add(new TomlKey("int"), 2);
    table.Add(new TomlKey("short"), (short) 3);
    table.Add(new TomlKey("byte"), (byte) 4);
    table.Add(new TomlKey("sbyte"), (sbyte) 5);
    table.Add(new TomlKey("guid"), guid);
    table.Add(new TomlKey("float"), 1.5f);
    table.Add(new TomlKey("double"), 2.5);
    table.Add(new TomlKey("decimal"), 3.5m);
    table.Add(new TomlKey("dateTime"), new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc));

    Assert.Multiple(() =>
    {
      Assert.That(table.Count, Is.EqualTo(12));
      Assert.That((string?) table["string"], Is.EqualTo("text"));
      Assert.That((bool?) table["bool"], Is.True);
      Assert.That((long?) table["long"], Is.EqualTo(1L));
      Assert.That((int?) table["int"], Is.EqualTo(2));
      Assert.That((short?) table["short"], Is.EqualTo((short) 3));
      Assert.That((byte?) table["byte"], Is.EqualTo((byte) 4));
      Assert.That((sbyte?) table["sbyte"], Is.EqualTo((sbyte) 5));
      Assert.That((Guid?) table["guid"], Is.EqualTo(guid));
      Assert.That((float?) table["float"], Is.EqualTo(1.5f));
      Assert.That((double?) table["double"], Is.EqualTo(2.5));
      Assert.That((decimal?) table["decimal"], Is.EqualTo(3.5m));
      Assert.That(((TomlPrimitiveValue) table["dateTime"]).Type, Is.EqualTo(TomlValueType.DateTime));
    });
  }

  [Test]
  public void Add_When_Table_Then_Uses_Its_FullName_As_Key()
  {
    var root = new TomlTable();
    var child = new TomlTable("child");

    root.Add(child);

    Assert.That(root["child"], Is.SameAs(child));
  }

  [Test]
  public void Add_When_Array_Then_Uses_Its_FullName_As_Key()
  {
    var root = new TomlTable();
    var array = new TomlArray(new TomlKey("numbers")) { 1, 2 };

    root.Add(array);

    Assert.That(root["numbers"], Is.SameAs(array));
  }

  [Test]
  public void Add_When_KeyValuePair_Then_Stores_It()
  {
    var table = new TomlTable();
    var key = new TomlKey("key");
    TomlToken value = 1;

    table.Add(new KeyValuePair<TomlKey, TomlToken>(key, value));

    Assert.That(table[key], Is.SameAs(value));
  }

  [Test]
  public void Add_When_Key_Already_Exists_Then_Throws_ArgumentException()
  {
    var table = new TomlTable();
    table.Add("key", 1);

    Assert.Throws<ArgumentException>(() => table.Add("key", 2));
  }

  [Test]
  public void Add_When_String_Key_Is_Null_Then_Throws_ArgumentNullException()
  {
    var table = new TomlTable();

    Assert.Throws<ArgumentNullException>(() => table.Add((string) null!, 1));
  }

  [Test]
  public void Add_When_Keys_Differ_Only_By_Case_Then_Stores_Both()
  {
    var table = new TomlTable();

    table.Add("key", 1);
    table.Add("KEY", 2);

    Assert.That(table.Count, Is.EqualTo(2));
    Assert.That((int?) table["key"], Is.EqualTo(1));
    Assert.That((int?) table["KEY"], Is.EqualTo(2));
  }

  [Test]
  public void Add_When_Table_Is_Null_Then_Throws_ArgumentNullException()
  {
    var table = new TomlTable();

    Assert.Throws<ArgumentNullException>(() => table.Add((TomlTable) null!));
  }

  [Test]
  public void Add_When_Array_Is_Null_Then_Throws_ArgumentNullException()
  {
    var table = new TomlTable();

    Assert.Throws<ArgumentNullException>(() => table.Add((TomlArray) null!));
  }

  [Test]
  public void Add_When_Value_Is_Null_Then_Throws_ArgumentNullException()
  {
    var table = new TomlTable();

    Assert.Throws<ArgumentNullException>(() => table.Add("key", (TomlToken) null!));
  }

  [Test]
  public void StringIndexer_When_Key_Is_Missing_Then_Throws_KeyNotFoundException()
  {
    var table = new TomlTable();

    Assert.Throws<KeyNotFoundException>(() => _ = table["missing"]);
  }

  [Test]
  public void KeyIndexer_When_Key_Is_Missing_Then_Throws_KeyNotFoundException()
  {
    var table = new TomlTable();

    Assert.Throws<KeyNotFoundException>(() => _ = table[new TomlKey("missing")]);
  }

  [Test]
  public void StringIndexer_When_Set_For_New_Key_Then_Adds_Value()
  {
    var table = new TomlTable();

    table["key"] = "value";

    Assert.That(table.Count, Is.EqualTo(1));
    Assert.That((string?) table["key"], Is.EqualTo("value"));
  }

  [Test]
  public void StringIndexer_When_Set_For_Existing_Key_Then_Replaces_Value()
  {
    var table = new TomlTable();
    table["key"] = 1;

    table["key"] = 2;

    Assert.That(table.Count, Is.EqualTo(1));
    Assert.That((int?) table["key"], Is.EqualTo(2));
  }

  [Test]
  public void KeyIndexer_When_Set_And_Get_Then_Round_Trips_Value()
  {
    var table = new TomlTable();
    var key = new TomlKey("key");

    table[key] = true;

    Assert.That((bool?) table[key], Is.True);
  }

  [Test]
  public void StringIndexer_When_Key_Is_Null_Then_Throws_ArgumentNullException()
  {
    var table = new TomlTable();

    Assert.Throws<ArgumentNullException>(() => _ = table[(string) null!]);
  }

  [Test]
  public void IntIndexer_When_Get_Then_Returns_Values_In_Insertion_Order()
  {
    var table = new TomlTable();
    table.Add("a", 1);
    table.Add("b", 2);
    table.Add("c", 3);

    Assert.That((int?) table[0], Is.EqualTo(1));
    Assert.That((int?) table[1], Is.EqualTo(2));
    Assert.That((int?) table[2], Is.EqualTo(3));
  }

  [Test]
  public void IntIndexer_When_Set_Then_Replaces_Value_For_Key_On_Index()
  {
    var table = new TomlTable();
    table.Add("a", 1);
    table.Add("b", 2);

    table[1] = 20;

    Assert.That(table.Count, Is.EqualTo(2));
    Assert.That((int?) table["b"], Is.EqualTo(20));
  }

  [Test]
  public void IntIndexer_When_Index_Is_Out_Of_Range_Then_Throws_ArgumentOutOfRangeException()
  {
    var table = new TomlTable();
    table.Add("a", 1);

    Assert.Throws<ArgumentOutOfRangeException>(() => _ = table[1]);
  }

  [Test]
  public void ContainsKey_When_Key_Exists_Then_Returns_True()
  {
    var table = new TomlTable();
    table.Add("key", 1);

    Assert.That(table.ContainsKey("key"), Is.True);
    Assert.That(table.ContainsKey(new TomlKey("key")), Is.True);
  }

  [Test]
  public void ContainsKey_When_Key_Does_Not_Exist_Then_Returns_False()
  {
    var table = new TomlTable();

    Assert.That(table.ContainsKey("key"), Is.False);
    Assert.That(table.ContainsKey(new TomlKey("key")), Is.False);
  }

  [Test]
  public void Remove_When_String_Key_Exists_Then_Removes_It_And_Returns_True()
  {
    var table = new TomlTable();
    table.Add("key", 1);

    var removed = table.Remove("key");

    Assert.That(removed, Is.True);
    Assert.That(table.Count, Is.EqualTo(0));
  }

  [Test]
  public void Remove_When_TomlKey_Exists_Then_Removes_It_And_Returns_True()
  {
    var table = new TomlTable();
    table.Add("key", 1);

    var removed = table.Remove(new TomlKey("key"));

    Assert.That(removed, Is.True);
    Assert.That(table.Count, Is.EqualTo(0));
  }

  [Test]
  public void Remove_When_Key_Does_Not_Exist_Then_Returns_False()
  {
    var table = new TomlTable();
    table.Add("key", 1);

    Assert.That(table.Remove("other"), Is.False);
    Assert.That(table.Remove(new TomlKey("other")), Is.False);
    Assert.That(table.Count, Is.EqualTo(1));
  }

  [Test]
  public void Remove_When_KeyValuePair_Matches_Then_Removes_It()
  {
    var table = new TomlTable();
    var key = new TomlKey("key");
    TomlToken value = 1;
    table.Add(key, value);

    var removed = table.Remove(new KeyValuePair<TomlKey, TomlToken>(key, value));

    Assert.That(removed, Is.True);
    Assert.That(table.Count, Is.EqualTo(0));
  }

  [Test]
  public void Contains_When_KeyValuePair_Matches_Then_Returns_True()
  {
    var table = new TomlTable();
    var key = new TomlKey("key");
    TomlToken value = 1;
    table.Add(key, value);

    Assert.That(table.Contains(new KeyValuePair<TomlKey, TomlToken>(key, value)), Is.True);
    Assert.That(table.Contains(new KeyValuePair<TomlKey, TomlToken>(key, 2)), Is.False);
  }

  [Test]
  public void Clear_When_Called_Then_Removes_All_Items()
  {
    var table = new TomlTable();
    table.Add("a", 1);
    table.Add("b", 2);

    table.Clear();

    Assert.That(table.Count, Is.EqualTo(0));
  }

  [Test]
  public void IsReadOnly_When_Called_Then_Returns_False()
  {
    Assert.That(new TomlTable().IsReadOnly, Is.False);
  }

  [Test]
  public void Keys_And_Values_When_Called_Then_Return_Added_Items()
  {
    var table = new TomlTable();
    table.Add("a", 1);
    table.Add("b", 2);

    Assert.That(table.Keys.Select(key => key.RawKey), Is.EquivalentTo(["a", "b"]));
    Assert.That(table.Values.Select(value => (int?) value), Is.EquivalentTo(new int?[] {1, 2}));
  }

  [Test]
  public void Tokens_When_Called_Then_Returns_Values()
  {
    var table = new TomlTable();
    table.Add("a", 1);
    table.Add("b", 2);

    Assert.That(table.Tokens.Select(value => (int?) value), Is.EquivalentTo(new int?[] {1, 2}));
  }

  [Test]
  public void GetEnumerator_When_Called_Then_Enumerates_Key_Value_Pairs()
  {
    var table = new TomlTable();
    table.Add("a", 1);
    table.Add("b", 2);

    var pairs = new List<KeyValuePair<TomlKey, TomlToken>>();
    foreach (var pair in table)
    {
      pairs.Add(pair);
    }

    Assert.That(pairs.Select(pair => pair.Key.RawKey), Is.EquivalentTo(["a", "b"]));
  }

  [Test]
  public void CopyTo_When_Called_Then_Copies_Key_Value_Pairs_From_Array_Index()
  {
    var table = new TomlTable
    {
      {"a", 1},
      {"b", 2}
    };
    var target = new KeyValuePair<TomlKey, TomlToken>[3];

    table.CopyTo(target, 1);

    Assert.That(target[0].Key, Is.Null);
    Assert.That(target[1].Key.RawKey, Is.EqualTo("a"));
    Assert.That(target[2].Key.RawKey, Is.EqualTo("b"));
  }

  [Test]
  public void HasTopLevelDefinition_When_Set_To_True_Then_Sets_TopLevelDeclaration()
  {
    var table = new TomlTable("t")
    {
      HasTopLevelDefinition = true
    };

    Assert.That(table.HasTopLevelDefinition, Is.True);
    Assert.That(table.HasTopLevelDeclaration, Is.True);
  }

  [Test]
  public void HasTopLevelDeclaration_When_Set_To_False_Then_Clears_TopLevelDefinition()
  {
    var table = new TomlTable("t") {HasTopLevelDefinition = true};

    table.HasTopLevelDeclaration = false;

    Assert.That(table.HasTopLevelDefinition, Is.False);
    Assert.That(table.HasTopLevelDeclaration, Is.False);
  }

  [Test]
  public void IsUntaggedTable_When_No_Tag_Is_Set_Then_Returns_True()
  {
    Assert.That(new TomlTable("t").IsUntaggedTable(), Is.True);
  }

  [TestCase(nameof(TomlTable.HasTopLevelDefinition))]
  [TestCase(nameof(TomlTable.HasFromKeyDefinition))]
  [TestCase(nameof(TomlTable.HasFromInlineTableDefinition))]
  [TestCase(nameof(TomlTable.IsArrayOfTablesMember))]
  public void IsUntaggedTable_When_Any_Tag_Is_Set_Then_Returns_False(string tag)
  {
    var table = new TomlTable("t");

    typeof(TomlTable).GetProperty(tag)!.SetValue(table, true);

    Assert.That(table.IsUntaggedTable(), Is.False);
  }

  [Test]
  public void Add_When_Untagged_Table_To_Root_Table_Then_Child_Has_TopLevelDefinition()
  {
    var root = new TomlTable();
    var child = new TomlTable("child");

    root.Add(child);

    Assert.That(child.HasTopLevelDefinition, Is.True);
  }

  [Test]
  public void Add_When_Untagged_Table_To_Table_With_FromKeyDefinition_Then_Propagates_FromKeyDefinition()
  {
    var parent = new TomlTable("parent")
    {
      HasFromKeyDefinition = true
    };
    var child = new TomlTable("child");

    parent.Add(child);

    Assert.That(child.HasFromKeyDefinition, Is.True);
    Assert.That(child.HasTopLevelDefinition, Is.False);
    Assert.That(child.HasFromInlineTableDefinition, Is.False);
  }

  [Test]
  public void Add_When_Untagged_Table_To_Table_With_TopLevelDefinition_Then_Propagates_TopLevelDefinition()
  {
    var parent = new TomlTable("parent")
    {
      HasTopLevelDefinition = true
    };
    var child = new TomlTable("child");

    parent.Add(child);

    Assert.That(child.HasTopLevelDefinition, Is.True);
    Assert.That(child.HasFromKeyDefinition, Is.False);
  }

  [Test]
  public void Add_When_Untagged_Table_To_Table_With_FromInlineTableDefinition_Then_Propagates_FromInlineTableDefinition()
  {
    var parent = new TomlTable("parent")
    {
      HasFromInlineTableDefinition = true
    };
    var child = new TomlTable("child");

    parent.Add(child);

    Assert.That(child.HasFromInlineTableDefinition, Is.True);
    Assert.That(child.HasTopLevelDefinition, Is.False);
  }

  [Test]
  public void Add_When_Already_Tagged_Table_Then_Does_Not_Change_Its_Tags()
  {
    var parent = new TomlTable("parent")
    {
      HasTopLevelDefinition = true
    };
    var child = new TomlTable("child")
    {
      HasFromKeyDefinition = true
    };

    parent.Add(child);

    Assert.That(child.HasFromKeyDefinition, Is.True);
    Assert.That(child.HasTopLevelDefinition, Is.False);
  }

  [Test]
  public void Add_When_Untagged_Table_To_Untagged_Table_Then_Leaves_Child_Untagged_Until_Parent_Is_Tagged()
  {
    var parent = new TomlTable("parent");
    var child = new TomlTable("child");

    parent.Add(child);

    Assert.That(child.IsUntaggedTable(), Is.True);
  }

  [Test]
  public void HasTopLevelDefinition_When_Set_On_Parent_With_Unresolved_Child_Then_Propagates_To_Child()
  {
    var parent = new TomlTable("parent");
    var child = new TomlTable("child");
    parent.Add(child);

    parent.HasTopLevelDefinition = true;

    Assert.That(child.HasTopLevelDefinition, Is.True);
  }

  [Test]
  public void HasFromKeyDefinition_When_Set_On_Parent_With_Unresolved_Child_Then_Propagates_To_Child()
  {
    var parent = new TomlTable("parent");
    var child = new TomlTable("child");
    parent.Add(child);

    parent.HasFromKeyDefinition = true;

    Assert.That(child.HasFromKeyDefinition, Is.True);
    Assert.That(child.HasTopLevelDefinition, Is.False);
  }

  [Test]
  public void HasFromInlineTableDefinition_When_Set_On_Parent_With_Unresolved_Child_Then_Propagates_To_Child()
  {
    var parent = new TomlTable("parent");
    var child = new TomlTable("child");
    parent.Add(child);

    parent.HasFromInlineTableDefinition = true;

    Assert.That(child.HasFromInlineTableDefinition, Is.True);
    Assert.That(child.HasTopLevelDefinition, Is.False);
  }

  [Test]
  public void HasTopLevelDefinition_When_Set_On_Parent_Then_Propagates_To_Nested_Unresolved_Tables()
  {
    var root = new TomlTable();
    var parent = new TomlTable("parent");
    var child = new TomlTable("child");
    var grandChild = new TomlTable("grandChild");
    child.Add(grandChild);
    parent.Add(child);

    root.Add(parent);

    Assert.That(parent.HasTopLevelDefinition, Is.True);
    Assert.That(child.HasTopLevelDefinition, Is.True);
    Assert.That(grandChild.HasTopLevelDefinition, Is.True);
  }

  [Test]
  public void IsArrayOfTablesMember_When_Set_Then_Returns_Set_Value()
  {
    var table = new TomlTable("t")
    {
      IsArrayOfTablesMember = true
    };

    Assert.That(table.IsArrayOfTablesMember, Is.True);
  }

  [Test]
  public void ToString_When_Called_Then_Contains_Name_And_Count()
  {
    var table = new TomlTable("server");
    table.Add("key", 1);

    var text = table.ToString();

    Assert.That(text, Does.Contain("Name: server"));
    Assert.That(text, Does.Contain("Count: 1"));
  }
}
