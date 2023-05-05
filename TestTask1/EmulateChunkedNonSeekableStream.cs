namespace TestTask1;

public class EmulateChunkedNonSeekableStream : MemoryStream
{
    private readonly int _chunkSize;

    public EmulateChunkedNonSeekableStream(int chunkSize) => _chunkSize = chunkSize;
    public EmulateChunkedNonSeekableStream(byte[] buffer, int chunkSize) : base(buffer) => _chunkSize = chunkSize;

    public override int Read(byte[] buffer, int offset, int count) =>
        base.Read(buffer, offset, Math.Min(_chunkSize, count));

    public override int Read(Span<byte> buffer) =>
        base.Read(buffer.Slice(0, Math.Min(_chunkSize, buffer.Length)));

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        base.ReadAsync(buffer, offset, Math.Min(_chunkSize, count), cancellationToken);
    
    
    public override bool CanSeek => false;

    public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
    
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}