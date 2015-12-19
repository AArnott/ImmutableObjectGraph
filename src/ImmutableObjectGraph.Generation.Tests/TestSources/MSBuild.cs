namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System.Collections.Immutable;
    using System.Text;

    // Note that ProjectElementContainer-derived types lack any properties that expose
    // the specific types of children they support. And the Children member that is exposed
    // allows any type of ProjectElement.
    // So an enhancement we should consider is to optionally hide the Children member
    // from the public API and allow more constrained types of children via the public API.

    // class ElementLocation {
    // 	int column;
    // 	int line;
    // 	string file;
    // 	string locationString;
    // }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    abstract partial class ProjectElement
    {
        readonly string condition;
        // readonly ElementLocation conditionLocation;

        readonly string label;
        // readonly ElementLocation labelLocation;

        // readonly ElementLocation location;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    abstract partial class ProjectElementContainer : ProjectElement
    {
        readonly ImmutableList<ProjectElement> children;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectRootElement : ProjectElementContainer
    {
        readonly string fullPath;
        readonly Encoding encoding;

        readonly string toolsVersion;
        // readonly ElementLocation toolsVersionLocation;

        readonly string defaultTargets;
        // readonly ElementLocation defaultTargetsLocation;

        readonly string initialTargets;
        // readonly ElementLocation initialTargetsLocation;

        readonly bool treatAsLocalProperty;
        // readonly ElementLocation treatAsLocalPropertylocation;

        static partial void CreateDefaultTemplate(ref Template template)
        {
            template.Children = ImmutableList.Create<ProjectElement>();
        }
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectPropertyGroupElement : ProjectElementContainer
    {
        static partial void CreateDefaultTemplate(ref Template template)
        {
            template.Children = ImmutableList.Create<ProjectElement>();
        }
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectItemGroupElement : ProjectElementContainer
    {
        static partial void CreateDefaultTemplate(ref Template template)
        {
            template.Children = ImmutableList.Create<ProjectElement>();
        }
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectChooseElement : ProjectElementContainer
    {
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectOtherwiseElement : ProjectElementContainer
    {
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectWhenElement : ProjectElementContainer
    {
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectPropertyElement : ProjectElement
    {
        readonly string name;
        readonly string value;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectItemElement : ProjectElementContainer
    {
        readonly string exclude;
        // readonly ElementLocation ExcludeLocation;

        readonly string include;
        // readonly ElementLocation IncludeLocation;

        readonly string itemType;

        readonly string keepDuplicates;
        // readonly ElementLocation KeepDuplicatesLocation;

        readonly string keepMetadata;
        // readonly ElementLocation KeepMetadataLocation;

        readonly string remove;
        // readonly ElementLocation RemoveLocation;

        readonly string removeMetadata;
        // readonly ElementLocation RemoveMetadataLocation;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectMetadataElement : ProjectElement
    {
        readonly string name;
        readonly string value;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectExtensionsElement : ProjectElement
    {
        readonly string content;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectImportElement : ProjectElement
    {
        readonly string project;
        // readonly ElementLocation projectLocation;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectImportGroupElement : ProjectElementContainer
    {
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectItemDefinitionElement : ProjectElementContainer
    {
        readonly string itemType;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectItemDefinitionGroupElement : ProjectElementContainer
    {
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectOnErrorElement : ProjectElement
    {
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectOutputElement : ProjectElement
    {
        readonly bool isOutputItem;
        readonly bool isOutputProperty;

        readonly string itemType;
        // readonly ElementLocation itemTypeLocation;

        readonly string propertyName;
        // readonly ElementLocation propertyNameLocation;

        readonly string taskParameter;
        // readonly ElementLocation TaskParameterLocation;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectTargetElement : ProjectElementContainer
    {
        readonly string afterTargets;
        // readonly ElementLocation AfterTargetsLocation;

        readonly string beforeTargets;
        // readonly ElementLocation BeforeTargetsLocation;

        readonly string dependsOnTargets;
        // readonly ElementLocation DependsOnTargetsLocation;

        readonly string inputs;
        // readonly ElementLocation InputsLocation;

        readonly string keepDuplicateOutputs;
        // readonly ElementLocation KeepDuplicateOutputsLocation;

        readonly string name;
        // readonly ElementLocation NameLocation;

        readonly string outputs;
        // readonly ElementLocation OutputsLocation;

        readonly string returns;
        // readonly ElementLocation ReturnsLocation;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectTaskElement : ProjectElementContainer
    {
        readonly string continueOnError;
        // readonly ElementLocation ContinueOnErrorLocation;

        readonly string msbuildArchitecture;
        // readonly ElementLocation MSBuildArchitectureLocation;

        readonly string msbuildRuntime;
        // readonly ElementLocation MSBuildRuntimeLocation;

        readonly string name;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectUsingTaskBodyElement : ProjectElement
    {
        readonly string evaluate;
        // readonly ElementLocation evaluateLocation;

        readonly string taskBody;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectUsingTaskElement : ProjectElementContainer
    {
        readonly string architecture;
        // readonly ElementLocation ArchitectureLocation;

        readonly string assemblyFile;
        // readonly ElementLocation AssemblyFileLocation;

        readonly string assemblyName;
        // readonly ElementLocation AssemblyNameLocation;

        readonly string runtime;
        // readonly ElementLocation RuntimeLocation;

        readonly string taskFactory;
        // readonly ElementLocation TaskFactoryLocation;

        readonly string taskName;
        // readonly ElementLocation TaskNameLocation;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class ProjectUsingTaskParameterElement : ProjectElement
    {
        readonly string name;

        readonly string output;
        // readonly ElementLocation outputLocation;

        readonly string parameterType;
        // readonly ElementLocation parameterTypeLocation;

        readonly string required;
        // readonly ElementLocation requiredLocation;
    }

    [GenerateImmutable(DefineRootedStruct = true, Delta = true, DefineWithMethodsPerProperty = true)]
    partial class UsingTaskParameterGroupElement : ProjectElementContainer
    {
    }
}
