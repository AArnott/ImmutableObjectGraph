[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
partial class Empty { }

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
partial class EmptyDerived : Empty
{
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
partial class NotSoEmptyDerived : Empty
{
    readonly bool oneField;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
partial class NonEmptyBase
{
    readonly bool oneField;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
partial class EmptyDerivedFromNonEmptyBase : NonEmptyBase
{
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
abstract partial class AbstractNonEmpty
{
    readonly bool oneField;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
partial class EmptyDerivedFromAbstract : AbstractNonEmpty
{
}
