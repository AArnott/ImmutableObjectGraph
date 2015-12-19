[ImmutableObjectGraph.Generation.GenerateImmutable]
partial class Fruit
{
    readonly int seeds;
}

[ImmutableObjectGraph.Generation.GenerateImmutable]
partial class Apple : Fruit
{
    readonly string color;
}