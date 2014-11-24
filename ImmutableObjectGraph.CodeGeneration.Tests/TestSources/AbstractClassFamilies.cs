[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
abstract partial class Abstract1
{
    readonly int abstract1Field1;
    readonly int abstract1Field2;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
abstract partial class Abstract2 : Abstract1
{
    readonly int abstract2Field1;
    readonly int abstract2Field2;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class ConcreteOf2Abstracts : Abstract2
{
    readonly int concreteField1;
    readonly int concreteField2;
}
