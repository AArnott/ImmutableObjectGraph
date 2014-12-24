[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true)]
partial class Fruit
{
    readonly int seeds;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable(GenerateBuilder = true)]
partial class Apple : Fruit
{
    readonly string color;
}