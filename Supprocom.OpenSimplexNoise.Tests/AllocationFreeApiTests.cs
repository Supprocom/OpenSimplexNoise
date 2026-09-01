using System.Buffers.Binary;
using System.Security.Cryptography;

using Noise = global::Supprocom.OpenSimplexNoise.OpenSimplexNoise;

namespace Supprocom.OpenSimplexNoise.Tests;

public sealed class AllocationFreeApiTests
{
    private const int StateLength = Noise.PermutationTableLength * 4 + Noise.SourceScratchLength;

    [Theory]
    [InlineData(0L, "EF7A513DA21876B001C48D30ADA49FFAE6A4847BCAE32CCAF554882DF25E0C44")]
    [InlineData(-1L, "77962DB45909B83E854A9C0D11DBF7B91BE86874E6BECEDA021B1515DAC6E8A1")]
    [InlineData(123456L, "AE99990D9640F33AF465A7DE4CE9B205748A4F860DB591538AD2A51A191D7C34")]
    [InlineData(long.MinValue, "51155226308F848B9229E6F3F390B321A725251894BA665BA156C8CFA1B55C69")]
    [InlineData(long.MaxValue, "3A64E0DB988BC3DC7D5F2F104E7BF86CED32C803FDA950B9D3700DD58DE41ECF")]
    public void InitializationPreservesPublishedPermutationTableHash(long seed, string expectedHash)
    {
        Span<byte> state = stackalloc byte[StateLength];
        GetStateSpans(
            state,
            out Span<byte> permutation,
            out Span<byte> permutation2D,
            out Span<byte> permutation3D,
            out Span<byte> permutation4D,
            out Span<byte> sourceScratch);

        Noise.Initialize(
            seed,
            permutation,
            permutation2D,
            permutation3D,
            permutation4D,
            sourceScratch);

        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(state[..(Noise.PermutationTableLength * 4)], digest);

        Assert.Equal(expectedHash, Convert.ToHexString(digest));
    }

    [Theory]
    [InlineData(0L, 0xBFD1846CDF5D1502UL, 0x3FD76FBC8E82DBDFUL, 0x3FD64E895C741CC7UL)]
    [InlineData(-1L, 0xBFDF15BCED1C7A04UL, 0x3FD5EA896A7F0DD0UL, 0xBFA913420E2A7A75UL)]
    [InlineData(123456L, 0x3FCA7AC069022666UL, 0xBFA4BE0B73FEA650UL, 0x3FCD93D193DFD996UL)]
    [InlineData(long.MinValue, 0xBFCBF11A3932DBD6UL, 0xBF9B3117F9FDB00EUL, 0xBFDFD11F8A8E4674UL)]
    [InlineData(long.MaxValue, 0xBFD9D30F9DE5D2A6UL, 0xBFD5AF09DEB50482UL, 0x3FD1206FFF3C98A4UL)]
    public void CallerOwnedStatePreservesPublishedNegativeCoordinateBits(
        long seed,
        ulong expected2D,
        ulong expected3D,
        ulong expected4D)
    {
        Span<byte> state = stackalloc byte[StateLength];
        GetStateSpans(
            state,
            out Span<byte> permutation,
            out Span<byte> permutation2D,
            out Span<byte> permutation3D,
            out Span<byte> permutation4D,
            out Span<byte> sourceScratch);

        Noise.Initialize(
            seed,
            permutation,
            permutation2D,
            permutation3D,
            permutation4D,
            sourceScratch);

        AssertBits(expected2D, Noise.Evaluate(permutation, permutation2D, -0.125, -17.5));
        AssertBits(expected3D, Noise.Evaluate(permutation, permutation3D, -0.125, -17.5, -2048.75));
        AssertBits(
            expected4D,
            Noise.Evaluate(permutation, permutation4D, -0.125, -17.5, -2048.75, -65_535.5));
    }

