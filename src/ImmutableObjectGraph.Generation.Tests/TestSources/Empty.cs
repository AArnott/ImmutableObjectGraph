namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    [GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
    partial class Empty { }

    [GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
    partial class EmptyDerived : Empty
    {
    }

    [GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
    partial class NotSoEmptyDerived : Empty
    {
        readonly bool oneField;
    }

    [GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
    partial class NonEmptyBase
    {
        readonly bool oneField;
    }

    [GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
    partial class EmptyDerivedFromNonEmptyBase : NonEmptyBase
    {
    }

    [GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
    abstract partial class AbstractNonEmpty
    {
        readonly bool oneField;
    }

    [GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
    partial class EmptyDerivedFromAbstract : AbstractNonEmpty
    {
    }
}