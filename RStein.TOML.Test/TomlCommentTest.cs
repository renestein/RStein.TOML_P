namespace RStein.TOML.Test;

[TestFixture]
internal class TomlCommentTest
{
  [Test]
  public void Ctor_When_Value_Then_Stores_It_And_Sets_Comment_Token_Type()
  {
    var comment = new TomlComment(" a comment");

    Assert.That(comment.Value, Is.EqualTo(" a comment"));
    Assert.That(comment.TokenType, Is.EqualTo(TomlTokenType.Comment));
  }

  [Test]
  public void Ctor_When_Value_Is_Null_Then_Value_Is_Empty()
  {
    var comment = new TomlComment(null!);

    Assert.That(comment.Value, Is.EqualTo(string.Empty));
  }

  [Test]
  public void Equals_When_Same_Value_Then_Returns_True()
  {
    var first = new TomlComment("text");
    var second = new TomlComment("text");

    Assert.That(first.Equals(second), Is.True);
    Assert.That(first.Equals((object) second), Is.True);
  }

  [Test]
  public void Equals_When_Different_Value_Then_Returns_False()
  {
    var first = new TomlComment("text");
    var second = new TomlComment("other");

    Assert.That(first.Equals(second), Is.False);
    Assert.That(first.Equals((object) second), Is.False);
  }

  [Test]
  public void Equals_When_Value_Differs_Only_By_Case_Then_Returns_False()
  {
    var first = new TomlComment("text");
    var second = new TomlComment("TEXT");

    Assert.That(first.Equals(second), Is.False);
  }

  [Test]
  public void Equals_When_Null_Then_Returns_False()
  {
    var comment = new TomlComment("text");

    Assert.That(comment.Equals((TomlComment?) null), Is.False);
    object? nullObject = null;
    Assert.That(comment.Equals(nullObject), Is.False);
  }

  [Test]
  public void Equals_When_Same_Reference_Then_Returns_True()
  {
    var comment = new TomlComment("text");

    Assert.That(comment.Equals(comment), Is.True);
    Assert.That(comment.Equals((object) comment), Is.True);
  }

  [Test]
  public void Equals_When_Different_Type_Then_Returns_False()
  {
    var comment = new TomlComment("text");

    Assert.That(comment.Equals("text"), Is.False);
    Assert.That(comment.Equals(new TomlPrimitiveValue("text")), Is.False);
  }

  [Test]
  public void GetHashCode_When_Equal_Comments_Then_Hash_Codes_Are_Equal()
  {
    var first = new TomlComment("text");
    var second = new TomlComment("text");

    Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
  }

  [Test]
  public void Tokens_When_Called_Then_Is_Empty()
  {
    var comment = new TomlComment("text");

    Assert.That(comment.Tokens, Is.Empty);
  }
}