    [Theory]
    [InlineData(0L, 0xBFE11CE126900D80UL, 0xBF978A6FDD460634UL, 0xBFB748BD59689B92UL)]
    [InlineData(-1L, 0xBFDC388E58CD2A90UL, 0xBFD123F79126D966UL, 0xBFD178C8D826799BUL)]
    [InlineData(123456L, 0x3FDF29303C7BEB3DUL, 0xBFD69603230E9FE9UL, 0xBFAE5F5736E81E4FUL)]
    [InlineData(long.MinValue, 0x3FC635BDFC4813BEUL, 0xBFD033F7C28DE40AUL, 0xBFDD750B9E8C6658UL)]
    [InlineData(long.MaxValue, 0x3FC93EC415C9882BUL, 0x3FD08D3597B98210UL, 0x3FCBE4781873EB0CUL)]
    public void CallerOwnedStatePreservesPublishedLargeCoordinateBits(
        long seed,
        ulong expected2D,
        ulong expected3D,
        ulong expected4D)
    {
        Span<byte> state = stackalloc byte[StateLength];
        GetStateSpans(
            state,
            out Span<byte> permutation,
            out Span<byte> permutation2D,
            out Span<byte> permutation3D,
            out Span<byte> permutation4D,
            out Span<byte> sourceScratch);

        Noise.Initialize(
            seed,
            permutation,
            permutation2D,
            permutation3D,
            permutation4D,
            sourceScratch);

        AssertBits(
            expected2D,
            Noise.Evaluate(permutation, permutation2D, 1_000_000_000.25, -999_999_999.75));
        AssertBits(
            expected3D,
            Noise.Evaluate(
                permutation,
                permutation3D,
                1_000_000_000.25,
                -999_999_999.75,
                536_870_911.5));
        AssertBits(
            expected4D,
            Noise.Evaluate(
                permutation,
                permutation4D,
                1_000_000_000.25,
                -999_999_999.75,
                536_870_911.5,
                -268_435_455.25));
    }

    [Theory]
    [InlineData(0L, 0x324027C45979C952UL, 0xB389F09AB14D5903UL)]
    [InlineData(-1L, 0xB2530E158A5B21F6UL, 0xB35E2E5A7B6BF5AFUL)]
    [InlineData(123456L, 0x328392A409F1165EUL, 0x33814FAD338B7F4DUL)]
    [InlineData(long.MinValue, 0xB23091CFF2BE8CD4UL, 0xB39041DD53673647UL)]
    [InlineData(long.MaxValue, 0xB281FE57D19AECF1UL, 0xB39706430430782AUL)]
    public void CallerOwnedStatePreservesPublishedZeroCoordinateBits(
        long seed,
        ulong expected3D,
        ulong expected4D)
    {
        Span<byte> state = stackalloc byte[StateLength];
        GetStateSpans(
            state,
            out Span<byte> permutation,
            out Span<byte> permutation2D,
            out Span<byte> permutation3D,
            out Span<byte> permutation4D,
            out Span<byte> sourceScratch);

        Noise.Initialize(
            seed,
            permutation,
            permutation2D,
            permutation3D,
            permutation4D,
            sourceScratch);

        AssertBits(0, Noise.Evaluate(permutation, permutation2D, 0.0, 0.0));
        AssertBits(expected3D, Noise.Evaluate(permutation, permutation3D, 0.0, 0.0, 0.0));
        AssertBits(expected4D, Noise.Evaluate(permutation, permutation4D, 0.0, 0.0, 0.0, 0.0));
    }

    [Fact]
    public void CallerOwnedStatePreservesPublishedCorpusHash()
    {
        const string expectedHash = "6B49B7744150328DB1638341F599493D7FCF4A2BB3AB2EDE9D2876E0BF8211CC";
        Span<byte> stateBuffer = stackalloc byte[StateLength];
        GetStateSpans(
            stateBuffer,
            out Span<byte> permutation,
            out Span<byte> permutation2D,
            out Span<byte> permutation3D,
            out Span<byte> permutation4D,
            out Span<byte> sourceScratch);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        ulong state = 0xD1B54A32D192ED03;

        for (int sample = 0; sample < 25_000; sample++)
        {
            long seed = unchecked((long)Next(ref state));
            Noise.Initialize(
                seed,
                permutation,
                permutation2D,
                permutation3D,
                permutation4D,
                sourceScratch);
            double x = Coordinate(ref state);
            double y = Coordinate(ref state);
            double z = Coordinate(ref state);
            double w = Coordinate(ref state);

            Append(hash, Noise.Evaluate(permutation, permutation2D, x, y));
            Append(hash, Noise.Evaluate(permutation, permutation3D, x, y, z));
            Append(hash, Noise.Evaluate(permutation, permutation4D, x, y, z, w));
        }

        Assert.Equal(expectedHash, Convert.ToHexString(hash.GetHashAndReset()));
    }

