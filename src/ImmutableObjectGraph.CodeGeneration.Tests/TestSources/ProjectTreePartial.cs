namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;

    [DebuggerDisplay("{Caption} ({Identity})")]
    partial class ProjectTree
    {
        public static IReadOnlyList<DiffGram> GetDelta(ProjectTree before, ProjectTree after)
        {
            return after.ChangesSince(before);
        }

        static partial void CreateDefaultTemplate(ref ProjectTree.Template template)
        {
            template.Children = ImmutableSortedSet.Create(ProjectTreeSort.Default);
            template.Capabilities = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);
            template.Visible = true;
        }

        public override string ToString()
        {
            return this.Caption;
        }
    }
}
