using System.Reflection;

namespace Supprocom.OpenSimplexNoise.Tests;

public sealed class PublicSurfaceTests
{
    [Fact]
    public void OpenSimplexNoisePreservesConstructorsAndEvaluateOverloads()
    {
        var type = typeof(global::Supprocom.OpenSimplexNoise.OpenSimplexNoise);
        var assemblyName = type.Assembly.GetName();
        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        var instanceMethods = type.GetMethods(
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        var staticMethods = type.GetMethods(
            BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);

        Assert.True(type.IsPublic);
        Assert.Equal("Supprocom.OpenSimplexNoise.OpenSimplexNoise", type.FullName);
        Assert.Equal("Supprocom.OpenSimplexNoise", assemblyName.Name);
        Assert.Equal(new Version(0, 1, 1, 0), assemblyName.Version);
        Assert.Equal(2, constructors.Length);
        Assert.Contains(constructors, constructor => constructor.GetParameters().Length == 0);
        Assert.Contains(constructors, constructor => HasParameters(constructor, typeof(long)));
        Assert.Equal(3, instanceMethods.Length);
        Assert.Contains(instanceMethods, method => IsEvaluate(method, typeof(double), typeof(double)));
        Assert.Contains(instanceMethods, method =>
            IsEvaluate(method, typeof(double), typeof(double), typeof(double)));
        Assert.Contains(instanceMethods, method =>
            IsEvaluate(method, typeof(double), typeof(double), typeof(double), typeof(double)));
        Assert.Equal(4, staticMethods.Length);
        Assert.Contains(staticMethods, method =>
            method.Name == "Initialize" &&
            method.ReturnType == typeof(void) &&
            HasParameters(
                method,
                typeof(long),
                typeof(Span<byte>),
                typeof(Span<byte>),
                typeof(Span<byte>),
                typeof(Span<byte>),
                typeof(Span<byte>)));
        Assert.Contains(staticMethods, method =>
            IsEvaluate(
                method,
                typeof(ReadOnlySpan<byte>),
                typeof(ReadOnlySpan<byte>),
                typeof(double),
                typeof(double)));
        Assert.Contains(staticMethods, method =>
            IsEvaluate(
                method,
                typeof(ReadOnlySpan<byte>),
                typeof(ReadOnlySpan<byte>),
                typeof(double),
                typeof(double),
                typeof(double)));
        Assert.Contains(staticMethods, method =>
            IsEvaluate(
                method,
                typeof(ReadOnlySpan<byte>),
                typeof(ReadOnlySpan<byte>),
                typeof(double),
                typeof(double),
                typeof(double),
                typeof(double)));
        Assert.Equal(256, global::Supprocom.OpenSimplexNoise.OpenSimplexNoise.PermutationTableLength);
        Assert.Equal(256, global::Supprocom.OpenSimplexNoise.OpenSimplexNoise.SourceScratchLength);
    }

    private static bool HasParameters(MethodBase method, params Type[] parameterTypes)
    {
        return method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes);
    }

    private static bool IsEvaluate(MethodInfo method, params Type[] parameterTypes)
    {
        return method.Name == "Evaluate" && method.ReturnType == typeof(double) && HasParameters(method, parameterTypes);
    }
}
