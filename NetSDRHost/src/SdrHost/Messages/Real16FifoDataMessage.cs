using System.IO;

namespace SdrHost.Messages;

public class Real16FifoDataMessage
{
    public required ushort SeqN { get; set; }
    public required byte Type { get; set; } = (byte)MessageTypes.TargetToHost.Data0;
    public byte[]? Data { get; set; } // LSB

    public bool BigPackets { get; set; } = false;

    public Header GetHeader()
    {
        return new Header(
            Type: Type,
            // Header (2 bytes) + SeqN (2 bytes) + Data
            Length: (ushort)(4 + (BigPackets ? 1024 : 512))
        );
    }

    public byte[] Serialize()
    {
        var header = GetHeader();

        // 1. Serialize Header (16-bit, Little Endian)
        byte[] headerBytes = header.Serialize();

        // 2. Serialize Sequence Number (16-bit, Little Endian)
        byte[] seqNBytes =
        [
            (byte)(SeqN & 0xFF),
            (byte)(SeqN >> 8 & 0xFF),
        ];

        // 3. Serialize Data (512 or 1024 bytes)
        byte[] dataBytes = Data ?? new byte[header.Length - 4];

        // 4. Combine all parts (Header + SeqN + Data)
        byte[] result = new byte[header.Length];
        Buffer.BlockCopy(headerBytes, 0, result, 0, headerBytes.Length); // Copy Header
        Buffer.BlockCopy(seqNBytes, 0, result, headerBytes.Length, seqNBytes.Length); // Copy Code
        Buffer.BlockCopy(dataBytes, 0, result, headerBytes.Length + seqNBytes.Length, dataBytes.Length); // Copy Parameter

        return result;
    }

    public static async Task<Real16FifoDataMessage?> Deserialize(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[2]; // Read 16-bit Header

        // Read first 2 bytes (Header)
        int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 2), cancellationToken);
        if (bytesRead < 2)
        {
            //throw new IOException("Failed to read the complete message header.");
            return null;
        }

        // Parse Header (Little-Endian)
        ushort headerValue = (ushort)(buffer[0] | buffer[1] << 8);
        Header header = new(headerValue);

        ushort seqN = 0;
        byte[]? data = null;
        if (header.Length > 2)
        {
            buffer = new byte[header.Length - 2];
            // Read SeqN+Data bytes
            _ = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);

            // Parse Code 16-bit (Little-Endian)
            seqN = (ushort)(buffer[0] | buffer[1] << 8);

            // Read Parameter bytes (keep them like it is)
            data = buffer[2..];
        }

        return new Real16FifoDataMessage
        {
            SeqN = seqN,
            Type = header.Type,
            Data = data,
            BigPackets = data?.Length == 1024
        };
    }

    public static Real16FifoDataMessage? Deserialize(byte[] buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.Length < 2) return null;

        // Read first 2 bytes (Header)
        ReadOnlySpan<byte> headerTwoBytes = buffer.AsSpan(0, 2); // Read 16-bit Header        

        // Parse Header (Little-Endian)
        ushort headerValue = (ushort)(headerTwoBytes[0] | headerTwoBytes[1] << 8);
        Header header = new(headerValue);

        ushort seqN = 0;
        ReadOnlySpan<byte> data = default;
        if (header.Length > 2)
        {
            // Read SeqN+Data bytes
            ReadOnlySpan<byte> payload = buffer.AsSpan(2, header.Length - 2);

            // Parse Code 16-bit (Little-Endian)
            seqN = (ushort)(payload[0] | payload[1] << 8);

            // Read Parameter bytes (keep them like it is)
            data = payload[2..];
        }

        return new Real16FifoDataMessage
        {
            SeqN = seqN,
            Type = header.Type,
            Data = data.ToArray(),
            BigPackets = data.Length == 1024
        };
    }
}
