using System;
using System.Globalization;

namespace RStein.TOML
{
  internal static class DateTimeExtensions
  {
    public static string ToTomlLocalDateTime(this in DateTime dateTime) => dateTime.ToString("yyyy-MM-ddThh:mm:ss", CultureInfo.InvariantCulture);
    public static string ToTomlLocalDate(this in DateTime dateTime) => dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static string ToTomlLocalTime(this in DateTime dateTime) => dateTime.ToString("hh:mm:ss", CultureInfo.InvariantCulture);
  }
}