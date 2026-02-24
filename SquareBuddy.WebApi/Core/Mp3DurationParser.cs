namespace SquareBuddy.WebApi.Core;

public readonly record struct Mp3DurationParseResult(
    double DurationSeconds,
    int ParsedFrameCount,
    int InvalidHeaderCount);

public static class Mp3DurationParser
{
    private const int Id3HeaderLength = 10;
    private const int Mp3FrameHeaderLength = 4;

    public static Mp3DurationParseResult Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < Mp3FrameHeaderLength)
        {
            return new Mp3DurationParseResult(0, 0, 0);
        }

        int offset = 0;
        double durationSeconds = 0;
        int parsedFrameCount = 0;
        int invalidHeaderCount = 0;

        while (offset <= bytes.Length - Mp3FrameHeaderLength)
        {
            if (HasId3v2Header(bytes, offset))
            {
                int id3Length = GetId3v2Length(bytes, offset);
                if (id3Length <= 0)
                {
                    invalidHeaderCount++;
                    offset++;
                    continue;
                }

                int nextOffset = offset + id3Length;
                if (nextOffset <= offset || nextOffset > bytes.Length)
                {
                    break;
                }

                offset = nextOffset;
                continue;
            }

            if (TryParseFrame(bytes, offset, out int frameLength, out double frameDurationSeconds) == false)
            {
                invalidHeaderCount++;
                offset++;
                continue;
            }

            if (frameLength <= 0 || offset + frameLength > bytes.Length)
            {
                invalidHeaderCount++;
                break;
            }

            durationSeconds += frameDurationSeconds;
            parsedFrameCount++;
            offset += frameLength;
        }

        return new Mp3DurationParseResult(durationSeconds, parsedFrameCount, invalidHeaderCount);
    }

    private static bool TryParseFrame(
        ReadOnlySpan<byte> bytes,
        int offset,
        out int frameLength,
        out double frameDurationSeconds)
    {
        frameLength = 0;
        frameDurationSeconds = 0;

        if (offset > bytes.Length - Mp3FrameHeaderLength)
        {
            return false;
        }

        byte b1 = bytes[offset];
        byte b2 = bytes[offset + 1];
        byte b3 = bytes[offset + 2];

        bool hasSync = b1 == 0xFF && (b2 & 0xE0) == 0xE0;
        if (hasSync == false)
        {
            return false;
        }

        int versionBits = (b2 >> 3) & 0x03;
        int layerBits = (b2 >> 1) & 0x03;
        int bitrateIndex = (b3 >> 4) & 0x0F;
        int sampleRateIndex = (b3 >> 2) & 0x03;
        int paddingBit = (b3 >> 1) & 0x01;

        if (versionBits == 1 || layerBits == 0 || bitrateIndex == 0 || bitrateIndex == 15 || sampleRateIndex == 3)
        {
            return false;
        }

        int bitrateKbps = GetBitrateKbps(versionBits, layerBits, bitrateIndex);
        int sampleRateHz = GetSampleRateHz(versionBits, sampleRateIndex);
        int samplesPerFrame = GetSamplesPerFrame(versionBits, layerBits);

        if (bitrateKbps <= 0 || sampleRateHz <= 0 || samplesPerFrame <= 0)
        {
            return false;
        }

        frameLength = GetFrameLengthBytes(versionBits, layerBits, bitrateKbps, sampleRateHz, paddingBit);
        if (frameLength <= 0)
        {
            return false;
        }

        frameDurationSeconds = (double)samplesPerFrame / sampleRateHz;
        return true;
    }

    private static int GetFrameLengthBytes(
        int versionBits,
        int layerBits,
        int bitrateKbps,
        int sampleRateHz,
        int paddingBit)
    {
        int bitrate = bitrateKbps * 1000;

        // Layer I
        if (layerBits == 3)
        {
            return (((12 * bitrate) / sampleRateHz) + paddingBit) * 4;
        }

        // Layer III in MPEG-2 / MPEG-2.5 uses 72, otherwise 144.
        bool isLayerThree = layerBits == 1;
        bool isMpegOne = versionBits == 3;
        int coefficient = isLayerThree && isMpegOne == false ? 72 : 144;
        return ((coefficient * bitrate) / sampleRateHz) + paddingBit;
    }

    private static int GetSamplesPerFrame(int versionBits, int layerBits)
    {
        // Layer I
        if (layerBits == 3)
        {
            return 384;
        }

        // Layer II
        if (layerBits == 2)
        {
            return 1152;
        }

        // Layer III
        return versionBits == 3 ? 1152 : 576;
    }

    private static int GetBitrateKbps(int versionBits, int layerBits, int bitrateIndex)
    {
        // MPEG1 + Layer I
        if (versionBits == 3 && layerBits == 3)
        {
            int[] table = [0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0];
            return table[bitrateIndex];
        }

        // MPEG1 + Layer II
        if (versionBits == 3 && layerBits == 2)
        {
            int[] table = [0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 0];
            return table[bitrateIndex];
        }

        // MPEG1 + Layer III
        if (versionBits == 3 && layerBits == 1)
        {
            int[] table = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0];
            return table[bitrateIndex];
        }

        // MPEG2/2.5 + Layer I
        if (layerBits == 3)
        {
            int[] table = [0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0];
            return table[bitrateIndex];
        }

        // MPEG2/2.5 + Layer II/III
        int[] lowSampleRateTable = [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0];
        return lowSampleRateTable[bitrateIndex];
    }

    private static int GetSampleRateHz(int versionBits, int sampleRateIndex)
    {
        // Base table is MPEG1.
        int[] sampleRates = [44100, 48000, 32000];
        int sampleRate = sampleRates[sampleRateIndex];

        // MPEG2
        if (versionBits == 2)
        {
            return sampleRate / 2;
        }

        // MPEG2.5
        if (versionBits == 0)
        {
            return sampleRate / 4;
        }

        return sampleRate;
    }

    private static bool HasId3v2Header(ReadOnlySpan<byte> bytes, int offset)
    {
        if (offset > bytes.Length - Id3HeaderLength)
        {
            return false;
        }

        return bytes[offset] == (byte)'I' &&
               bytes[offset + 1] == (byte)'D' &&
               bytes[offset + 2] == (byte)'3';
    }

    private static int GetId3v2Length(ReadOnlySpan<byte> bytes, int offset)
    {
        if (offset > bytes.Length - Id3HeaderLength)
        {
            return 0;
        }

        bool hasSynchsafeSize =
            (bytes[offset + 6] & 0x80) == 0 &&
            (bytes[offset + 7] & 0x80) == 0 &&
            (bytes[offset + 8] & 0x80) == 0 &&
            (bytes[offset + 9] & 0x80) == 0;

        if (hasSynchsafeSize == false)
        {
            return 0;
        }

        int tagSize =
            (bytes[offset + 6] << 21) |
            (bytes[offset + 7] << 14) |
            (bytes[offset + 8] << 7) |
            bytes[offset + 9];

        int footerSize = (bytes[offset + 5] & 0x10) != 0 ? Id3HeaderLength : 0;
        return Id3HeaderLength + tagSize + footerSize;
    }
}
