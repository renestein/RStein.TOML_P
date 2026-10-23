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
}
