[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true)]
partial class Fruit
{
    /// <summary>
    /// The number of seeds.
    /// </summary>
    readonly int seeds;
}

[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true)]
partial class Apple : Fruit
{
    readonly string color;
}