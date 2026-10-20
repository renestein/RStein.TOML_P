namespace RStein.TOML.Test;

[TestFixture]
internal class TomlSerializerExceptionTest
{
  [Test]
  public void Line_And_Column_When_Created_Without_Position_Then_Are_Null()
  {
    var exception = new TomlSerializerException("message");

    Assert.That(exception.Line, Is.Null);
    Assert.That(exception.Column, Is.Null);
  }

  [Test]
  public void Line_And_Column_When_Created_With_Position_Then_Return_It()
  {
    var exception = new TomlSerializerException("message", 3, 7);

    Assert.That(exception.Line, Is.EqualTo(3));
    Assert.That(exception.Column, Is.EqualTo(7));
  }
}