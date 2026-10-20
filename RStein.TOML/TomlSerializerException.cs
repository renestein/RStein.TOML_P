using System;

namespace RStein.TOML
{
  /// <summary>
  /// Exception thrown when TOML parsing or serialization fails.
  /// </summary>
  /// <remarks>
  /// <see cref="TomlSerializerException"/> is raised for any errors encountered during TOML document parsing or serialization.
  /// The exception message describes the error. For errors found while parsing a TOML document, the message states
  /// the position, and <see cref="Line"/> and <see cref="Column"/> are set. For other errors they are <c>null</c>.
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

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializerException"/> for an error found at a position in a TOML document.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The 1-based line number where the error was found.</param>
    /// <param name="column">The 1-based column number where the error was found.</param>
    public TomlSerializerException(string message,
                                   int line,
                                   int column) : base(message)
    {
      Line = line;
      Column = column;
    }

    /// <summary>
    /// Gets the 1-based line of the TOML document where the error was found,
    /// or <c>null</c> if the error is not related to a position in a document.
    /// </summary>
    public int? Line
    {
      get;
    }

    /// <summary>
    /// Gets the 1-based column of the TOML document where the error was found,
    /// or <c>null</c> if the error is not related to a position in a document.
    /// </summary>
    public int? Column
    {
      get;
    }
  }
}