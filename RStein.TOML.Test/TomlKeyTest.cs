namespace RStein.TOML.Test;

[TestFixture]
internal class TomlKeyTest
{
  [Test]
  public void ToString_When_Unquoted_Key_Then_Returns_Raw_Key()
  {
    var key = new TomlKey("key");

    Assert.That(key.ToString(), Is.EqualTo("key"));
  }

  [Test]
  public void ToString_When_Dotted_Key_Then_Returns_All_Parts_Separated_By_Dot()
  {
    var key = new TomlKey("a")
    {
      NextKeyPart = new TomlKey("b")
      {
        NextKeyPart = new TomlKey("c")
      }
    };

    Assert.That(key.ToString(), Is.EqualTo("a.b.c"));
  }

  [Test]
  public void ToString_When_Part_Contains_Dot_Then_Part_Stays_Quoted()
  {
    var key = new TomlKey("a")
    {
      NextKeyPart = new TomlKey("b.c")
    };

    Assert.That(key.ToString(), Is.EqualTo("a.\"b.c\""));
  }

  [Test]
  public void ToString_When_Literal_String_Key_Then_Uses_Single_Quotes()
  {
    var key = new TomlKey("x y", TomlKeyType.SimpleQuotedLiteralString);

    Assert.That(key.ToString(), Is.EqualTo("'x y'"));
  }

  [Test]
  public void ToString_When_Basic_String_Key_Has_Quote_Then_Escapes_It()
  {
    var key = new TomlKey("a\"b", TomlKeyType.SimpleQuotedBasicString);

    Assert.That(key.ToString(), Is.EqualTo("\"a\\\"b\""));
  }
}
