using System;

namespace RStein.TOML
{
  /// <summary>
  /// Exception thrown when TOML parsing or serialization fails.
  /// </summary>
  /// <remarks>
  /// <see cref="TomlSerializerException"/> is raised for any errors encountered during TOML document parsing or serialization.
  /// The exception message includes details about the error, and for parsing errors, it includes line and column information.
  /// </remarks>
  public class TomlSerializerException : Exception
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializerException"/>.
    /// </summary>
    public TomlSerializerException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializerException"/>.
    /// </summary>
    public TomlSerializerException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializerException"/>.
    /// </summary>
    public TomlSerializerException(string message,
             Exception innerException) : base(message, innerException)
    {
    }
  }
}