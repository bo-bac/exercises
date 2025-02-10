namespace SdrHost.Messages;

public class ControlMessage(ushort code, byte type, byte[]? parameter = null)
{
    public ushort Code { get; } = code;
    public byte Type { get; } = type;
    public byte[] Parameter { get; protected init; } = parameter ?? [];

    public bool IsNAK => GetHeader().IsNAK && Code == 0 && Parameter.Length == 0;

    public Header GetHeader()
    {
        return new Header(
            Type: Type,
            Length: Type == 0 && Code == 0 && Parameter.Length == 0
                ? (ushort)2
                : (ushort)(4 + Parameter.Length)
        );
    }

    public byte[] Serialize()
    {
        // 1. Serialize Header (16-bit, Little Endian)
        byte[] headerBytes = GetHeader().Serialize();

        if (IsNAK)
        {
            return headerBytes;
        }

        // 2. Serialize Code (16-bit, Little Endian)
        byte[] codeBytes =
        [
            (byte)(Code & 0xFF),  // LSB
            (byte)(Code >> 8 & 0xFF),  // MSB
        ];

        // 3. Serialize Parameter (Variable-length bytes)
        byte[] parameterBytes = Parameter ?? [];

        // 4. Combine all parts (Header + Code + Parameter)
        byte[] result = new byte[headerBytes.Length + codeBytes.Length + parameterBytes.Length];
        Buffer.BlockCopy(headerBytes, 0, result, 0, headerBytes.Length); 
        Buffer.BlockCopy(codeBytes, 0, result, headerBytes.Length, codeBytes.Length);
        Buffer.BlockCopy(parameterBytes, 0, result, headerBytes.Length + codeBytes.Length, parameterBytes.Length);

        return result;
    }

    public static async Task<ControlMessage?> Deserialize(Stream stream, CancellationToken cancellationToken = default)
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

        ushort code = 0;
        byte[]? parameter = null;
        if (header.Length > 2)
        {
            buffer = new byte[header.Length - 2];
            // Read Code+Parameter bytes
            _ = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);

            // Parse Code 16-bit (Little-Endian)
            code = (ushort)(buffer[0] | buffer[1] << 8);

            // Read Parameter bytes (keep them like it is)
            parameter = buffer[2..];
        }

        return new ControlMessage(code, header.Type, parameter);
    }
}
