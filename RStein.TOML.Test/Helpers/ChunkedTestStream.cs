namespace RStein.TOML.Test.Helpers;
internal sealed class ChunkedTestStream : Stream
{
  private readonly byte[] _data;
  private readonly int _maxChunkSize;
  private readonly bool _allowSyncReads;
  private readonly int _stallAfterBytes;
  private int _position;

  public ChunkedTestStream(byte[] data,
                           int maxChunkSize,
                           bool allowSyncReads = true,
                           int stallAfterBytes = int.MaxValue)
  {
    _data = data;
    _maxChunkSize = maxChunkSize;
    _allowSyncReads = allowSyncReads;
    _stallAfterBytes = stallAfterBytes;
  }

  public int SyncReadCount
  {
    get;
    private set;
  }

  public override bool CanRead => true;

  public override bool CanSeek => false;

  public override bool CanWrite => false;

  public override long Length => throw new NotSupportedException();

  public override long Position
  {
    get => throw new NotSupportedException();
    set => throw new NotSupportedException();
  }

  public override void Flush()
  {
  }

  public override int Read(byte[] buffer, int offset, int count)
  {
    ++SyncReadCount;
    if (!_allowSyncReads)
    {
      throw new InvalidOperationException("Synchronous reads are disallowed.");
    }

    return copyNextChunk(buffer.AsSpan(offset, count));
  }

  public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
  {
    await Task.Yield();
    cancellationToken.ThrowIfCancellationRequested();
    if (_position >= _stallAfterBytes)
    {
      await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
    }

    return copyNextChunk(buffer.Span);
  }

  public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
  {
    return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
  }

  public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

  public override void SetLength(long value) => throw new NotSupportedException();

  public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

  private int copyNextChunk(Span<byte> destination)
  {
    var count = Math.Min(Math.Min(destination.Length, _maxChunkSize), _data.Length - _position);
    _data.AsSpan(_position, count).CopyTo(destination);
    _position += count;
    return count;
  }
}
