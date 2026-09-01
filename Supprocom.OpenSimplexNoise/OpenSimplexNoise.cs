// Source: https://gist.github.com/digitalshadow/134a3a02b67cecd72181

using System.Runtime.CompilerServices;

namespace Supprocom.OpenSimplexNoise;

public partial class OpenSimplexNoise
{
    private const double STRETCH_2D = -0.211324865405187;
    private const double STRETCH_3D = -1.0 / 6.0;
    private const double STRETCH_4D = -0.138196601125011;
    private const double SQUISH_2D = 0.366025403784439;
    private const double SQUISH_3D = 1.0 / 3.0;
    private const double SQUISH_4D = 0.309016994374947;
    private const double NORM_2D = 1.0 / 47.0;
    private const double NORM_3D = 1.0 / 103.0;
    private const double NORM_4D = 1.0 / 30.0;
    private const int LookupLength2D = 64;
    private const int LookupLength3D = 2048;
    private const int LookupLength4D = 1_048_576;

    public const int PermutationTableLength = 256;
    public const int SourceScratchLength = 256;

    private readonly byte[] perm;
    private readonly byte[] perm2D;
    private readonly byte[] perm3D;
    private readonly byte[] perm4D;

    public OpenSimplexNoise()
        : this(DateTime.Now.Ticks)
    {
    }

    public OpenSimplexNoise(long seed)
    {
        perm = new byte[PermutationTableLength];
        perm2D = new byte[PermutationTableLength];
        perm3D = new byte[PermutationTableLength];
        perm4D = new byte[PermutationTableLength];
        var source = new byte[SourceScratchLength];

        Initialize(seed, perm, perm2D, perm3D, perm4D, source);
    }

    public static void Initialize(
        long seed,
        Span<byte> permutation,
        Span<byte> permutation2D,
        Span<byte> permutation3D,
        Span<byte> permutation4D,
        Span<byte> sourceScratch)
    {
        ValidateInitializationBuffers(
            permutation,
            permutation2D,
            permutation3D,
            permutation4D,
            sourceScratch);

        permutation = permutation[..PermutationTableLength];
        permutation2D = permutation2D[..PermutationTableLength];
        permutation3D = permutation3D[..PermutationTableLength];
        permutation4D = permutation4D[..PermutationTableLength];
        sourceScratch = sourceScratch[..SourceScratchLength];

        for (int index = 0; index < SourceScratchLength; index++)
        {
            sourceScratch[index] = (byte)index;
        }

        seed = unchecked(seed * 6364136223846793005L + 1442695040888963407L);
        seed = unchecked(seed * 6364136223846793005L + 1442695040888963407L);
        seed = unchecked(seed * 6364136223846793005L + 1442695040888963407L);

        for (int index = PermutationTableLength - 1; index >= 0; index--)
        {
            seed = unchecked(seed * 6364136223846793005L + 1442695040888963407L);
            int sourceIndex = (int)((seed + 31) % (index + 1));
            if (sourceIndex < 0)
            {
                sourceIndex += index + 1;
            }

            permutation[index] = sourceScratch[sourceIndex];
            permutation2D[index] = (byte)(permutation[index] & 0x0E);
            permutation3D[index] = (byte)(permutation[index] % 24 * 3);
            permutation4D[index] = (byte)(permutation[index] & 0xFC);
            sourceScratch[sourceIndex] = sourceScratch[index];
        }
    }

    public double Evaluate(double x, double y)
    {
        return Evaluate(perm, perm2D, x, y);
    }

    public double Evaluate(double x, double y, double z)
    {
        return Evaluate(perm, perm3D, x, y, z);
    }

    public double Evaluate(double x, double y, double z, double w)
    {
        return Evaluate(perm, perm4D, x, y, z, w);
    }

    public static double Evaluate(
        ReadOnlySpan<byte> permutation,
        ReadOnlySpan<byte> permutation2D,
        double x,
        double y)
    {
        ValidateEvaluationTables(permutation, permutation2D);

        var stretchOffset = (x + y) * STRETCH_2D;
        var xs = x + stretchOffset;
        var ys = y + stretchOffset;

        var xsb = FastFloor(xs);
        var ysb = FastFloor(ys);

        var squishOffset = (xsb + ysb) * SQUISH_2D;
        var dx0 = x - (xsb + squishOffset);
        var dy0 = y - (ysb + squishOffset);

        var xins = xs - xsb;
        var yins = ys - ysb;
        var inSum = xins + yins;

        var hash =
            (int)(xins - yins + 1) |
            (int)inSum << 1 |
            (int)(inSum + yins) << 2 |
            (int)(inSum + xins) << 4;

        if (!TryGetChain(
                hash,
                LookupLength2D,
                LookupPairs2D,
                ChainMetadata2D,
                out int recordStart,
                out int recordCount))
        {
            return 0.0;
        }

        ReadOnlySpan<long> contributions = Contributions2D;
        ReadOnlySpan<double> gradients = Gradients2D;
        var value = 0.0;

        for (int record = 0; record < recordCount; record++)
        {
            int offset = (recordStart + record) * 4;
            int xsv = (int)contributions[offset];
            int ysv = (int)contributions[offset + 1];
            var dx = dx0 + BitConverter.Int64BitsToDouble(contributions[offset + 2]);
            var dy = dy0 + BitConverter.Int64BitsToDouble(contributions[offset + 3]);
            var attn = 2 - dx * dx - dy * dy;

            if (attn > 0)
            {
                var px = xsb + xsv;
                var py = ysb + ysv;
                var gradientIndex = permutation2D[permutation[px & 0xFF] + py & 0xFF];
                var valuePart = gradients[gradientIndex] * dx + gradients[gradientIndex + 1] * dy;

                attn *= attn;
                value += attn * attn * valuePart;
            }
        }

        return value * NORM_2D;
    }

