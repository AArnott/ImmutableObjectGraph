namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [GenerateImmutable]
    partial class Generations
    {
        [Generation(2)]
        readonly int age; // this field is above earlier generation to test source level backward compatibility
        readonly string firstName;
        readonly string lastName;
    }

    [GenerateImmutable]
    partial class GenerationsDerived : Generations
    {
        [Generation(2)]
        readonly string title;
        readonly string position;
    }
}
