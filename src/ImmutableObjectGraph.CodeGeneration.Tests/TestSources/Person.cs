namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using ImmutableObjectGraph;

    [GenerateImmutable(DefineWithMethodsPerProperty = true, GenerateBuilder = true)]
    partial class Family
    {
        readonly ImmutableSortedSet<Person> members;

        static partial void CreateDefaultTemplate(ref Template template)
        {
            template.Members = ImmutableSortedSet.Create<Person>(new FamilyMemberComparer());
        }

        private class FamilyMemberComparer : IComparer<Person>
        {
            public int Compare(Person x, Person y)
            {
                return x.Age.CompareTo(y.Age);
            }
        }
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