    [Fact]
    public void CallerOwnedStatePreservesPublishedExtremeBehavior()
    {
        var permutation = new byte[Noise.PermutationTableLength];
        var permutation2D = new byte[Noise.PermutationTableLength];
        var permutation3D = new byte[Noise.PermutationTableLength];
        var permutation4D = new byte[Noise.PermutationTableLength];
        var sourceScratch = new byte[Noise.SourceScratchLength];
        Noise.Initialize(
            123456,
            permutation,
            permutation2D,
            permutation3D,
            permutation4D,
            sourceScratch);

        Assert.Throws<IndexOutOfRangeException>(
            () => Noise.Evaluate(permutation, permutation2D, double.MaxValue, double.MinValue));
        Assert.Throws<IndexOutOfRangeException>(
            () => Noise.Evaluate(
                permutation,
                permutation3D,
                double.MaxValue,
                double.MinValue,
                double.MaxValue));
        Assert.Throws<IndexOutOfRangeException>(
            () => Noise.Evaluate(
                permutation,
                permutation4D,
                double.MaxValue,
                double.MinValue,
                double.MaxValue,
                double.MinValue));
        Assert.Throws<IndexOutOfRangeException>(
            () => Noise.Evaluate(
                permutation,
                permutation3D,
                2_147_000_000.25,
                -2_146_999_999.75,
                1_073_500_000.5));
        Assert.Throws<IndexOutOfRangeException>(
            () => Noise.Evaluate(
                permutation,
                permutation4D,
                2_147_000_000.25,
                -2_146_999_999.75,
                1_073_500_000.5,
                -536_750_000.25));
    }

