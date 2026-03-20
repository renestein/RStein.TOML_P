using System.Globalization;

namespace RStein.TOML.Test;

internal class TomlPrimitiveValueTest
{

  [TestCase("""
            Roses are red
            Violets are blue
            """)]
  [TestCase("""""""
            """"""
            """"""")]
  [TestCase("The quick brown fox jumps over the lazy dog.")]
  [TestCase("""
            "This," she said, "is just a pointless statement."
            """)]
  public void NullableStringOperator_When_Called_Then_Returns_Expected_Value(string expectedValue)
  {
    var tomlValue = new TomlPrimitiveValue(expectedValue, TomlValueType.String);

    var stringValue = (string)tomlValue!;

    Assert.That(stringValue, Is.EqualTo(tomlValue.Value));
  }

  [Test]
  public void NullableIntOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var intValue = (int?)tomlPrimitiveValue;

    Assert.That(intValue , Is.Null);
  }

  [Test]
  public void NullableIntOperator_When_TomlValue_Is_Not_Number_Then_Throws_InvalidCastException()
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", TomlValueType.String);

    Assert.Catch<InvalidCastException>(() => _ = (int?)tomlPrimitiveValue);
  }


  [TestCase("+99", TomlValueType.Integer, TomlDataType.IntegerDec, 99)]
  [TestCase("42", TomlValueType.Integer, TomlDataType.IntegerDec, 42)]
  [TestCase("0", TomlValueType.Integer, TomlDataType.IntegerDec, 0)]
  [TestCase("17", TomlValueType.Integer, TomlDataType.IntegerDec, 17)]
  [TestCase("0xDEADBEE", TomlValueType.Integer, TomlDataType.IntegerHex, 0xDEADBEE)]
  [TestCase("0xdeadbee", TomlValueType.Integer, TomlDataType.IntegerHex, 0xdeadbee)]
  [TestCase("0o01234567", TomlValueType.Integer, TomlDataType.IntegerOct, 342391)]
  [TestCase("0b11010110", TomlValueType.Integer, TomlDataType.IntegerBin, 214)]
  [TestCase( "+1.0", TomlValueType.Float, TomlDataType.Float, 1)]
  [TestCase( "3.1415", TomlValueType.Float, TomlDataType.Float, 3)]
  [TestCase( "-0.01", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase( "1e06", TomlValueType.Float, TomlDataType.Float, 1000000)]
  [TestCase( "0.0", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("+0.0", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("-0.0", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("inf", TomlValueType.Float, TomlDataType.Float, int.MaxValue)]
  [TestCase("+inf", TomlValueType.Float, TomlDataType.Float, int.MaxValue)]
  [TestCase("-inf", TomlValueType.Float, TomlDataType.Float, int.MinValue)]
  [TestCase("nan", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("+nan", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("-nan", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("true", TomlValueType.Boolean, TomlDataType.Unspecified, 1)]
  [TestCase("false", TomlValueType.Boolean, TomlDataType.Unspecified, 0)]
  public void NullableIntOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Integer(string rawValue,
                                                                                       TomlValueType tomlValueType,
                                                                                       TomlDataType tomlDataType,
                                                                                       int expectedNumber)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var intNumber = (int?) tomlPrimitiveValue;

    Assert.That(intNumber, Is.EqualTo(expectedNumber));
  }


  [Test]
  public void NullableLongOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var longValue = (long?)tomlPrimitiveValue; 

    Assert.That(longValue, Is.Null);
  }

  [Test]
  public void NullableLongOperator_When_TomlValue_Is_Not_Number_Then_Throws_InvalidCastException()
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", TomlValueType.String);

    Assert.Catch<InvalidCastException>(() => _ = (long?)tomlPrimitiveValue);
  }

  [TestCase("+99", TomlValueType.Integer, TomlDataType.IntegerDec, 99)]
  [TestCase("42", TomlValueType.Integer, TomlDataType.IntegerDec, 42)]
  [TestCase("0", TomlValueType.Integer, TomlDataType.IntegerDec, 0)]
  [TestCase("17", TomlValueType.Integer, TomlDataType.IntegerDec, 17)]
  [TestCase("0xDEADBEEF", TomlValueType.Integer, TomlDataType.IntegerHex, 0xDEADBEEF)]
  [TestCase("0xdeadbeef", TomlValueType.Integer, TomlDataType.IntegerHex, 0xdeadbeef)]
  [TestCase("0o01234567", TomlValueType.Integer, TomlDataType.IntegerOct, 342391)]
  [TestCase("0b11010110", TomlValueType.Integer, TomlDataType.IntegerBin, 214)]
  [TestCase("+1.0", TomlValueType.Float, TomlDataType.Float, 1)]
  [TestCase("3.1415", TomlValueType.Float, TomlDataType.Float, 3)]
  [TestCase("-0.01", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("1e06", TomlValueType.Float, TomlDataType.Float, 1000000)]
  [TestCase("0.0", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("+0.0", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("-0.0", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("inf", TomlValueType.Float, TomlDataType.Float, long.MaxValue)]
  [TestCase("+inf", TomlValueType.Float, TomlDataType.Float, long.MaxValue)]
  [TestCase("-inf", TomlValueType.Float, TomlDataType.Float, long.MinValue)]
  [TestCase("nan", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("+nan", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("-nan", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("true", TomlValueType.Boolean, TomlDataType.Unspecified, 1)]
  [TestCase("false", TomlValueType.Boolean, TomlDataType.Unspecified, 0)]
  public void NullableLongOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Long(string rawValue,
                                                                                       TomlValueType tomlValueType,
                                                                                       TomlDataType tomlDataType,
                                                                                       long expectedNumber)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var longNumber = (long?)tomlPrimitiveValue;

    Assert.That(longNumber, Is.EqualTo(expectedNumber));
  }

  [Test]
  public void NullableDoubleOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var doubleValue = (double?)tomlPrimitiveValue;

    Assert.That(doubleValue, Is.Null);
  }

  [Test]
  public void NullableDoubleOperator_When_TomlValue_Is_Not_Number_Then_Throws_InvalidCastException()
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", TomlValueType.String);

    Assert.Catch<InvalidCastException>(() => _ = (double?)tomlPrimitiveValue);
  }

  [TestCase("+99", TomlValueType.Integer, TomlDataType.IntegerDec, 99.0)]
  [TestCase("42", TomlValueType.Integer, TomlDataType.IntegerDec, 42.0)]
  [TestCase("0", TomlValueType.Integer, TomlDataType.IntegerDec, 0.0)]
  [TestCase("17", TomlValueType.Integer, TomlDataType.IntegerDec, 17.0)]
  [TestCase("+1.0", TomlValueType.Float, TomlDataType.Float, 1.0)]
  [TestCase("3.1415", TomlValueType.Float, TomlDataType.Float, 3.1415)]
  [TestCase("-0.01", TomlValueType.Float, TomlDataType.Float, -0.01)]
  [TestCase("1e06", TomlValueType.Float, TomlDataType.Float, 1000000.0)]
  [TestCase("0.0", TomlValueType.Float, TomlDataType.Float, 0.0)]
  [TestCase("+0.0", TomlValueType.Float, TomlDataType.Float, 0.0)]
  [TestCase("-0.0", TomlValueType.Float, TomlDataType.Float, 0.0)]
  [TestCase("inf", TomlValueType.Float, TomlDataType.Float, double.PositiveInfinity)]
  [TestCase("+inf", TomlValueType.Float, TomlDataType.Float, double.PositiveInfinity)]
  [TestCase("-inf", TomlValueType.Float, TomlDataType.Float, double.NegativeInfinity)]
  [TestCase("nan", TomlValueType.Float, TomlDataType.Float, double.NaN)]
  [TestCase("+nan", TomlValueType.Float, TomlDataType.Float, double.NaN)]
  [TestCase("-nan", TomlValueType.Float, TomlDataType.Float, double.NaN)]
  public void NullableDoubleOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Double(string rawValue,
                                                                                                 TomlValueType tomlValueType,
                                                                                                 TomlDataType tomlDataType,
                                                                                                 double expectedNumber)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var doubleNumber = (double?)tomlPrimitiveValue;

    Assert.That(doubleNumber, Is.EqualTo(expectedNumber));
  }

  [Test]
  public void NullableFloatOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var floatValue = (float?)tomlPrimitiveValue;

    Assert.That(floatValue, Is.Null);
  }

  [Test]
  public void NullableFloatOperator_When_TomlValue_Is_Not_Number_Then_Throws_InvalidCastException()
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", TomlValueType.String);

    Assert.Catch<InvalidCastException>(() => _ = (float?)tomlPrimitiveValue);
  }

  [TestCase("+99", TomlValueType.Integer, TomlDataType.IntegerDec, 99.0f)]
  [TestCase("42", TomlValueType.Integer, TomlDataType.IntegerDec, 42.0f)]
  [TestCase("0", TomlValueType.Integer, TomlDataType.IntegerDec, 0.0f)]
  [TestCase("17", TomlValueType.Integer, TomlDataType.IntegerDec, 17.0f)]
  [TestCase("+1.0", TomlValueType.Float, TomlDataType.Float, 1.0f)]
  [TestCase("3.1415", TomlValueType.Float, TomlDataType.Float, 3.1415f)]
  [TestCase("-0.01", TomlValueType.Float, TomlDataType.Float, -0.01f)]
  [TestCase("1e06", TomlValueType.Float, TomlDataType.Float, 1000000.0f)]
  [TestCase("0.0", TomlValueType.Float, TomlDataType.Float, 0.0f)]
  [TestCase("+0.0", TomlValueType.Float, TomlDataType.Float, 0.0f)]
  [TestCase("-0.0", TomlValueType.Float, TomlDataType.Float, 0.0f)]
  [TestCase("inf", TomlValueType.Float, TomlDataType.Float, float.PositiveInfinity)]
  [TestCase("+inf", TomlValueType.Float, TomlDataType.Float, float.PositiveInfinity)]
  [TestCase("-inf", TomlValueType.Float, TomlDataType.Float, float.NegativeInfinity)]
  [TestCase("nan", TomlValueType.Float, TomlDataType.Float, float.NaN)]
  [TestCase("+nan", TomlValueType.Float, TomlDataType.Float, float.NaN)]
  [TestCase("-nan", TomlValueType.Float, TomlDataType.Float, float.NaN)]
  public void NullableFloatOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Float(string rawValue,
                                                                                               TomlValueType tomlValueType,
                                                                                               TomlDataType tomlDataType,
                                                                                               float expectedNumber)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var floatNumber = (float?)tomlPrimitiveValue;

    Assert.That(floatNumber, Is.EqualTo(expectedNumber));
  }

  [Test]
  public void NullableDecimalOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var decimalValue = (decimal?)tomlPrimitiveValue;

    Assert.That(decimalValue, Is.Null);
  }

  [Test]
  public void NullableDecimalOperator_When_TomlValue_Is_Not_Number_Then_Throws_InvalidCastException()
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", TomlValueType.String);

    Assert.Catch<InvalidCastException>(() => _ = (decimal?)tomlPrimitiveValue);
  }

  [TestCase("+99", TomlValueType.Integer, TomlDataType.IntegerDec, 99.0)]
  [TestCase("42", TomlValueType.Integer, TomlDataType.IntegerDec, 42.0)]
  [TestCase("0", TomlValueType.Integer, TomlDataType.IntegerDec, 0.0)]
  [TestCase("17", TomlValueType.Integer, TomlDataType.IntegerDec, 17.0)]
  [TestCase("+1.0", TomlValueType.Float, TomlDataType.Float, 1.0)]
  [TestCase("3.1415", TomlValueType.Float, TomlDataType.Float, 3.1415)]
  [TestCase("-0.01", TomlValueType.Float, TomlDataType.Float, -0.01)]
  [TestCase("0.0", TomlValueType.Float, TomlDataType.Float, 0.0)]
  [TestCase("+0.0", TomlValueType.Float, TomlDataType.Float, 0.0)]
  [TestCase("-0.0", TomlValueType.Float, TomlDataType.Float, 0.0)]
  public void NullableDecimalOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Decimal(string rawValue,
                                                                                                   TomlValueType tomlValueType,
                                                                                                   TomlDataType tomlDataType,
                                                                                                   decimal expectedNumber)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var decimalNumber = (decimal?)tomlPrimitiveValue;

    Assert.That(decimalNumber, Is.EqualTo(expectedNumber));
  }

  [Test]
  public void NullableUIntOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var uintValue = (uint?)tomlPrimitiveValue;

    Assert.That(uintValue, Is.Null);
  }

