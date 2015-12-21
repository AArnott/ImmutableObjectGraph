namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class MSBuildTests
    {
        [Fact]
        public void IdentityUniqueAcrossTypesInFamily_DefaultInstance()
        {
            var pige = ProjectItemGroupElement.Create();
            var ppge = ProjectPropertyGroupElement.Create();
            Assert.Equal(pige.Identity + 1, ppge.Identity);
        }

        [Fact]
        public void IdentityUniqueAcrossTypesInFamily_NonDefaultInstance()
        {
            var pige = ProjectItemGroupElement.Create(label: "mylabel");
            var ppge = ProjectPropertyGroupElement.Create(label: "myotherlabel");
            Assert.Equal(pige.Identity + 1, ppge.Identity);
        }

        [Fact]
        public void CreateLabel()
        {
            var pige = ProjectItemGroupElement.Create(label: "mylabel");
            Assert.Equal("mylabel", pige.Label);
        }

        [Fact]
        public void TypeExistanceTest()
        {
#pragma warning disable CS0168
            Microsoft.Build.Construction.ProjectRootElement pre;
            ProjectRootElement ipre;

            Microsoft.Build.Construction.ProjectElement pe;
            ProjectElement ipe;

            Microsoft.Build.Construction.ProjectElementContainer pec;
            ProjectElementContainer ipec;

            Microsoft.Build.Construction.ProjectPropertyGroupElement ppge;
            ProjectPropertyGroupElement ippge;

            Microsoft.Build.Construction.ProjectPropertyElement ppe;
            ProjectPropertyElement ippe;

            Microsoft.Build.Construction.ProjectMetadataElement pme;
            ProjectMetadataElement ipme;

            Microsoft.Build.Construction.ProjectItemGroupElement pige;
            ProjectItemGroupElement ipige;

            Microsoft.Build.Construction.ProjectItemElement pie;
            ProjectItemElement ipie;

            Microsoft.Build.Construction.ProjectChooseElement pce;
            ProjectChooseElement ipce;

            Microsoft.Build.Construction.ProjectWhenElement pwe;
            ProjectWhenElement ipwe;

            Microsoft.Build.Construction.ProjectOtherwiseElement poe;
            ProjectOtherwiseElement ipoe;

            Microsoft.Build.Construction.ProjectExtensionsElement pee;
            ProjectExtensionsElement ipee;

            Microsoft.Build.Construction.ProjectImportElement pimporte;
            ProjectImportElement ipimporte;

            Microsoft.Build.Construction.ProjectImportGroupElement pimportge;
            ProjectImportGroupElement ipimportge;

            Microsoft.Build.Construction.ProjectItemDefinitionElement pide;
            ProjectItemDefinitionElement ipide;

            Microsoft.Build.Construction.ProjectItemDefinitionGroupElement pidge;
            ProjectItemDefinitionGroupElement ipidge;

            Microsoft.Build.Construction.ProjectOnErrorElement poee;
            ProjectOnErrorElement ipoee;

            Microsoft.Build.Construction.ProjectOutputElement poutpute;
            ProjectOutputElement ipoutpute;

            Microsoft.Build.Construction.ProjectTargetElement pte;
            ProjectTargetElement ipte;

            Microsoft.Build.Construction.ProjectTaskElement ptaske;
            ProjectTaskElement iptaske;

            Microsoft.Build.Construction.ProjectUsingTaskBodyElement putbe;
            ProjectUsingTaskBodyElement iputbe;

            Microsoft.Build.Construction.ProjectUsingTaskElement pute;
            ProjectUsingTaskElement ipute;

            Microsoft.Build.Construction.ProjectUsingTaskParameterElement putpe;
            ProjectUsingTaskParameterElement iputpe;

            Microsoft.Build.Construction.UsingTaskParameterGroupElement utpge;
            UsingTaskParameterGroupElement iutpge;
#pragma warning restore CS0168
        }

        [Fact]
        public void BasicProjectStructure()
        {
            var pre = CreateBasicProjectStructure();
            Assert.Equal(3, pre.Children.Count);
            Assert.Equal(2, pre.Children.OfType<ProjectItemGroupElement>().Single().Children.Count);
        }

        [Fact]
        public void RecursiveMutation()
        {
            RootedProjectRootElement root = CreateBasicProjectStructure().AsRoot;
            RootedProjectItemElement aCsItem = root.Children[1].AsProjectElementContainer.Children[0].AsProjectItemElement;
            RootedProjectItemElement aCsItemUpdated = aCsItem.With(include: "A.cs");
            Assert.Equal("A.cs", aCsItemUpdated.Include);

            RootedProjectRootElement rootUpdated = aCsItemUpdated.Root.AsProjectRootElement;
            Assert.NotSame(root.ProjectRootElement, rootUpdated.ProjectRootElement);
            Assert.Same(
                aCsItemUpdated.ProjectItemElement,
                rootUpdated.ProjectRootElement.Find(aCsItemUpdated.Identity));
        }

        [Fact]
        public void ChangesSinceToolsetAndMetadata()
        {
            var root = CreateBasicProjectStructure().AsRoot;
            var newRoot = root.With(toolsVersion: "14.0");
            var aCs = newRoot.Children[1].AsProjectItemGroupElement.Children[0].AsProjectItemElement;
            var newMetadata = ProjectMetadataElement.Create(name: "ExcludeFromStyleCop", value: "true");
            newRoot = aCs.AddChild(newMetadata).Parent.Root.AsProjectRootElement;
            var changes = newRoot.ChangesSince(root);
            Assert.Equal(2, changes.Count);
            Assert.Equal(ChangeKind.Replaced, changes[0].Kind);
            Assert.Equal(ProjectElementChangedProperties.ToolsVersion, changes[0].Changes);
            Assert.Equal(ChangeKind.Added, changes[1].Kind);
            Assert.Same(newMetadata, changes[1].After);
        }

        private static ProjectRootElement CreateBasicProjectStructure()
        {
            var pre = ProjectRootElement.Create(toolsVersion: "12.0").AddChildren(
                ProjectPropertyGroupElement.Create().AddChildren(
                    ProjectPropertyElement.Create(name: "TargetFrameworkIdentifier", value: ".NETFramework"),
                    ProjectPropertyElement.Create(name: "TargetFrameworkVersion", value: "v4.5")),
                ProjectItemGroupElement.Create().AddChildren(
                    ProjectItemElement.Create(itemType: "Compile", include: "a.cs"),
                    ProjectItemElement.Create(itemType: "Compile", include: "b.cs")),
                ProjectImportElement.Create(project: "$(MSBuildExtensionsPath32)Microsoft.CSharp.targets"));
            return pre;
        }
    }

    partial class ProjectItemElement
    {
        static partial void CreateDefaultTemplate(ref ProjectItemElement.Template template)
        {
            template.Children = ImmutableList.Create<ProjectElement>();
        }
    }
}
