[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true)]
partial class A
{
    readonly int field1;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true)]
partial class B : A
{
    readonly int field2;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true)]
partial class C1 : B
{
    readonly int field3;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true)]
partial class C2 : B
{
    readonly int field3;
}
