[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class Fruit
{
    readonly int seeds;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class Apple : Fruit
{
    readonly string color;
}