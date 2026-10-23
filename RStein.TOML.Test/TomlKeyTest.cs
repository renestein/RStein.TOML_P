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

  [Test]
  public void Ctor_When_Raw_Key_Is_Null_Then_Throws_ArgumentNullException()
  {
    Assert.Throws<ArgumentNullException>(() => _ = new TomlKey(null!));
  }

  [Test]
  public void Ctor_When_Simple_Key_Then_Is_Unquoted_Key_Token()
  {
    var key = new TomlKey("key1");

    Assert.That(key.TokenType, Is.EqualTo(TomlTokenType.Key));
    Assert.That(key.Type, Is.EqualTo(TomlKeyType.SimpleUnquoted));
    Assert.That(key.IsUnquotedKey, Is.True);
    Assert.That(key.IsQuotedKey, Is.False);
  }

  [TestCase("a b")]
  [TestCase("a.b")]
  [TestCase("")]
  public void Ctor_When_Key_Needs_Quoting_Then_Is_Quoted_Basic_String_Key(string rawKey)
  {
    var key = new TomlKey(rawKey);

    Assert.That(key.Type, Is.EqualTo(TomlKeyType.SimpleQuotedBasicString));
    Assert.That(key.IsQuotedKey, Is.True);
    Assert.That(key.IsQuotedBasicStringKey, Is.True);
    Assert.That(key.IsQuotedLiteralStringKey, Is.False);
  }

  [Test]
  public void Ctor_When_Type_Is_Specified_Then_Uses_It()
  {
    var key = new TomlKey("abc", TomlKeyType.SimpleQuotedLiteralString);

    Assert.That(key.Type, Is.EqualTo(TomlKeyType.SimpleQuotedLiteralString));
    Assert.That(key.IsQuotedLiteralStringKey, Is.True);
    Assert.That(key.IsQuotedBasicStringKey, Is.False);
  }

  [Test]
  public void IsDottedKey_When_Next_Key_Part_Is_Set_Then_Returns_True()
  {
    var key = new TomlKey("a") {NextKeyPart = new TomlKey("b")};

    Assert.That(key.IsDottedKey, Is.True);
    Assert.That(new TomlKey("a").IsDottedKey, Is.False);
  }

  [Test]
  public void LastKeyPart_When_Simple_Key_Then_Returns_Itself()
  {
    var key = new TomlKey("a");

    Assert.That(key.LastKeyPart, Is.SameAs(key));
  }

  [Test]
  public void LastKeyPart_When_Dotted_Key_Then_Returns_Last_Part()
  {
    var last = new TomlKey("c");
    var key = new TomlKey("a")
    {
      NextKeyPart = new TomlKey("b")
      {
        NextKeyPart = last
      }
    };

    Assert.That(key.LastKeyPart, Is.SameAs(last));
  }

  [Test]
  public void Clone_When_Dotted_Key_Then_Returns_Deep_Copy_With_Same_Content()
  {
    var key = new TomlKey("a")
    {
      NextKeyPart = new TomlKey("b c", TomlKeyType.SimpleQuotedLiteralString)
    };

    var clone = key.Clone();

    Assert.That(clone, Is.Not.SameAs(key));
    Assert.That(clone.NextKeyPart, Is.Not.SameAs(key.NextKeyPart));
    Assert.That(clone.ToString(), Is.EqualTo(key.ToString()));
    Assert.That(clone.NextKeyPart!.Type, Is.EqualTo(TomlKeyType.SimpleQuotedLiteralString));
  }

  [Test]
  public void Clone_When_Original_Is_Modified_Then_Clone_Is_Not_Affected()
  {
    var key = new TomlKey("a") {NextKeyPart = new TomlKey("b")};
    var clone = key.Clone();

    key.NextKeyPart = null;

    Assert.That(clone.IsDottedKey, Is.True);
  }

  [Test]
  public void Equals_When_Same_Raw_Key_Then_Returns_True()
  {
    var first = new TomlKey("key");
    var second = new TomlKey("key");

    Assert.That(first.Equals(second), Is.True);
    Assert.That(first.Equals((object) second), Is.True);
  }

  [Test]
  public void Equals_When_Different_Raw_Key_Then_Returns_False()
  {
    var first = new TomlKey("key1");
    var second = new TomlKey("key2");

    Assert.That(first.Equals(second), Is.False);
    Assert.That(first.Equals((object) second), Is.False);
  }

  [Test]
  public void Equals_When_Dotted_Keys_Differ_In_Later_Part_Then_Returns_False()
  {
    var first = new TomlKey("a")
    {
      NextKeyPart = new TomlKey("b")
    };
    var second = new TomlKey("a")
    {
      NextKeyPart = new TomlKey("c")
    };

    Assert.That(first.Equals(second), Is.False);
  }

  [Test]
  public void Equals_When_One_Key_Is_Dotted_Then_Returns_False()
  {
    var first = new TomlKey("a")
    {
      NextKeyPart = new TomlKey("b")
    };
    var second = new TomlKey("a");

    Assert.That(first.Equals(second), Is.False);
    Assert.That(second.Equals(first), Is.False);
  }

  [Test]
  public void Equals_When_Null_Then_Returns_False()
  {
    var key = new TomlKey("key");

    Assert.That(key.Equals((TomlKey?) null), Is.False);
    object? nullObject = null;
    Assert.That(key.Equals(nullObject), Is.False);
  }

  [Test]
  public void Equals_When_Different_Type_Then_Returns_False()
  {
    Assert.That(new TomlKey("key").Equals("key"), Is.False);
  }

  [Test]
  public void GetHashCode_When_Equal_Keys_Then_Hash_Codes_Are_Equal()
  {
    var first = new TomlKey("a")
    {
      NextKeyPart = new TomlKey("b")
    };
    var second = new TomlKey("a")
    {
      NextKeyPart = new TomlKey("b")
    };

    Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
  }
}
