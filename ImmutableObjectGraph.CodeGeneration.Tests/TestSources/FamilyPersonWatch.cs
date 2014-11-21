using System.Collections.Immutable;
using ImmutableObjectGraph;

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class Family
{
    ImmutableSortedSet<Person> members;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class Person
{
    [Required]
    string name;
    int age;
    Watch watch;
}

[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class Watch
{
    string color;
    int size;
}