  [Test]
  public void NullableUIntOperator_When_TomlValue_Is_Not_Number_Then_Throws_InvalidCastException()
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", TomlValueType.String);

    Assert.Catch<InvalidCastException>(() => _ = (uint?)tomlPrimitiveValue);
  }

  [TestCase("+99", TomlValueType.Integer, TomlDataType.IntegerDec, 99u)]
  [TestCase("42", TomlValueType.Integer, TomlDataType.IntegerDec, 42u)]
  [TestCase("0", TomlValueType.Integer, TomlDataType.IntegerDec, 0u)]
  [TestCase("17", TomlValueType.Integer, TomlDataType.IntegerDec, 17u)]
  [TestCase("0xDEADBEE", TomlValueType.Integer, TomlDataType.IntegerHex, 0xDEADBEEu)]
  [TestCase("0xdeadbee", TomlValueType.Integer, TomlDataType.IntegerHex, 0xdeadbeeu)]
  [TestCase("0o01234567", TomlValueType.Integer, TomlDataType.IntegerOct, 342391u)]
  [TestCase("0b11010110", TomlValueType.Integer, TomlDataType.IntegerBin, 214u)]
  [TestCase("true", TomlValueType.Boolean, TomlDataType.Unspecified, 1u)]
  [TestCase("false", TomlValueType.Boolean, TomlDataType.Unspecified, 0u)]
  public void NullableUIntOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Uint(string rawValue,
                                                                                       TomlValueType tomlValueType,
                                                                                       TomlDataType tomlDataType,
                                                                                       uint expectedNumber)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var uIntNumber = (uint?)tomlPrimitiveValue;

    Assert.That(uIntNumber, Is.EqualTo(expectedNumber));
  }

  [Test]
  public void NullableULongOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var ulongValue = (ulong?)tomlPrimitiveValue;

    Assert.That(ulongValue, Is.Null);
  }

  [Test]
  public void NullableULongOperator_When_TomlValue_Is_Not_Number_Then_Throws_InvalidCastException()
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", TomlValueType.String);

    Assert.Catch<InvalidCastException>(() => _ = (ulong?)tomlPrimitiveValue);
  }

  [TestCase("+99", TomlValueType.Integer, TomlDataType.IntegerDec, 99UL)]
  [TestCase("42", TomlValueType.Integer, TomlDataType.IntegerDec, 42UL)]
  [TestCase("0", TomlValueType.Integer, TomlDataType.IntegerDec, 0UL)]
  [TestCase("17", TomlValueType.Integer, TomlDataType.IntegerDec, 17UL)]
  [TestCase("0xDEADBEEFF", TomlValueType.Integer, TomlDataType.IntegerHex, 0xDEADBEEFFUL)]
  [TestCase("0xdeadbeeff", TomlValueType.Integer, TomlDataType.IntegerHex, 0xdeadbeeffUL)]
  [TestCase("0o01234567", TomlValueType.Integer, TomlDataType.IntegerOct, 342391UL)]
  [TestCase("0b11010110", TomlValueType.Integer, TomlDataType.IntegerBin, 214UL)]
  [TestCase("true", TomlValueType.Boolean, TomlDataType.Unspecified, 1UL)]
  [TestCase("false", TomlValueType.Boolean, TomlDataType.Unspecified, 0UL)]
  public void NullableUlongOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Ulong(string rawValue,
                                                                                             TomlValueType tomlValueType,
                                                                                             TomlDataType tomlDataType,
                                                                                             ulong expectedNumber)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var uLongNumber = (ulong?)tomlPrimitiveValue;

    Assert.That(uLongNumber, Is.EqualTo(expectedNumber));
  }

  [Test]
  public void NullableByteOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var byteValue = (byte?)tomlPrimitiveValue;

    Assert.That(byteValue, Is.Null);
  }

  [Test]
  public void NullableByteOperator_When_TomlValue_Is_Not_Number_Then_Throws_InvalidCastException()
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", TomlValueType.String);

    Assert.Catch<InvalidCastException>(() => _ = (byte?)tomlPrimitiveValue);
  }

  [TestCase("+99", TomlValueType.Integer, TomlDataType.IntegerDec, 99)]
  [TestCase("42", TomlValueType.Integer, TomlDataType.IntegerDec, 42)]
  [TestCase("0", TomlValueType.Integer, TomlDataType.IntegerDec, 0)]
  [TestCase("17", TomlValueType.Integer, TomlDataType.IntegerDec, 17)]
  [TestCase("0xDE", TomlValueType.Integer, TomlDataType.IntegerHex, 0xDE)]
  [TestCase("0xDEEF", TomlValueType.Integer, TomlDataType.IntegerHex, 0xEF)]
  [TestCase("0xd", TomlValueType.Integer, TomlDataType.IntegerHex, 0xd)]
  [TestCase("0b11010110", TomlValueType.Integer, TomlDataType.IntegerBin, 214)]
  public void NullableByteOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Ulong(string rawValue,
                                                                                              TomlValueType tomlValueType,
                                                                                              TomlDataType tomlDataType,
                                                                                              byte expectedNumber)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var byteNumber = (byte?)tomlPrimitiveValue;

    Assert.That(byteNumber, Is.EqualTo(expectedNumber));
  }

  [Test]
  public void NullableSbyteOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var sByteValue = (sbyte?)tomlPrimitiveValue;

    Assert.That(sByteValue, Is.Null);
  }

  [Test]
  public void NullableSbyteOperator_When_TomlValue_Is_Not_Number_Then_Throws_InvalidCastException()
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", TomlValueType.String);

    Assert.Catch<InvalidCastException>(() => _ = (sbyte?)tomlPrimitiveValue);
  }

  [TestCase("+99", TomlValueType.Integer, TomlDataType.IntegerDec, 99)]
  [TestCase("-99", TomlValueType.Integer, TomlDataType.IntegerDec, -99)]
  [TestCase("42", TomlValueType.Integer, TomlDataType.IntegerDec, 42)]
  [TestCase("0", TomlValueType.Integer, TomlDataType.IntegerDec, 0)]
  [TestCase("17", TomlValueType.Integer, TomlDataType.IntegerDec, 17)]
  [TestCase("0x4B", TomlValueType.Integer, TomlDataType.IntegerHex, 0x4B)]
  [TestCase("0xDE1F", TomlValueType.Integer, TomlDataType.IntegerHex, 0x1F)]
  [TestCase("0xd", TomlValueType.Integer, TomlDataType.IntegerHex, 0xd)]
  [TestCase("0b00010110", TomlValueType.Integer, TomlDataType.IntegerBin, 22)]
  public void NullableSbyteOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Ulong(string rawValue,
                                                                                              TomlValueType tomlValueType,
                                                                                              TomlDataType tomlDataType,
                                                                                              sbyte expectedNumber)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var byteNumber = (sbyte?)tomlPrimitiveValue;

    Assert.That(byteNumber, Is.EqualTo(expectedNumber));
  }

  [Test]
  public void NullableCharOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var charValue = (char?)tomlPrimitiveValue;

    Assert.That(charValue, Is.Null);
  }

  [TestCase("A", TomlValueType.String, TomlDataType.Unspecified, 'A')]
  [TestCase("AB", TomlValueType.String, TomlDataType.Unspecified, 'A')]
  [TestCase("", TomlValueType.String, TomlDataType.Unspecified, null)]
  
  public void NullableCharOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Char(string rawValue,
                                                                                             TomlValueType tomlValueType,
                                                                                             TomlDataType tomlDataType,
                                                                                             char? expectedChar)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var charValue = (char?)tomlPrimitiveValue;

    Assert.That(charValue, Is.EqualTo(expectedChar));
  }

  [Test]
  public void NullableBoolOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var boolValue = (bool?)tomlPrimitiveValue;

    Assert.That(boolValue, Is.Null);
  }

  [TestCase(TomlValueType.String)]
  [TestCase(TomlValueType.DateTime)]
  [TestCase(TomlValueType.Float)]
  public void NullableBoolOperator_When_TomlValue_Is_Not_Bool_Then_Throws_InvalidCastException(TomlValueType imvalidTomlValueType)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", imvalidTomlValueType);

    Assert.Catch<InvalidCastException>(() => _ = (bool?)tomlPrimitiveValue);
  }

  [TestCase("true", TomlValueType.Boolean, TomlDataType.Unspecified, true)]
  [TestCase("false", TomlValueType.Boolean, TomlDataType.Unspecified, false)]
  [TestCase("-99", TomlValueType.Integer, TomlDataType.IntegerDec, false)]
  [TestCase("42", TomlValueType.Integer, TomlDataType.IntegerDec, true)]
  public void NullableBoolOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Ulong(string rawValue,
                                                                                              TomlValueType tomlValueType,
                                                                                              TomlDataType tomlDataType,
                                                                                              bool expectedValue)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var boolValue = (bool?)tomlPrimitiveValue;

    Assert.That(boolValue, Is.EqualTo(expectedValue));
  }

  [Test]
  public void NullableShortOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var shortValue = (short?)tomlPrimitiveValue;

    Assert.That(shortValue, Is.Null);
  }

  [Test]
  public void NullableShortOperator_When_TomlValue_Is_Not_Number_Then_Throws_InvalidCastException()
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", TomlValueType.String);

    Assert.Catch<InvalidCastException>(() => _ = (short?)tomlPrimitiveValue);
  }

  [TestCase("+99", TomlValueType.Integer, TomlDataType.IntegerDec, 99)]
  [TestCase("42", TomlValueType.Integer, TomlDataType.IntegerDec, 42)]
  [TestCase("0", TomlValueType.Integer, TomlDataType.IntegerDec, 0)]
  [TestCase("17", TomlValueType.Integer, TomlDataType.IntegerDec, 17)]
  [TestCase("0xDEA", TomlValueType.Integer, TomlDataType.IntegerHex, 0xDEA)]
  [TestCase("0xdea", TomlValueType.Integer, TomlDataType.IntegerHex, 0xdea)]
  [TestCase("0o012345", TomlValueType.Integer, TomlDataType.IntegerOct, 5349)]
  [TestCase("0b11010110", TomlValueType.Integer, TomlDataType.IntegerBin, 214)]
  [TestCase("+1.0", TomlValueType.Float, TomlDataType.Float, 1)]
  [TestCase("3.1415", TomlValueType.Float, TomlDataType.Float, 3)]
  [TestCase("-0.01", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("1e03", TomlValueType.Float, TomlDataType.Float, 1000)]
  [TestCase("0.0", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("+0.0", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("-0.0", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("inf", TomlValueType.Float, TomlDataType.Float, -1)]
  [TestCase("+inf", TomlValueType.Float, TomlDataType.Float, -1)]
  [TestCase("-inf", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("nan", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("+nan", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("-nan", TomlValueType.Float, TomlDataType.Float, 0)]
  [TestCase("true", TomlValueType.Boolean, TomlDataType.Unspecified, 1)]
  [TestCase("false", TomlValueType.Boolean, TomlDataType.Unspecified, 0)]
  public void NullableShortOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Integer(string rawValue,
                                                                                                 TomlValueType tomlValueType,
                                                                                                 TomlDataType tomlDataType,
                                                                                                 short expectedNumber)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var intNumber = (short?)tomlPrimitiveValue;

    Assert.That(intNumber, Is.EqualTo(expectedNumber));
  }


  [Test]
  public void NullableUshortOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var ushortValue = (ushort?)tomlPrimitiveValue;

    Assert.That(ushortValue, Is.Null);
  }

  [Test]
  public void NullableUshortOperator_When_TomlValue_Is_Not_Number_Then_Throws_InvalidCastException()
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", TomlValueType.String);

    Assert.Catch<InvalidCastException>(() => _ = (ushort?)tomlPrimitiveValue);
  }

  [TestCase("+99", TomlValueType.Integer, TomlDataType.IntegerDec, (ushort)99)]
  [TestCase("42", TomlValueType.Integer, TomlDataType.IntegerDec, (ushort)42)]
  [TestCase("0", TomlValueType.Integer, TomlDataType.IntegerDec, (ushort)0)]
  [TestCase("17", TomlValueType.Integer, TomlDataType.IntegerDec, (ushort)17)]
  [TestCase("0xDEA", TomlValueType.Integer, TomlDataType.IntegerHex, (ushort)0xDEA)]
  [TestCase("0xdead", TomlValueType.Integer, TomlDataType.IntegerHex, (ushort)0xdead)]
  [TestCase("0o012345", TomlValueType.Integer, TomlDataType.IntegerOct, (ushort)5349)]
  [TestCase("0b11010110", TomlValueType.Integer, TomlDataType.IntegerBin, (ushort) 214)]
  [TestCase("true", TomlValueType.Boolean, TomlDataType.Unspecified, (ushort)1)]
  [TestCase("false", TomlValueType.Boolean, TomlDataType.Unspecified, (ushort)0)]
  public void NullableUUshortOperator_When_TomlValue_Is_Valid_Number_Then_Returns_Expected_Ushort(string rawValue,
                                                                                                  TomlValueType tomlValueType,
                                                                                                  TomlDataType tomlDataType,
                                                                                                  ushort expectedNumber)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var ushortNumber = (ushort?)tomlPrimitiveValue;

    Assert.That(ushortNumber, Is.EqualTo(expectedNumber));
  }

  [Test]
  public void NullableGuidOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var guidValue = (Guid?)tomlPrimitiveValue;

    Assert.That(guidValue, Is.Null);
  }

  [TestCase(TomlValueType.DateTime)]
  [TestCase(TomlValueType.Float)]
  public void NullableGuidOperator_When_TomlValue_Is_Not_String_Then_Throws_InvalidCastException(TomlValueType invalidTomlValueType)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", invalidTomlValueType);

    Assert.Catch<InvalidCastException>(() => _ = (Guid?)tomlPrimitiveValue);
  }

  [TestCase("{FF7D9B46-66D8-4125-82DC-34FCA23F0E87}", TomlValueType.String, TomlDataType.Unspecified, "FF7D9B46-66D8-4125-82DC-34FCA23F0E87")]
  [TestCase(@"E2F130D2-568F-4A0A-BA93-E591FE1E22CE", TomlValueType.String, TomlDataType.Unspecified, "E2F130D2-568F-4A0A-BA93-E591FE1E22CE")]
  public void NullableGuidOperator_When_TomlValue_Is_Valid_Guid_Then_Returns_Expected_Guid(string rawValue,
                                                                                           TomlValueType tomlValueType,
                                                                                           TomlDataType tomlDataType,
                                                                                           string expectedGuid)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    var guidValue = (Guid?)tomlPrimitiveValue;

    Assert.That(guidValue != null);
    Assert.That(guidValue!.Value.ToString().ToUpperInvariant(), Is.EqualTo(expectedGuid));
  }


  [TestCase("{FF7D9B4-66D8-4125-82DC-34FCA23F0E87}", TomlValueType.String, TomlDataType.Unspecified)]
  [TestCase(@"E2F130D2-568F-4A0A-BA93-E591FE1E2CE", TomlValueType.String, TomlDataType.Unspecified)]
  [TestCase("", TomlValueType.String, TomlDataType.Unspecified)]
  [TestCase("     sdsd", TomlValueType.String, TomlDataType.Unspecified)]
  public void NullableGuidOperator_When_TomlValue_Is_Invalid_Guid_Then_Throws_InvalidCastException(string rawValue,
                                                                                                   TomlValueType tomlValueType,
                                                                                                   TomlDataType tomlDataType)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, tomlValueType, tomlDataType);

    Assert.Catch<InvalidCastException>(() => _ = (Guid?)tomlPrimitiveValue);
  }

  [Test]
  public void NullableDateTimeOperator_When_TomlValue_Is_Null_Then_Returns_Null()
  {
    TomlPrimitiveValue? tomlPrimitiveValue = null;

    var dateTimeValue = (Guid?)tomlPrimitiveValue;

    Assert.That(dateTimeValue, Is.Null);
  }

  [TestCase(TomlValueType.Float)]
  [TestCase(TomlValueType.Integer)]
  [TestCase(TomlValueType.Boolean)]
  public void NullableDateTimeOperator_When_TomlValue_Is_Not_String_Then_Throws_InvalidCastException(TomlValueType invalidTomlValueType)
  {
    TomlPrimitiveValue tomlPrimitiveValue = new("stringValue", invalidTomlValueType);

    Assert.Catch<InvalidCastException>(() => _ = (DateTime?)tomlPrimitiveValue);
  }

  [TestCase("1979-05-27T07:32:00Z", TomlDataType.OffsetDateTime, "1979-05-27T07:32:00")]
  [TestCase("1979-05-27T00:32:00-07:00", TomlDataType.OffsetDateTime, "1979-05-27T07:32:00")]
  [TestCase("1979-05-27T00:32:00.999999-07:00", TomlDataType.OffsetDateTime, "1979-05-27T07:32:00")]
  [TestCase("1979-05-27 07:32:00Z", TomlDataType.OffsetDateTime, "1979-05-27T07:32:00")]
  [TestCase("1979-05-27T07:32:00", TomlDataType.LocalDateTime, "1979-05-27T07:32:00")]
  [TestCase("1979-05-27T00:32:00.999999", TomlDataType.LocalDateTime, "1979-05-27T00:32:00")]
  [TestCase("1979-05-27", TomlDataType.LocalDate, "1979-05-27T00:00:00")]
  [TestCase("07:32:00", TomlDataType.LocalTime, "07:32:00")]
  [TestCase("01:32:00.999999", TomlDataType.LocalTime, "01:32:00")]
  public void NullableDateTimeOperator_When_TomlValue_Is_Valid_DateTime_Then_Returns_Expected_DateTime(string rawValue,
                                                                                                       TomlDataType tomlDataType,
                                                                                                       string expectedDateTime)
  {

    TomlPrimitiveValue tomlPrimitiveValue = new(rawValue, TomlValueType.DateTime, tomlDataType);

    var dateTimeValue = (DateTime?)tomlPrimitiveValue;

    var dateTimeStringValue = dateTimeValue!.Value.ToString(tomlDataType == TomlDataType.LocalTime
                                                             ? "hh:mm:ss"
                                                             : "s",
                                                           CultureInfo.InvariantCulture);
    Assert.That(dateTimeStringValue, Is.EqualTo(expectedDateTime));
  }

  [TestCase("1979-05-27T07:32:00Z", TomlDataType.OffsetDateTime, "1979-05-27T07:32:00Z")]
  [TestCase("1979-05-27T00:32:00-07:00", TomlDataType.OffsetDateTime, "1979-05-27T07:32:00Z")]
  [TestCase("1979-05-27T07:32:00", TomlDataType.LocalDateTime, "1979-05-27T07:32:00")]
  [TestCase("1979-05-27", TomlDataType.LocalDate, "1979-05-27")]
  [TestCase("07:32:00", TomlDataType.LocalTime, "07:32:00")]
  public void Ctor_When_DateTime_Then_Returns_Expected_Value(string rawValue,
                                                                     TomlDataType tomlDataType,
                                                                     string expectedDateTime)
  {
    var firstTomlDateTimeValue = new TomlPrimitiveValue(rawValue, TomlValueType.DateTime, tomlDataType);
    var dateTime = (DateTime) firstTomlDateTimeValue!;
    var secondTomlDateTimeValue = new TomlPrimitiveValue(dateTime, tomlDataType);

    Assert.That(secondTomlDateTimeValue.Value, Is.EqualTo(expectedDateTime));
  }

  [TestCase("str", TomlDataType.BasicString, TomlDataType.BasicString)]
  [TestCase("str with \r\n new line", TomlDataType.BasicString, TomlDataType.BasicMlString)]
  [TestCase("str with \r\n new line", TomlDataType.BasicMlString, TomlDataType.BasicMlString)]
  [TestCase("str with \n new line", TomlDataType.BasicString, TomlDataType.BasicMlString)]
  [TestCase("str with \n new line", TomlDataType.BasicMlString, TomlDataType.BasicMlString)]
  [TestCase("str", TomlDataType.Unspecified, TomlDataType.BasicString)]
  [TestCase("str with \r\n new line", TomlDataType.Unspecified, TomlDataType.BasicMlString)]
  [TestCase("str with \n new line", TomlDataType.Unspecified, TomlDataType.BasicMlString)]
  [TestCase("str", TomlDataType.LiteralString, TomlDataType.LiteralString)]
  [TestCase("str with \r\n new line", TomlDataType.LiteralMlString, TomlDataType.LiteralMlString)]
  [TestCase("str with \r\n new line", TomlDataType.LiteralString, TomlDataType.LiteralMlString)]
  [TestCase("str with \n new line", TomlDataType.LiteralMlString, TomlDataType.LiteralMlString)]
  [TestCase("str with \n new line", TomlDataType.LiteralString, TomlDataType.LiteralMlString)]
  [TestCase(null!, TomlDataType.BasicString, TomlDataType.BasicString)]
  [TestCase(null!, TomlDataType.BasicMlString, TomlDataType.BasicMlString)]
  [TestCase(null!, TomlDataType.Unspecified, TomlDataType.BasicString)]
  [TestCase("", TomlDataType.BasicString, TomlDataType.BasicString)]
  [TestCase("", TomlDataType.BasicMlString, TomlDataType.BasicMlString)]
  [TestCase("", TomlDataType.Unspecified, TomlDataType.BasicString)]
  public void Ctor_When_String_Then_Returns_Expected_Type_And_Subtype(string rawValue,
                                                                      TomlDataType providedStringSubtype,
                                                                      TomlDataType expectedStringSubtype)
  {
    var stringValue = new TomlPrimitiveValue(rawValue, providedStringSubtype);

    Assert.That(stringValue.Type, Is.EqualTo(TomlValueType.String));
    Assert.That(stringValue.SubType, Is.EqualTo(expectedStringSubtype));
  }


  [TestCase("str", TomlDataType.BasicString)]
  [TestCase("str with \r\n new line", TomlDataType.BasicMlString)]
  [TestCase("str with \r\n new line", TomlDataType.BasicMlString)]
  [TestCase("str with \n new line", TomlDataType.BasicMlString)]
  [TestCase("str with \n new line", TomlDataType.BasicMlString)]
  [TestCase("str",TomlDataType.BasicString)]
  [TestCase("str with \r\n new line", TomlDataType.BasicMlString)]
  [TestCase("str with \n new line", TomlDataType.BasicMlString)]
  [TestCase("str", TomlDataType.LiteralString)]
  [TestCase("str with \r\n new line", TomlDataType.LiteralMlString)]
  [TestCase("str with \r\n new line", TomlDataType.LiteralMlString)]
  [TestCase("str with \n new line", TomlDataType.LiteralMlString)]
  [TestCase("str with \n new line", TomlDataType.LiteralMlString)]
  [TestCase(null!, TomlDataType.BasicString)]
  [TestCase(null!, TomlDataType.BasicMlString)]
  [TestCase(null!, TomlDataType.BasicString)]
  [TestCase("", TomlDataType.BasicString)]
  [TestCase("", TomlDataType.BasicMlString)]
  [TestCase("", TomlDataType.BasicString)]
  public void Ctor_When_String_Then_Returns_Expected_Type_And_StringValueType(string rawValue,
                                                                              TomlDataType expectedStringType)
  {
    var stringValue = new TomlPrimitiveValue(rawValue, expectedStringType);

    Assert.That(stringValue.Type, Is.EqualTo(TomlValueType.String));
    Assert.That(stringValue.SubType, Is.EqualTo(expectedStringType));
  }

}