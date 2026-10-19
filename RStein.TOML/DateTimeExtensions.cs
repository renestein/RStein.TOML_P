using System;
using System.Globalization;

namespace RStein.TOML
{
  internal static class DateTimeExtensions
  {
    public static string ToTomlLocalDateTime(this in DateTime dateTime) => dateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    public static string ToTomlLocalDate(this in DateTime dateTime) => dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public static string ToTomlLocalTime(this in DateTime dateTime) => dateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
  }
}