namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [GenerateImmutable(DefineWithMethodsPerProperty = true, DefineRootedStruct = true)]
    partial class ImmutableDictionaryHelpers
    {
        readonly ImmutableDictionary<string, int> matches;
        readonly ImmutableList<ImmutableDictionaryHelpers> children;

        static partial void CreateDefaultTemplate(ref Template template)
        {
            template.Matches = ImmutableDictionary.Create<string, int>();
        }
    }
}
