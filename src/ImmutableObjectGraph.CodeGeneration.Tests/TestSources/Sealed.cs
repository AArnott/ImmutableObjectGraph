namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    [GenerateImmutable(DefineInterface = true, DefineWithMethodsPerProperty = true, GenerateBuilder = true)]
    sealed partial class Sealed
    {
        readonly string name;
    }

    [GenerateImmutable(DefineInterface = true, DefineWithMethodsPerProperty = true, GenerateBuilder = true)]
    partial class Sealed_UnsealedBase
    {
        readonly string name;
    }

    [GenerateImmutable(DefineInterface = true, DefineWithMethodsPerProperty = true, GenerateBuilder = true)]
    sealed partial class Sealed_WithBase : Sealed_UnsealedBase
    {
        readonly int age;
    }
}
