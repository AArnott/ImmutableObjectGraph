[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true)]
partial class Empty { }

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true)]
partial class EmptyDerived : Empty
{
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true)]
partial class NotSoEmptyDerived : Empty
{
    readonly bool oneField;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true)]
partial class NonEmptyBase
{
    readonly bool oneField;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true)]
partial class EmptyDerivedFromNonEmptyBase : NonEmptyBase
{
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true)]
abstract partial class AbstractNonEmpty
{
    readonly bool oneField;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true, DefineInterface = true)]
partial class EmptyDerivedFromAbstract : AbstractNonEmpty
{
}
