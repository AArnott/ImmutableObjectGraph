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

// Type hierachy using abstract class in-between and a required field
[ImmutableObjectGraph.Generation.GenerateImmutable]
abstract partial class Pear : Fruit
{
    [ImmutableObjectGraph.Generation.Required]
    readonly string color;
}

[ImmutableObjectGraph.Generation.GenerateImmutable]
partial class SomePear : Pear
{
    readonly string someField;
}