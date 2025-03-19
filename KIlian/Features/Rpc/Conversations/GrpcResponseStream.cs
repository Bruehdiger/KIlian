using System.Buffers;
using Google.Protobuf;
using Grpc.Core;
using KIlian.Generated.Rpc.Conversations;

namespace KIlian.Features.Rpc.Conversations;

public class GrpcResponseStream(IServerStreamWriter<TrainingDataDto> writer) : Stream
{
    private ArrayBufferWriter<byte> _buffer = new();
    
    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await writer.WriteAsync(new()
        {
            Chunk = ByteString.CopyFrom(_buffer.WrittenSpan)
        }, cancellationToken);
        _buffer = new();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _buffer.Write(buffer.AsSpan(offset, count));
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => -1;
    public override long Position { get; set; }
}