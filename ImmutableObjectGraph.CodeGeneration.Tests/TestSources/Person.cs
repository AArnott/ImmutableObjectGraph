namespace ImmutableObjectGraph.CodeGeneration.Tests.TestSources
{
    using System.Collections.Immutable;
    using ImmutableObjectGraph;

    [GenerateImmutable(DefineWithMethodsPerProperty = true, GenerateBuilder = true)]
    partial class Family
    {
        readonly ImmutableSortedSet<Person> members;
    }

    [GenerateImmutable(DefineWithMethodsPerProperty = true, GenerateBuilder = true)]
    partial class Person
    {
        [Required]
        readonly string name;
        readonly int age;
        readonly Watch watch;
    }

    [GenerateImmutable(DefineWithMethodsPerProperty = true, GenerateBuilder = true)]
    partial class Watch
    {
        readonly string color;
        readonly int size;
    }
}