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
        var methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);

        Assert.True(type.IsPublic);
        Assert.Equal("Supprocom.OpenSimplexNoise.OpenSimplexNoise", type.FullName);
        Assert.Equal("Supprocom.OpenSimplexNoise", assemblyName.Name);
        Assert.Equal(new Version(0, 1, 0, 0), assemblyName.Version);
        Assert.Equal(2, constructors.Length);
        Assert.Contains(constructors, constructor => constructor.GetParameters().Length == 0);
        Assert.Contains(constructors, constructor => HasParameters(constructor, typeof(long)));
        Assert.Equal(3, methods.Length);
        Assert.Contains(methods, method => IsEvaluate(method, typeof(double), typeof(double)));
        Assert.Contains(methods, method => IsEvaluate(method, typeof(double), typeof(double), typeof(double)));
        Assert.Contains(methods, method => IsEvaluate(method, typeof(double), typeof(double), typeof(double), typeof(double)));
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
