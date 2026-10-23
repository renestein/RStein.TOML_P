namespace RStein.TOML.Test;

[TestFixture]
internal class TomlInlineTableTest
{
  [Test]
  public void Ctor_When_Default_Then_Creates_Anonymous_Inline_Table()
  {
    var inlineTable = new TomlInlineTable();

    Assert.That(inlineTable.TokenType, Is.EqualTo(TomlTokenType.InlineTable));
    Assert.That(inlineTable.Name, Is.EqualTo("Anonymous table"));
    Assert.That(inlineTable.Count, Is.EqualTo(0));
  }

  [Test]
  public void Ctor_When_Default_Then_Sets_Inline_Table_Tags()
  {
    var inlineTable = new TomlInlineTable();

    Assert.That(inlineTable.HasFromInlineTableDefinition, Is.True);
    Assert.That(inlineTable.HasFromKeyDefinition, Is.True);
    Assert.That(inlineTable.HasTopLevelDeclaration, Is.False);
    Assert.That(inlineTable.HasTopLevelDefinition, Is.False);
  }

  [Test]
  public void Ctor_When_Key_Then_Uses_It_As_FullName_And_Sets_Inline_Table_Tags()
  {
    var key = new TomlKey("point");

    var inlineTable = new TomlInlineTable(key);

    Assert.That(inlineTable.FullName, Is.SameAs(key));
    Assert.That(inlineTable.HasFromInlineTableDefinition, Is.True);
    Assert.That(inlineTable.HasFromKeyDefinition, Is.True);
  }

  [Test]
  public void Ctor_When_Name_Then_Uses_It_As_Name()
  {
    var inlineTable = new TomlInlineTable("point");

    Assert.That(inlineTable.Name, Is.EqualTo("point"));
    Assert.That(inlineTable.TokenType, Is.EqualTo(TomlTokenType.InlineTable));
  }

  [Test]
  public void Ctor_When_Called_Then_Is_TomlTable()
  {
    Assert.That(new TomlInlineTable(), Is.InstanceOf<TomlTable>());
  }

  [Test]
  public void Add_When_Values_Then_Behaves_Like_Table()
  {
    var inlineTable = new TomlInlineTable();

    inlineTable.Add("x", 1);
    inlineTable["y"] = 2;

    Assert.That(inlineTable.Count, Is.EqualTo(2));
    Assert.That((int?) inlineTable["x"], Is.EqualTo(1));
    Assert.That((int?) inlineTable["y"], Is.EqualTo(2));
    Assert.That(inlineTable.ContainsKey("x"), Is.True);
  }

  [Test]
  public void Add_When_Untagged_Table_Then_Propagates_Inline_Table_Tags_To_It()
  {
    var inlineTable = new TomlInlineTable();
    var child = new TomlTable("child");

    inlineTable.Add(child);

    Assert.That(child.HasFromInlineTableDefinition, Is.True);
    Assert.That(child.HasFromKeyDefinition, Is.True);
    Assert.That(child.HasTopLevelDefinition, Is.False);
  }

  [Test]
  public void ToString_When_Called_Then_Starts_With_Type_Name_And_Contains_Table_Info()
  {
    var inlineTable = new TomlInlineTable("point");
    inlineTable.Add("x", 1);

    var text = inlineTable.ToString();

    Assert.That(text, Does.StartWith("TomlInlineTable - "));
    Assert.That(text, Does.Contain("Name: point"));
    Assert.That(text, Does.Contain("Count: 1"));
  }
}
