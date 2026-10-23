namespace RStein.TOML.Test;

[TestFixture]
internal class TomlArrayOfTablesTest
{
  [Test]
  public void Ctor_When_String_Name_Then_Creates_Array_Of_Tables_With_Name()
  {
    var arrayOfTables = new TomlArrayOfTables("products");

    Assert.That(arrayOfTables.TokenType, Is.EqualTo(TomlTokenType.ArrayOfTables));
    Assert.That(arrayOfTables.Name, Is.EqualTo("products"));
    Assert.That(arrayOfTables.Count, Is.EqualTo(0));
  }

  [Test]
  public void Ctor_When_Key_Then_Uses_It_As_FullName()
  {
    var key = new TomlKey("a")
    {
      NextKeyPart = new TomlKey("products")
    };

    var arrayOfTables = new TomlArrayOfTables(key);

    Assert.That(arrayOfTables.FullName, Is.SameAs(key));
    Assert.That(arrayOfTables.Name, Is.EqualTo("products"));
    Assert.That(arrayOfTables.TokenType, Is.EqualTo(TomlTokenType.ArrayOfTables));
  }

  [Test]
  public void Ctor_When_Called_Then_Is_TomlArray()
  {
    var arrayOfTables = new TomlArrayOfTables("products");

    Assert.That(arrayOfTables, Is.InstanceOf<TomlArray>());
  }

  [Test]
  public void Add_When_Tables_Then_Keeps_Them_In_Order()
  {
    var arrayOfTables = new TomlArrayOfTables("products");
    var first = new TomlTable("first");
    var second = new TomlTable("second");

    arrayOfTables.Add(first);
    arrayOfTables.Add(second);

    Assert.That(arrayOfTables.Count, Is.EqualTo(2));
    Assert.That(arrayOfTables[0], Is.SameAs(first));
    Assert.That(arrayOfTables[1], Is.SameAs(second));
  }

  [Test]
  public void Add_When_Table_Is_Null_Then_Throws_ArgumentNullException()
  {
    var arrayOfTables = new TomlArrayOfTables("products");

    Assert.Throws<ArgumentNullException>(() => arrayOfTables.Add((TomlTable) null!));
  }

  [Test]
  public void GetEnumerator_When_Called_Then_Enumerates_Tables()
  {
    var arrayOfTables = new TomlArrayOfTables("products");
    var first = new TomlTable("first");
    var second = new TomlTable("second");
    arrayOfTables.Add(first);
    arrayOfTables.Add(second);

    Assert.That(arrayOfTables.ToList(), Is.EqualTo(new TomlToken[] {first, second}));
  }

  [Test]
  public void StringIndexer_When_Used_Then_Throws_InvalidOperationException()
  {
    var arrayOfTables = new TomlArrayOfTables("products");

    Assert.Throws<InvalidOperationException>(() => _ = arrayOfTables["key"]);
  }

  [Test]
  public void ToString_When_Called_Then_Starts_With_Type_Name_And_Contains_Name_And_Count()
  {
    var arrayOfTables = new TomlArrayOfTables("products");
    arrayOfTables.Add(new TomlTable("first"));

    var text = arrayOfTables.ToString();

    Assert.That(text, Does.StartWith("TomlArrayOfTables - "));
    Assert.That(text, Does.Contain("products"));
    Assert.That(text, Does.Contain("Count: 1"));
  }
}
