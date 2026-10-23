namespace RStein.TOML.Test;

[TestFixture]
internal class TomlKeyValueTest
{
  [Test]
  public void Ctor_When_Key_And_Value_Then_Stores_Them()
  {
    var key = new TomlKey("key");
    var value = new TomlPrimitiveValue(1);

    var keyValue = new TomlKeyValue(key, value);

    Assert.That(keyValue.Key, Is.SameAs(key));
    Assert.That(keyValue.Value, Is.SameAs(value));
    Assert.That(keyValue.TokenType, Is.EqualTo(TomlTokenType.KeyValue));
    Assert.That(keyValue.Comment, Is.Null);
  }

  [Test]
  public void Ctor_When_Comment_Then_Stores_It()
  {
    var comment = new TomlComment(" comment");

    var keyValue = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1), comment);

    Assert.That(keyValue.Comment, Is.SameAs(comment));
  }

  [Test]
  public void Equals_When_Same_Key_And_Value_Then_Returns_True()
  {
    var first = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1));
    var second = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1));

    Assert.That(first.Equals(second), Is.True);
    Assert.That(first.Equals((object) second), Is.True);
  }

  [Test]
  public void Equals_When_Different_Value_Then_Returns_False()
  {
    var first = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1));
    var second = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(2));

    Assert.That(first.Equals(second), Is.False);
    Assert.That(first.Equals((object) second), Is.False);
  }

  [Test]
  public void Equals_When_Different_Key_Then_Returns_False()
  {
    var first = new TomlKeyValue(new TomlKey("key1"), new TomlPrimitiveValue(1));
    var second = new TomlKeyValue(new TomlKey("key2"), new TomlPrimitiveValue(1));

    Assert.That(first.Equals(second), Is.False);
  }

  [Test]
  public void Equals_When_Null_Then_Returns_False()
  {
    var keyValue = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1));

    Assert.That(keyValue.Equals((TomlKeyValue?) null), Is.False);
    object? nullObject = null;
    Assert.That(keyValue.Equals(nullObject), Is.False);
  }

  [Test]
  public void Equals_When_Same_Reference_Then_Returns_True()
  {
    var keyValue = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1));

    Assert.That(keyValue.Equals(keyValue), Is.True);
    Assert.That(keyValue.Equals((object) keyValue), Is.True);
  }

  [Test]
  public void Equals_When_Different_Type_Then_Returns_False()
  {
    var keyValue = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1));

    Assert.That(keyValue.Equals("key"), Is.False);
  }

  [Test]
  public void Equals_When_Only_Comment_Differs_Then_Returns_True()
  {
    var first = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1), new TomlComment("a"));
    var second = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1), new TomlComment("b"));
    var withoutComment = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1));

    Assert.That(first.Equals(second), Is.True);
    Assert.That(first.Equals(withoutComment), Is.True);
    Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
  }

  [Test]
  public void GetHashCode_When_Equal_Key_Values_Then_Hash_Codes_Are_Equal()
  {
    var first = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1));
    var second = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1));

    Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
  }

  [Test]
  public void ToString_When_Called_Then_Contains_Key_And_Value()
  {
    var keyValue = new TomlKeyValue(new TomlKey("key"), new TomlPrimitiveValue(1));

    var text = keyValue.ToString();

    Assert.That(text, Does.Contain("Key: key"));
    Assert.That(text, Does.Contain("Value:"));
  }
}
