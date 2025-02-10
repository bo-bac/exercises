namespace SdrHost.Messages;

public readonly record struct Header(byte Type, ushort Length) : IEquatable<Header>
{
    public Header(ushort header)
        : this((byte)(header >> 13 & 0x07), (ushort)(header & 0x1FFF)) { }

    public bool IsNAK => Type == 0 && Length == 2;
    public ushort ToUShort() => (ushort)(Type << 13 | Length & 0x1FFF);

    public byte[] Serialize()
    {
        ushort header = ToUShort();
        return [(byte)(header & 0xFF), (byte)(header >> 8)]; // LSB first

        //byte lengthLsb = (byte)(Length & 0xFF);  // Lower 8 bits of length
        //byte lengthMsb = (byte)((Length >> 8) & 0x1F); // Upper 5 bits of length (masking to 5 bits)
        //byte headerMsb = (byte)((Type << 5) | lengthMsb); // Type in upper 3 bits, Length MSB in lower 5 bits

        //return [lengthLsb, headerMsb];
    }

    public static async Task<Header?> Deserialize(Stream stream, CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[2]; // Read 16-bit Header
        int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 2), cancellationToken);
        if (bytesRead < 2)
        {
            return null;
        }

        // Parse Header (Little-Endian)
        ushort headerValue = (ushort)(buffer[0] | buffer[1] << 8);

        return new Header(headerValue);
    }
}

//public class Header : IEquatable<Header>
//{
//    public byte Type { get; private init; }

//    public int Length { get; private init; }

//    public Header(byte type, int length)
//    {
//        Type = type;
//        Length = length;
//    }

//    public Header(ushort header)
//    {
//        Type = (byte)((header >> 13) & 0x07); // Extract top 3 bits (bits 13-15)
//        Length = header & 0x1FFF; // Extract lower 13 bits (bits 0-12)
//    }

//    public ushort ToUShort()
//    {
//        return (ushort)((Type << 13) | (Length & 0x1FFF));
//    }

//    public byte[] ToBytes()
//    {
//        ushort header = ToUShort();
//        return [(byte)(header & 0xFF), (byte)(header >> 8)]; // LSB first
//    }

//    #region Equals

//    // IEquatable<Header> implementation
//    public bool Equals(Header? other)
//    {
//        // Check if 'other' is null or if both 'Type' and 'Length' are equal
//        if (other == null) return false;
//        return Type == other.Type && Length == other.Length;
//    }

//    public override bool Equals(object? obj)
//    {
//        return obj is Header header &&
//               Type == header.Type &&
//               Length == header.Length;
//    }

//    public override int GetHashCode()
//    {
//        return HashCode.Combine(Type, Length);
//    }

//    #endregion /Equals 
//}
