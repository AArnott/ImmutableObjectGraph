[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true)]
partial class Fruit
{
    readonly int seeds;
}

[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true)]
partial class Apple : Fruit
{
    readonly string color;
}

// Type hierachy using abstract class in-between and a required field
[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true)]
abstract partial class Pear : Fruit
{
    [ImmutableObjectGraph.Generation.Required]
    readonly string color;
}

[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true)]
partial class SomePear : Pear
{
    readonly string someField;
}