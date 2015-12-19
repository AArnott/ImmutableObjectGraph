using ImmutableObjectGraph; // this Using statement should remain
using ImmutableObjectGraph.CodeGeneration;

[GenerateImmutable]
partial class SomeClass
{
    ImmutableDeque<int> deque; // something to use the ImmutableObjectGraph namespace
}