    public static double Evaluate(
        ReadOnlySpan<byte> permutation,
        ReadOnlySpan<byte> permutation3D,
        double x,
        double y,
        double z)
    {
        ValidateEvaluationTables(permutation, permutation3D);

        var stretchOffset = (x + y + z) * STRETCH_3D;
        var xs = x + stretchOffset;
        var ys = y + stretchOffset;
        var zs = z + stretchOffset;

        var xsb = FastFloor(xs);
        var ysb = FastFloor(ys);
        var zsb = FastFloor(zs);

        var squishOffset = (xsb + ysb + zsb) * SQUISH_3D;
        var dx0 = x - (xsb + squishOffset);
        var dy0 = y - (ysb + squishOffset);
        var dz0 = z - (zsb + squishOffset);

        var xins = xs - xsb;
        var yins = ys - ysb;
        var zins = zs - zsb;
        var inSum = xins + yins + zins;

        var hash =
            (int)(yins - zins + 1) |
            (int)(xins - yins + 1) << 1 |
            (int)(xins - zins + 1) << 2 |
            (int)inSum << 3 |
            (int)(inSum + zins) << 5 |
            (int)(inSum + yins) << 7 |
            (int)(inSum + xins) << 9;

        if (!TryGetChain(
                hash,
                LookupLength3D,
                LookupPairs3D,
                ChainMetadata3D,
                out int recordStart,
                out int recordCount))
        {
            return 0.0;
        }

        ReadOnlySpan<long> contributions = Contributions3D;
        ReadOnlySpan<double> gradients = Gradients3D;
        var value = 0.0;

        for (int record = 0; record < recordCount; record++)
        {
            int offset = (recordStart + record) * 6;
            int xsv = (int)contributions[offset];
            int ysv = (int)contributions[offset + 1];
            int zsv = (int)contributions[offset + 2];
            var dx = dx0 + BitConverter.Int64BitsToDouble(contributions[offset + 3]);
            var dy = dy0 + BitConverter.Int64BitsToDouble(contributions[offset + 4]);
            var dz = dz0 + BitConverter.Int64BitsToDouble(contributions[offset + 5]);
            var attn = 2 - dx * dx - dy * dy - dz * dz;

            if (attn > 0)
            {
                var px = xsb + xsv;
                var py = ysb + ysv;
                var pz = zsb + zsv;
                var gradientIndex = permutation3D[
                    permutation[permutation[px & 0xFF] + py & 0xFF] + pz & 0xFF];
                var valuePart =
                    gradients[gradientIndex] * dx +
                    gradients[gradientIndex + 1] * dy +
                    gradients[gradientIndex + 2] * dz;

                attn *= attn;
                value += attn * attn * valuePart;
            }
        }

        return value * NORM_3D;
    }

