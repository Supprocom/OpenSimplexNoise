namespace Supprocom.OpenSimplexNoise.Tests;

public sealed class ExactnessTests
{
    private readonly global::Supprocom.OpenSimplexNoise.OpenSimplexNoise _noise = new(123456);

    [Fact]
    public void Evaluate2DPreservesReferenceBits()
    {
        AssertBits(0x0000000000000000, _noise.Evaluate(0.0, 0.0));
        AssertBits(0x3FCA7AC069022666, _noise.Evaluate(-0.125, -17.5));
        AssertBits(0x3FDF29303C7BEB3D, _noise.Evaluate(1_000_000_000.25, -999_999_999.75));
        AssertBits(0x0000000000000000, _noise.Evaluate(double.Epsilon, -double.Epsilon));
        Assert.Throws<IndexOutOfRangeException>(() => _noise.Evaluate(double.MaxValue, double.MinValue));
    }

    [Fact]
    public void Evaluate3DPreservesReferenceBits()
    {
        AssertBits(0x328392A409F1165E, _noise.Evaluate(0.0, 0.0, 0.0));
        AssertBits(0xBFA4BE0B73FEA650, _noise.Evaluate(-0.125, -17.5, -2048.75));
        AssertBits(0xBFD69603230E9FE9, _noise.Evaluate(1_000_000_000.25, -999_999_999.75, 536_870_911.5));
        AssertBits(0x0000000000000000, _noise.Evaluate(double.Epsilon, -double.Epsilon, double.Epsilon));
        Assert.Throws<IndexOutOfRangeException>(() => _noise.Evaluate(double.MaxValue, double.MinValue, double.MaxValue));
    }

    [Fact]
    public void Evaluate4DPreservesReferenceBits()
    {
        AssertBits(0x33814FAD338B7F4D, _noise.Evaluate(0.0, 0.0, 0.0, 0.0));
        AssertBits(0x3FCD93D193DFD996, _noise.Evaluate(-0.125, -17.5, -2048.75, -65_535.5));
        AssertBits(0xBFAE5F5736E81E4F, _noise.Evaluate(1_000_000_000.25, -999_999_999.75, 536_870_911.5, -268_435_455.25));
        AssertBits(0x0000000000000000, _noise.Evaluate(double.Epsilon, -double.Epsilon, double.Epsilon, -double.Epsilon));
        Assert.Throws<IndexOutOfRangeException>(() => _noise.Evaluate(double.MaxValue, double.MinValue, double.MaxValue, double.MinValue));
    }

    private static void AssertBits(ulong expected, double actual)
    {
        Assert.Equal(expected, unchecked((ulong)BitConverter.DoubleToInt64Bits(actual)));
    }
}
