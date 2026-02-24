using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.TOML
{
  /// <summary>
  /// Provides methods for serializing and deserializing TOML data.
  /// </summary>
  public static class TomlSerializer
  {
    /// <summary>
    /// Asynchronously deserializes a TOML string to a <see cref="TomlTable"/>.
    /// </summary>
    /// <param name="toml">The TOML content to deserialize.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized <see cref="TomlTable"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toml"/> is <c>null</c>.</exception>
    /// <exception cref="TomlSerializerException">Thrown when the TOML content cannot be parsed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static Task<TomlTable> DeserializeAsync(string toml,
                                                   CancellationToken cancellationToken = default)
    {
      return DeserializeAsync(toml, TomlSettings.Default, cancellationToken);
    }

    /// <summary>
    /// Asynchronously deserializes a TOML string to a <see cref="TomlTable"/> using the specified settings.
    /// </summary>
    /// <param name="toml">The TOML content to deserialize.</param>
    /// <param name="tomlSettings">The settings to use for deserialization. If <c>null</c>, default settings are used.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized <see cref="TomlTable"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="toml"/> is <c>null</c>.</exception>
    /// <exception cref="TomlSerializerException">Thrown when the TOML content cannot be parsed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task<TomlTable> DeserializeAsync(string toml,
                                                         TomlSettings? tomlSettings,
                                                         CancellationToken cancellationToken = default)
    {
      if (toml == null)
      {
        throw new ArgumentNullException(nameof(toml));
      }

      return await TomlParser.ParseAsync(toml, tomlSettings, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously deserializes TOML content from a stream to a <see cref="TomlTable"/>.
    /// </summary>
    /// <param name="tomlStream">The stream containing TOML content to deserialize.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized <see cref="TomlTable"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tomlStream"/> is <c>null</c>.</exception>
    /// <exception cref="TomlSerializerException">Thrown when the TOML content cannot be parsed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static Task<TomlTable> DeserializeAsync(Stream tomlStream,
                                                   CancellationToken cancellationToken = default)
    {
      return DeserializeAsync(tomlStream, TomlSettings.Default, cancellationToken);
    }

    private static async Task<TomlTable> DeserializeAsync(Stream tomlStream,
                                                          TomlSettings? tomlSettings,
                                                          CancellationToken cancellationToken = default)
    {
      if (tomlStream == null)
      {
        throw new ArgumentNullException(nameof(tomlStream));
      }

      return await TomlParser.ParseAsync(tomlStream, tomlSettings ?? TomlSettings.Default, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="TomlToken"/> to a TOML string using the specified settings.
    /// </summary>
    /// <param name="tomlToken">The TOML token to serialize.</param>
    /// <param name="tomlSettings">The settings to use for serialization. If <c>null</c>, default settings are used.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the serialized TOML string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tomlToken"/> is <c>null</c>.</exception>
    /// <exception cref="TomlSerializerException">Thrown when the token cannot be serialized.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task<string> SerializeToStringAsync(TomlToken tomlToken,
                                                            TomlSettings? tomlSettings,
                                                            CancellationToken cancellationToken)
    {
      using var stringWriter = new StringWriter();
      await serializeToTextWriterAsync(tomlToken, stringWriter, tomlSettings, cancellationToken).ConfigureAwait(false);
      return stringWriter.ToString();
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="TomlToken"/> to a TOML string.
    /// </summary>
    /// <param name="tomlToken">The TOML token to serialize.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the serialized TOML string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tomlToken"/> is <c>null</c>.</exception>
    /// <exception cref="TomlSerializerException">Thrown when the token cannot be serialized.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static Task<string> SerializeToStringAsync(TomlToken tomlToken,
                                                      CancellationToken cancellationToken = default)
    {
      return SerializeToStringAsync(tomlToken, null, cancellationToken);
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="TomlToken"/> to a stream in TOML format.
    /// </summary>
    /// <param name="tomlToken">The TOML token to serialize.</param>
    /// <param name="stream">The stream to write the serialized TOML content to.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tomlToken"/> or <paramref name="stream"/> is <c>null</c>.</exception>
    /// <exception cref="TomlSerializerException">Thrown when the token cannot be serialized.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static Task SerializeAsync(TomlToken tomlToken,
                                      Stream stream,
                                      CancellationToken cancellationToken = default)
    {
      return SerializeAsync(tomlToken, stream, null, cancellationToken);
    }

    /// <summary>
    /// Asynchronously serializes a <see cref="TomlToken"/> to a stream in TOML format using the specified settings.
    /// </summary>
    /// <param name="tomlToken">The TOML token to serialize.</param>
    /// <param name="stream">The stream to write the serialized TOML content to.</param>
    /// <param name="tomlSettings">The settings to use for serialization. If <c>null</c>, default settings are used.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tomlToken"/> or <paramref name="stream"/> is <c>null</c>.</exception>
    /// <exception cref="TomlSerializerException">Thrown when the token cannot be serialized.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task SerializeAsync(TomlToken tomlToken,
                                            Stream stream,
                                            TomlSettings? tomlSettings,
                                            CancellationToken cancellationToken)
    {
      if (stream == null)
      {
        throw new ArgumentNullException(nameof(stream));
      }

      using var textWriter = new StreamWriter(stream, Encoding.UTF8);
      await serializeToTextWriterAsync(tomlToken, textWriter, tomlSettings, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask serializeToTextWriterAsync(TomlToken tomlToken,
                                                              TextWriter textWriter,
                                                              TomlSettings? tomlSettings,
                                                              CancellationToken cancellationToken)
    {
      try
      {
        if (tomlToken == null)
        {
          throw new ArgumentNullException(nameof(tomlToken));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (tomlToken is TomlTable tomlTable && tomlTable.IsUntaggedTable())
        {
          tomlTable.HasTopLevelDefinition = true;
        }

        tomlSettings ??= TomlSettings.Default;
        var serializerVisitor = new TomlSerializerVisitor();
        var writer = new TomlWriter(textWriter);
        var context = new TomlSerializerVisitorContext(writer)
        {
          TomlSettings = tomlSettings,
          CancellationToken = cancellationToken
        };

        await tomlToken.AcceptVisitorAsync(serializerVisitor, context).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        throw;
      }
      catch (TomlSerializerException)
      {
        throw;
      }
      catch (Exception e)
      {
        throw new TomlSerializerException(e.Message, e);
      }
    }
  }
}