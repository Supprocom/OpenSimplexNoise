// Source: https://gist.github.com/digitalshadow/134a3a02b67cecd72181

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Supprocom.OpenSimplexNoise;

/// <summary>Evaluates deterministic OpenSimplex noise in two, three, or four dimensions.</summary>
/// <remarks>
/// Constructors allocate managed state. Use <see cref="Initialize"/> and the static evaluation
/// overloads with caller-owned buffers when initialization and sampling must not allocate.
/// </remarks>
[SuppressMessage("Naming", "CA1724", Justification = "The published type and namespace are preserved for source and binary compatibility.")]
[SuppressMessage("Design", "MA0053", Justification = "The published class supports inheritance; sealing it would break existing consumers.")]
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

    /// <summary>The number of bytes required by each permutation table.</summary>
    public const int PermutationTableLength = 256;

    /// <summary>The number of scratch bytes required during initialization.</summary>
    public const int SourceScratchLength = 256;

    private readonly byte[] perm;
    private readonly byte[] perm2D;
    private readonly byte[] perm3D;
    private readonly byte[] perm4D;

    /// <summary>Creates managed permutation tables using the current local clock ticks as the seed.</summary>
    public OpenSimplexNoise()
        : this(DateTime.Now.Ticks)
    {
    }

    /// <summary>Creates managed permutation tables for a deterministic seed.</summary>
    /// <param name="seed">The seed used to initialize all four permutation tables.</param>
    public OpenSimplexNoise(long seed)
    {
        perm = new byte[PermutationTableLength];
        perm2D = new byte[PermutationTableLength];
        perm3D = new byte[PermutationTableLength];
        perm4D = new byte[PermutationTableLength];
        byte[] source = new byte[SourceScratchLength];

        Initialize(seed, perm, perm2D, perm3D, perm4D, source);
    }

    /// <summary>Initializes caller-owned permutation tables without managed allocations.</summary>
    /// <param name="seed">The deterministic seed.</param>
    /// <param name="permutation">The general permutation table.</param>
    /// <param name="permutation2D">The two-dimensional gradient permutation table.</param>
    /// <param name="permutation3D">The three-dimensional gradient permutation table.</param>
    /// <param name="permutation4D">The four-dimensional gradient permutation table.</param>
    /// <param name="sourceScratch">Scratch space used only during initialization.</param>
    /// <remarks>
    /// Each buffer must contain at least 256 bytes. Only the first 256 bytes are read or written,
    /// and these used portions must not overlap. Evaluation does not require the scratch buffer.
    /// </remarks>
    /// <exception cref="ArgumentException">A buffer is too short or the used portions overlap.</exception>
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

        seed = unchecked((seed * 6364136223846793005L) + 1442695040888963407L);
        seed = unchecked((seed * 6364136223846793005L) + 1442695040888963407L);
        seed = unchecked((seed * 6364136223846793005L) + 1442695040888963407L);

        for (int index = PermutationTableLength - 1; index >= 0; index--)
        {
            seed = unchecked((seed * 6364136223846793005L) + 1442695040888963407L);
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

    /// <summary>Evaluates two-dimensional noise using this instance's permutation tables.</summary>
    /// <param name="x">The first coordinate.</param>
    /// <param name="y">The second coordinate.</param>
    /// <returns>The deterministic noise value.</returns>
    /// <exception cref="IndexOutOfRangeException">The coordinates exceed the supported lattice range.</exception>
    public double Evaluate(double x, double y)
    {
        return Evaluate(perm, perm2D, x, y);
    }

    /// <summary>Evaluates three-dimensional noise using this instance's permutation tables.</summary>
    /// <param name="x">The first coordinate.</param>
    /// <param name="y">The second coordinate.</param>
    /// <param name="z">The third coordinate.</param>
    /// <returns>The deterministic noise value.</returns>
    /// <exception cref="IndexOutOfRangeException">The coordinates exceed the supported lattice range.</exception>
    public double Evaluate(double x, double y, double z)
    {
        return Evaluate(perm, perm3D, x, y, z);
    }

    /// <summary>Evaluates four-dimensional noise using this instance's permutation tables.</summary>
    /// <param name="x">The first coordinate.</param>
    /// <param name="y">The second coordinate.</param>
    /// <param name="z">The third coordinate.</param>
    /// <param name="w">The fourth coordinate.</param>
    /// <returns>The deterministic noise value.</returns>
    /// <exception cref="IndexOutOfRangeException">The coordinates exceed the supported lattice range.</exception>
    public double Evaluate(double x, double y, double z, double w)
    {
        return Evaluate(perm, perm4D, x, y, z, w);
    }

    /// <summary>Evaluates two-dimensional noise without managed allocations.</summary>
    /// <param name="permutation">The general table produced by <see cref="Initialize"/>.</param>
    /// <param name="permutation2D">The matching two-dimensional table from the same initialization.</param>
    /// <param name="x">The first coordinate.</param>
    /// <param name="y">The second coordinate.</param>
    /// <returns>The deterministic noise value.</returns>
    /// <remarks>Table lengths are validated; table values and dimension identity are trusted.</remarks>
    /// <exception cref="ArgumentException">Either table contains fewer than 256 bytes.</exception>
    /// <exception cref="IndexOutOfRangeException">The coordinates or table values exceed the supported range.</exception>
    public static double Evaluate(
        ReadOnlySpan<byte> permutation,
        ReadOnlySpan<byte> permutation2D,
        double x,
        double y)
    {
        ValidateEvaluationTables(permutation, permutation2D);

        double stretchOffset = (x + y) * STRETCH_2D;
        double xs = x + stretchOffset;
        double ys = y + stretchOffset;

        int xsb = FastFloor(xs);
        int ysb = FastFloor(ys);

        double squishOffset = (xsb + ysb) * SQUISH_2D;
        double dx0 = x - (xsb + squishOffset);
        double dy0 = y - (ysb + squishOffset);

        double xins = xs - xsb;
        double yins = ys - ysb;
        double inSum = xins + yins;

        int hash =
            (int)(xins - yins + 1) |
            ((int)inSum << 1) |
            ((int)(inSum + yins) << 2) |
            ((int)(inSum + xins) << 4);

        return !TryGetChain(
                hash,
                LookupLength2D,
                LookupPairs2D,
                ChainMetadata2D,
                out int recordStart,
                out int recordCount)
            ? 0.0
            : EvaluateContributions2D(permutation, permutation2D, xsb, ysb, dx0, dy0, recordStart, recordCount);
    }

    private static double EvaluateContributions2D(
        ReadOnlySpan<byte> permutation,
        ReadOnlySpan<byte> permutation2D,
        int xsb,
        int ysb,
        double dx0,
        double dy0,
        int recordStart,
        int recordCount)
    {
        ReadOnlySpan<long> contributions = Contributions2D;
        ReadOnlySpan<double> gradients = Gradients2D;
        double value = 0.0;

        for (int record = 0; record < recordCount; record++)
        {
            int offset = (recordStart + record) * 4;
            int xsv = (int)contributions[offset];
            int ysv = (int)contributions[offset + 1];
            double dx = dx0 + BitConverter.Int64BitsToDouble(contributions[offset + 2]);
            double dy = dy0 + BitConverter.Int64BitsToDouble(contributions[offset + 3]);
            double attn = 2 - (dx * dx) - (dy * dy);

            if (attn > 0)
            {
                int px = xsb + xsv;
                int py = ysb + ysv;
                byte gradientIndex = permutation2D[(permutation[px & 0xFF] + py) & 0xFF];
                double valuePart = (gradients[gradientIndex] * dx) + (gradients[gradientIndex + 1] * dy);

                attn *= attn;
                value += attn * attn * valuePart;
            }
        }

        return value * NORM_2D;
    }

    /// <summary>Evaluates three-dimensional noise without managed allocations.</summary>
    /// <param name="permutation">The general table produced by <see cref="Initialize"/>.</param>
    /// <param name="permutation3D">The matching three-dimensional table from the same initialization.</param>
    /// <param name="x">The first coordinate.</param>
    /// <param name="y">The second coordinate.</param>
    /// <param name="z">The third coordinate.</param>
    /// <returns>The deterministic noise value.</returns>
    /// <remarks>Table lengths are validated; table values and dimension identity are trusted.</remarks>
    /// <exception cref="ArgumentException">Either table contains fewer than 256 bytes.</exception>
    /// <exception cref="IndexOutOfRangeException">The coordinates or table values exceed the supported range.</exception>
    public static double Evaluate(
        ReadOnlySpan<byte> permutation,
        ReadOnlySpan<byte> permutation3D,
        double x,
        double y,
        double z)
    {
        ValidateEvaluationTables(permutation, permutation3D);

        double stretchOffset = (x + y + z) * STRETCH_3D;
        double xs = x + stretchOffset;
        double ys = y + stretchOffset;
        double zs = z + stretchOffset;

        int xsb = FastFloor(xs);
        int ysb = FastFloor(ys);
        int zsb = FastFloor(zs);

        double squishOffset = (xsb + ysb + zsb) * SQUISH_3D;
        double dx0 = x - (xsb + squishOffset);
        double dy0 = y - (ysb + squishOffset);
        double dz0 = z - (zsb + squishOffset);

        double xins = xs - xsb;
        double yins = ys - ysb;
        double zins = zs - zsb;
        double inSum = xins + yins + zins;

        int hash =
            (int)(yins - zins + 1) |
            ((int)(xins - yins + 1) << 1) |
            ((int)(xins - zins + 1) << 2) |
            ((int)inSum << 3) |
            ((int)(inSum + zins) << 5) |
            ((int)(inSum + yins) << 7) |
            ((int)(inSum + xins) << 9);

        return !TryGetChain(
                hash,
                LookupLength3D,
                LookupPairs3D,
                ChainMetadata3D,
                out int recordStart,
                out int recordCount)
            ? 0.0
            : EvaluateContributions3D(permutation, permutation3D, xsb, ysb, zsb, dx0, dy0, dz0, recordStart, recordCount);
    }

    private static double EvaluateContributions3D(
        ReadOnlySpan<byte> permutation,
        ReadOnlySpan<byte> permutation3D,
        int xsb,
        int ysb,
        int zsb,
        double dx0,
        double dy0,
        double dz0,
        int recordStart,
        int recordCount)
    {
        ReadOnlySpan<long> contributions = Contributions3D;
        ReadOnlySpan<double> gradients = Gradients3D;
        double value = 0.0;

        for (int record = 0; record < recordCount; record++)
        {
            int offset = (recordStart + record) * 6;
            int xsv = (int)contributions[offset];
            int ysv = (int)contributions[offset + 1];
            int zsv = (int)contributions[offset + 2];
            double dx = dx0 + BitConverter.Int64BitsToDouble(contributions[offset + 3]);
            double dy = dy0 + BitConverter.Int64BitsToDouble(contributions[offset + 4]);
            double dz = dz0 + BitConverter.Int64BitsToDouble(contributions[offset + 5]);
            double attn = 2 - (dx * dx) - (dy * dy) - (dz * dz);

            if (attn > 0)
            {
                int px = xsb + xsv;
                int py = ysb + ysv;
                int pz = zsb + zsv;
                byte gradientIndex = permutation3D[
                    (permutation[(permutation[px & 0xFF] + py) & 0xFF] + pz) & 0xFF];
                double valuePart =
                    (gradients[gradientIndex] * dx) +
                    (gradients[gradientIndex + 1] * dy) +
                    (gradients[gradientIndex + 2] * dz);

                attn *= attn;
                value += attn * attn * valuePart;
            }
        }

        return value * NORM_3D;
    }

    /// <summary>Evaluates four-dimensional noise without managed allocations.</summary>
    /// <param name="permutation">The general table produced by <see cref="Initialize"/>.</param>
    /// <param name="permutation4D">The matching four-dimensional table from the same initialization.</param>
    /// <param name="x">The first coordinate.</param>
    /// <param name="y">The second coordinate.</param>
    /// <param name="z">The third coordinate.</param>
    /// <param name="w">The fourth coordinate.</param>
    /// <returns>The deterministic noise value.</returns>
    /// <remarks>Table lengths are validated; table values and dimension identity are trusted.</remarks>
    /// <exception cref="ArgumentException">Either table contains fewer than 256 bytes.</exception>
    /// <exception cref="IndexOutOfRangeException">The coordinates or table values exceed the supported range.</exception>
    public static double Evaluate(
        ReadOnlySpan<byte> permutation,
        ReadOnlySpan<byte> permutation4D,
        double x,
        double y,
        double z,
        double w)
    {
        ValidateEvaluationTables(permutation, permutation4D);

        double stretchOffset = (x + y + z + w) * STRETCH_4D;
        double xs = x + stretchOffset;
        double ys = y + stretchOffset;
        double zs = z + stretchOffset;
        double ws = w + stretchOffset;

        int xsb = FastFloor(xs);
        int ysb = FastFloor(ys);
        int zsb = FastFloor(zs);
        int wsb = FastFloor(ws);

        double squishOffset = (xsb + ysb + zsb + wsb) * SQUISH_4D;
        double dx0 = x - (xsb + squishOffset);
        double dy0 = y - (ysb + squishOffset);
        double dz0 = z - (zsb + squishOffset);
        double dw0 = w - (wsb + squishOffset);

        double xins = xs - xsb;
        double yins = ys - ysb;
        double zins = zs - zsb;
        double wins = ws - wsb;
        double inSum = xins + yins + zins + wins;

        int hash =
            (int)(zins - wins + 1) |
            ((int)(yins - zins + 1) << 1) |
            ((int)(yins - wins + 1) << 2) |
            ((int)(xins - yins + 1) << 3) |
            ((int)(xins - zins + 1) << 4) |
            ((int)(xins - wins + 1) << 5) |
            ((int)inSum << 6) |
            ((int)(inSum + wins) << 8) |
            ((int)(inSum + zins) << 11) |
            ((int)(inSum + yins) << 14) |
            ((int)(inSum + xins) << 17);

        return !TryGetChain(
                hash,
                LookupLength4D,
                LookupPairs4D,
                ChainMetadata4D,
                out int recordStart,
                out int recordCount)
            ? 0.0
            : EvaluateContributions4D(permutation, permutation4D, xsb, ysb, zsb, wsb, dx0, dy0, dz0, dw0, recordStart, recordCount);
    }

    private static double EvaluateContributions4D(
        ReadOnlySpan<byte> permutation,
        ReadOnlySpan<byte> permutation4D,
        int xsb,
        int ysb,
        int zsb,
        int wsb,
        double dx0,
        double dy0,
        double dz0,
        double dw0,
        int recordStart,
        int recordCount)
    {
        ReadOnlySpan<long> contributions = Contributions4D;
        ReadOnlySpan<double> gradients = Gradients4D;
        double value = 0.0;

        for (int record = 0; record < recordCount; record++)
        {
            int offset = (recordStart + record) * 8;
            int xsv = (int)contributions[offset];
            int ysv = (int)contributions[offset + 1];
            int zsv = (int)contributions[offset + 2];
            int wsv = (int)contributions[offset + 3];
            double dx = dx0 + BitConverter.Int64BitsToDouble(contributions[offset + 4]);
            double dy = dy0 + BitConverter.Int64BitsToDouble(contributions[offset + 5]);
            double dz = dz0 + BitConverter.Int64BitsToDouble(contributions[offset + 6]);
            double dw = dw0 + BitConverter.Int64BitsToDouble(contributions[offset + 7]);
            double attn = 2 - (dx * dx) - (dy * dy) - (dz * dz) - (dw * dw);

            if (attn > 0)
            {
                int px = xsb + xsv;
                int py = ysb + ysv;
                int pz = zsb + zsv;
                int pw = wsb + wsv;
                byte gradientIndex = permutation4D[
                    (permutation[
                        (permutation[(permutation[px & 0xFF] + py) & 0xFF] + pz) & 0xFF] +
                    pw) & 0xFF];
                double valuePart =
                    (gradients[gradientIndex] * dx) +
                    (gradients[gradientIndex + 1] * dy) +
                    (gradients[gradientIndex + 2] * dz) +
                    (gradients[gradientIndex + 3] * dw);

                attn *= attn;
                value += attn * attn * valuePart;
            }
        }

        return value * NORM_4D;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FastFloor(double value)
    {
        int integer = (int)value;
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
            // Preserve the exception type produced by the published dense lookup implementation.
#pragma warning disable CA2201
            throw new IndexOutOfRangeException();
#pragma warning restore CA2201
        }

        int low = 0;
        int high = (lookupPairs.Length / 2) - 1;

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

        ValidateNoOverlap(permutation, permutation2D, nameof(permutation2D));
        ValidateNoOverlap(permutation, permutation3D, nameof(permutation3D));
        ValidateNoOverlap(permutation, permutation4D, nameof(permutation4D));
        ValidateNoOverlap(permutation, sourceScratch, nameof(sourceScratch));
        ValidateNoOverlap(permutation2D, permutation3D, nameof(permutation3D));
        ValidateNoOverlap(permutation2D, permutation4D, nameof(permutation4D));
        ValidateNoOverlap(permutation2D, sourceScratch, nameof(sourceScratch));
        ValidateNoOverlap(permutation3D, permutation4D, nameof(permutation4D));
        ValidateNoOverlap(permutation3D, sourceScratch, nameof(sourceScratch));
        ValidateNoOverlap(permutation4D, sourceScratch, nameof(sourceScratch));
    }

    private static void ValidateNoOverlap(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> other, string parameterName)
    {
        if (buffer.Overlaps(other))
        {
            throw new ArgumentException("The initialization buffers must not overlap.", parameterName);
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
                string.Create(CultureInfo.InvariantCulture, $"The buffer must contain at least {minimumLength} bytes."),
                parameterName);
        }
    }
}
