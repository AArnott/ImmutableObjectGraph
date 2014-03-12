namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Xunit;

	public class MSBuildTests {
		[Fact]
		public void TypeExistanceTest() {
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
		}

		[Fact]
		public void BasicProjectStructure() {
			var pre = CreateBasicProjectStructure();
			Assert.Equal(3, pre.Children.Count);
			Assert.Equal(2, pre.Children.OfType<ProjectItemGroupElement>().Single().Children.Count);
		}

		[Fact]
		public void RecursiveMutation() {
			RootedProjectRootElement root = CreateBasicProjectStructure().AsRoot;
			RootedProjectItemElement aCsItem = root.Children[1].AsProjectElementContainer.Children[0].AsProjectItemElement;
			RootedProjectItemElement aCsItemUpdated = aCsItem.WithInclude("A.cs");
			Assert.Equal("A.cs", aCsItemUpdated.Include);

			RootedProjectRootElement rootUpdated = aCsItemUpdated.Root.AsProjectRootElement;
			Assert.NotSame(root.ProjectRootElement, rootUpdated.ProjectRootElement);
			Assert.Same(
				aCsItemUpdated.ProjectItemElement,
				rootUpdated.ProjectRootElement.Find(aCsItemUpdated.Identity));
		}

		private static ProjectRootElement CreateBasicProjectStructure() {
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
}
