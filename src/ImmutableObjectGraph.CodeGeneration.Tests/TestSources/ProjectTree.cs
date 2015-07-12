namespace ImmutableObjectGraph.CodeGeneration.Tests.TestSources
{
    using System.Collections.Immutable;

    interface IRule { }

    interface IProjectPropertiesContext { }

    interface IPropertySheet { }

    class ProjectPropertiesContext : IProjectPropertiesContext
    {
    }

    [GenerateImmutable(DefineInterface = true, GenerateBuilder = true, DefineWithMethodsPerProperty = true, DefineRootedStruct = true, Delta = true)]
    partial class ProjectTree
    {
        [Required]
        readonly string caption;
        readonly string filePath;
        readonly string iconMoniker;
        readonly string expandedIconMoniker;
        readonly bool visible;
        readonly IRule browseObjectProperties;
        readonly ImmutableHashSet<string> capabilities;
        readonly ImmutableSortedSet<ProjectTree> children;
    }

    [GenerateImmutable(DefineInterface = true, GenerateBuilder = true, DefineWithMethodsPerProperty = true, DefineRootedStruct = true, Delta = true)]
    partial class ProjectItemTree : ProjectTree
    {
        [Required]
        readonly IProjectPropertiesContext projectPropertiesContext;
        readonly IPropertySheet propertySheet;
        readonly bool isLinked;
    }
}
