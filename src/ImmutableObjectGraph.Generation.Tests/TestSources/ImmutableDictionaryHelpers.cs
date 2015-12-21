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

        public ImmutableDictionaryHelpers AddMatch(string key, int value)
        {
            return this.With(matches: this.matches.Add(key, value));
        }

        public ImmutableDictionaryHelpers SetMatch(string key, int value)
        {
            return this.With(matches: this.matches.SetItem(key, value));
        }

        public ImmutableDictionaryHelpers RemoveMatch(string key)
        {
            return this.With(matches: this.matches.Remove(key));
        }

        static partial void CreateDefaultTemplate(ref Template template)
        {
            template.Matches = ImmutableDictionary.Create<string, int>();
        }
    }

    partial struct RootedImmutableDictionaryHelpers
    {
        public RootedImmutableDictionaryHelpers AddMatch(string key, int value)
        {
            return this.With(matches: this.Matches.Add(key, value));
        }

        public RootedImmutableDictionaryHelpers SetMatch(string key, int value)
        {
            return this.With(matches: this.Matches.SetItem(key, value));
        }

        public RootedImmutableDictionaryHelpers RemoveMatch(string key)
        {
            return this.With(matches: this.Matches.Remove(key));
        }

    }
}