    [Fact]
    public void InitializationAndEvaluationAllocateNoManagedBytes()
    {
        Assert.Null(typeof(Noise).TypeInitializer);

        Span<byte> state = stackalloc byte[StateLength];
        GetStateSpans(
            state,
            out Span<byte> permutation,
            out Span<byte> permutation2D,
            out Span<byte> permutation3D,
            out Span<byte> permutation4D,
            out Span<byte> sourceScratch);

        _ = GC.GetAllocatedBytesForCurrentThread();
        long beforeInitialization = GC.GetAllocatedBytesForCurrentThread();
        Noise.Initialize(
            123456,
            permutation,
            permutation2D,
            permutation3D,
            permutation4D,
            sourceScratch);
        long afterInitialization = GC.GetAllocatedBytesForCurrentThread();

        double total = Noise.Evaluate(permutation, permutation2D, -0.125, -17.5);
        total += Noise.Evaluate(permutation, permutation3D, -0.125, -17.5, -2048.75);
        total += Noise.Evaluate(permutation, permutation4D, -0.125, -17.5, -2048.75, -65_535.5);
        long afterEvaluation = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, afterInitialization - beforeInitialization);
        Assert.Equal(0, afterEvaluation - afterInitialization);
        Assert.NotEqual(0.0, total);
    }

    [Fact]
    public void RepeatedInitializationAndEvaluationAllocateNoManagedBytes()
    {
        Span<byte> state = stackalloc byte[StateLength];
        GetStateSpans(
            state,
            out Span<byte> permutation,
            out Span<byte> permutation2D,
            out Span<byte> permutation3D,
            out Span<byte> permutation4D,
            out Span<byte> sourceScratch);
        Noise.Initialize(
            0,
            permutation,
            permutation2D,
            permutation3D,
            permutation4D,
            sourceScratch);
        _ = Noise.Evaluate(permutation, permutation4D, 12.5, -4.25, 8.0, 0.5);

        long before = GC.GetAllocatedBytesForCurrentThread();
        double total = 0.0;

        for (int iteration = 0; iteration < 1_000; iteration++)
        {
            Noise.Initialize(
                iteration - 500,
                permutation,
                permutation2D,
                permutation3D,
                permutation4D,
                sourceScratch);
            total += Noise.Evaluate(permutation, permutation2D, 12.5, -4.25);
            total += Noise.Evaluate(permutation, permutation3D, 12.5, -4.25, 8.0);
            total += Noise.Evaluate(permutation, permutation4D, 12.5, -4.25, 8.0, 0.5);
        }

        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, after - before);
        Assert.NotEqual(0.0, total);
    }

    [Fact]
    public void CallerOwnedStatesRemainIndependent()
    {
        Span<byte> firstState = stackalloc byte[StateLength];
        Span<byte> secondState = stackalloc byte[StateLength];
        GetStateSpans(
            firstState,
            out Span<byte> firstPermutation,
            out Span<byte> firstPermutation2D,
            out Span<byte> firstPermutation3D,
            out Span<byte> firstPermutation4D,
            out Span<byte> firstScratch);
        GetStateSpans(
            secondState,
            out Span<byte> secondPermutation,
            out Span<byte> secondPermutation2D,
            out Span<byte> secondPermutation3D,
            out Span<byte> secondPermutation4D,
            out Span<byte> secondScratch);

        Noise.Initialize(
            123456,
            firstPermutation,
            firstPermutation2D,
            firstPermutation3D,
            firstPermutation4D,
            firstScratch);
        ulong expected = BitConverter.DoubleToUInt64Bits(
            Noise.Evaluate(firstPermutation, firstPermutation4D, 12.5, -4.25, 8.0, 0.5));

        Noise.Initialize(
            -987654321,
            secondPermutation,
            secondPermutation2D,
            secondPermutation3D,
            secondPermutation4D,
            secondScratch);

        Assert.False(firstPermutation.SequenceEqual(secondPermutation));
        Assert.Equal(
            expected,
            BitConverter.DoubleToUInt64Bits(
                Noise.Evaluate(firstPermutation, firstPermutation4D, 12.5, -4.25, 8.0, 0.5)));
    }

    [Fact]
    public void InitializationRejectsShortOrOverlappingBuffers()
    {
        var shortBuffer = new byte[Noise.PermutationTableLength - 1];
        var buffer = new byte[Noise.PermutationTableLength];

        Assert.Throws<ArgumentException>(
            () => Noise.Initialize(0, shortBuffer, buffer, buffer, buffer, buffer));
        Assert.Throws<ArgumentException>(
            () => Noise.Initialize(0, buffer, buffer, buffer, buffer, buffer));
        Assert.Throws<ArgumentException>(
            () => Noise.Evaluate(shortBuffer, buffer, 0.0, 0.0));
    }

    private static void GetStateSpans(
        Span<byte> state,
        out Span<byte> permutation,
        out Span<byte> permutation2D,
        out Span<byte> permutation3D,
        out Span<byte> permutation4D,
        out Span<byte> sourceScratch)
    {
        permutation = state[..Noise.PermutationTableLength];
        permutation2D = state.Slice(Noise.PermutationTableLength, Noise.PermutationTableLength);
        permutation3D = state.Slice(Noise.PermutationTableLength * 2, Noise.PermutationTableLength);
        permutation4D = state.Slice(Noise.PermutationTableLength * 3, Noise.PermutationTableLength);
        sourceScratch = state.Slice(Noise.PermutationTableLength * 4, Noise.SourceScratchLength);
    }

    private static void Append(IncrementalHash hash, double value)
    {
        Span<byte> bits = stackalloc byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(bits, BitConverter.DoubleToUInt64Bits(value));
        hash.AppendData(bits);
    }

    private static ulong Next(ref ulong state)
    {
        state = unchecked(state * 6364136223846793005UL + 1442695040888963407UL);
        return state;
    }

    private static double Coordinate(ref ulong state)
    {
        long whole = (long)(Next(ref state) % 2_000_001UL) - 1_000_000L;
        double fraction = (Next(ref state) & 0xFFFFUL) / 65_536.0;
        return whole + fraction;
    }

    private static void AssertBits(ulong expected, double actual)
    {
        Assert.Equal(expected, BitConverter.DoubleToUInt64Bits(actual));
    }
}