    public static double Evaluate(
        ReadOnlySpan<byte> permutation,
        ReadOnlySpan<byte> permutation4D,
        double x,
        double y,
        double z,
        double w)
    {
        ValidateEvaluationTables(permutation, permutation4D);

        var stretchOffset = (x + y + z + w) * STRETCH_4D;
        var xs = x + stretchOffset;
        var ys = y + stretchOffset;
        var zs = z + stretchOffset;
        var ws = w + stretchOffset;

        var xsb = FastFloor(xs);
        var ysb = FastFloor(ys);
        var zsb = FastFloor(zs);
        var wsb = FastFloor(ws);

        var squishOffset = (xsb + ysb + zsb + wsb) * SQUISH_4D;
        var dx0 = x - (xsb + squishOffset);
        var dy0 = y - (ysb + squishOffset);
        var dz0 = z - (zsb + squishOffset);
        var dw0 = w - (wsb + squishOffset);

        var xins = xs - xsb;
        var yins = ys - ysb;
        var zins = zs - zsb;
        var wins = ws - wsb;
        var inSum = xins + yins + zins + wins;

        var hash =
            (int)(zins - wins + 1) |
            (int)(yins - zins + 1) << 1 |
            (int)(yins - wins + 1) << 2 |
            (int)(xins - yins + 1) << 3 |
            (int)(xins - zins + 1) << 4 |
            (int)(xins - wins + 1) << 5 |
            (int)inSum << 6 |
            (int)(inSum + wins) << 8 |
            (int)(inSum + zins) << 11 |
            (int)(inSum + yins) << 14 |
            (int)(inSum + xins) << 17;

        if (!TryGetChain(
                hash,
                LookupLength4D,
                LookupPairs4D,
                ChainMetadata4D,
                out int recordStart,
                out int recordCount))
        {
            return 0.0;
        }

        ReadOnlySpan<long> contributions = Contributions4D;
        ReadOnlySpan<double> gradients = Gradients4D;
        var value = 0.0;

        for (int record = 0; record < recordCount; record++)
        {
            int offset = (recordStart + record) * 8;
            int xsv = (int)contributions[offset];
            int ysv = (int)contributions[offset + 1];
            int zsv = (int)contributions[offset + 2];
            int wsv = (int)contributions[offset + 3];
            var dx = dx0 + BitConverter.Int64BitsToDouble(contributions[offset + 4]);
            var dy = dy0 + BitConverter.Int64BitsToDouble(contributions[offset + 5]);
            var dz = dz0 + BitConverter.Int64BitsToDouble(contributions[offset + 6]);
            var dw = dw0 + BitConverter.Int64BitsToDouble(contributions[offset + 7]);
            var attn = 2 - dx * dx - dy * dy - dz * dz - dw * dw;

            if (attn > 0)
            {
                var px = xsb + xsv;
                var py = ysb + ysv;
                var pz = zsb + zsv;
                var pw = wsb + wsv;
                var gradientIndex = permutation4D[
                    permutation[
                        permutation[permutation[px & 0xFF] + py & 0xFF] + pz & 0xFF] +
                    pw & 0xFF];
                var valuePart =
                    gradients[gradientIndex] * dx +
                    gradients[gradientIndex + 1] * dy +
                    gradients[gradientIndex + 2] * dz +
                    gradients[gradientIndex + 3] * dw;

                attn *= attn;
                value += attn * attn * valuePart;
            }
        }

        return value * NORM_4D;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FastFloor(double value)
    {
        var integer = (int)value;
        return value < integer ? integer - 1 : integer;
    }

    private static bool TryGetChain(
        int hash,
        int lookupLength,
        ReadOnlySpan<int> lookupPairs,
        ReadOnlySpan<int> chainMetadata,
        out int recordStart,
        out int recordCount)
    {
        if ((uint)hash >= (uint)lookupLength)
        {
            throw new IndexOutOfRangeException();
        }

        int low = 0;
        int high = lookupPairs.Length / 2 - 1;

        while (low <= high)
        {
            int middle = low + ((high - low) >> 1);
            int pairOffset = middle * 2;
            int candidateHash = lookupPairs[pairOffset];

            if (candidateHash < hash)
            {
                low = middle + 1;
                continue;
            }

            if (candidateHash > hash)
            {
                high = middle - 1;
                continue;
            }

            int chainOffset = lookupPairs[pairOffset + 1] * 2;
            recordStart = chainMetadata[chainOffset];
            recordCount = chainMetadata[chainOffset + 1];
            return true;
        }

        recordStart = 0;
        recordCount = 0;
        return false;
    }

    private static void ValidateInitializationBuffers(
        Span<byte> permutation,
        Span<byte> permutation2D,
        Span<byte> permutation3D,
        Span<byte> permutation4D,
        Span<byte> sourceScratch)
    {
        ValidateBuffer(permutation, PermutationTableLength, nameof(permutation));
        ValidateBuffer(permutation2D, PermutationTableLength, nameof(permutation2D));
        ValidateBuffer(permutation3D, PermutationTableLength, nameof(permutation3D));
        ValidateBuffer(permutation4D, PermutationTableLength, nameof(permutation4D));
        ValidateBuffer(sourceScratch, SourceScratchLength, nameof(sourceScratch));

        permutation = permutation[..PermutationTableLength];
        permutation2D = permutation2D[..PermutationTableLength];
        permutation3D = permutation3D[..PermutationTableLength];
        permutation4D = permutation4D[..PermutationTableLength];
        sourceScratch = sourceScratch[..SourceScratchLength];

        if (permutation.Overlaps(permutation2D) ||
            permutation.Overlaps(permutation3D) ||
            permutation.Overlaps(permutation4D) ||
            permutation.Overlaps(sourceScratch) ||
            permutation2D.Overlaps(permutation3D) ||
            permutation2D.Overlaps(permutation4D) ||
            permutation2D.Overlaps(sourceScratch) ||
            permutation3D.Overlaps(permutation4D) ||
            permutation3D.Overlaps(sourceScratch) ||
            permutation4D.Overlaps(sourceScratch))
        {
            throw new ArgumentException("The initialization buffers must not overlap.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateEvaluationTables(
        ReadOnlySpan<byte> permutation,
        ReadOnlySpan<byte> dimensionPermutation)
    {
        ValidateBuffer(permutation, PermutationTableLength, nameof(permutation));
        ValidateBuffer(dimensionPermutation, PermutationTableLength, nameof(dimensionPermutation));
    }

    private static void ValidateBuffer(ReadOnlySpan<byte> buffer, int minimumLength, string parameterName)
    {
        if (buffer.Length < minimumLength)
        {
            throw new ArgumentException(
                $"The buffer must contain at least {minimumLength} bytes.",
                parameterName);
        }
    }
}
