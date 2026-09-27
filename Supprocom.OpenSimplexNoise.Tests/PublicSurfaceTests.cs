using System.Reflection;

namespace Supprocom.OpenSimplexNoise.Tests;

/// <summary>Checks the released type identity and public member signatures.</summary>
public sealed class PublicSurfaceTests
{
    /// <summary>Preserves the published constructors, constants, and sampling overloads.</summary>
    [Fact]
    public void OpenSimplexNoisePreservesConstructorsAndEvaluateOverloads()
    {
        Type type = typeof(global::Supprocom.OpenSimplexNoise.OpenSimplexNoise);
        AssemblyName assemblyName = type.Assembly.GetName();
        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        MethodInfo[] instanceMethods = type.GetMethods(
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        MethodInfo[] staticMethods = type.GetMethods(
            BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);

        Assert.True(type.IsPublic);
        Assert.Equal("Supprocom.OpenSimplexNoise.OpenSimplexNoise", type.FullName);
        Assert.Equal("Supprocom.OpenSimplexNoise", assemblyName.Name);
        Assert.Equal(new Version(0, 1, 2, 0), assemblyName.Version);
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
            string.Equals(method.Name, "Initialize", StringComparison.Ordinal) &&
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
        return string.Equals(method.Name, "Evaluate", StringComparison.Ordinal) && method.ReturnType == typeof(double) && HasParameters(method, parameterTypes);
    }
}
