using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImmutableObjectGraph.CodeGeneration.Tests.TestSources
{
    partial class Nested
    {
        [GenerateImmutable( GenerateBuilder = true, DefineWithMethodsPerProperty = true )]
        public partial class NestedClass
        {
            readonly string name;
        }
    }
}
