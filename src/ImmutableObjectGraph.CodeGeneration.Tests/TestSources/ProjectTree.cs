namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System.Collections.Immutable;

    public interface IRule { }

    public interface IProjectPropertiesContext { }

    public interface IPropertySheet { }

    public class ProjectPropertiesContext : IProjectPropertiesContext
    {
    }

    [GenerateImmutable(DefineInterface = true, GenerateBuilder = true, DefineWithMethodsPerProperty = true, DefineRootedStruct = true, Delta = true)]
    partial class ProjectTree
    {
        [Required]
        readonly string caption;
        readonly string filePath;
        readonly System.Drawing.Image icon;
        readonly System.Drawing.Image expandedIcon;
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
