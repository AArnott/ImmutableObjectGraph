namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Xunit;

	public class MSBuildTests {
		[Fact]
		public void ProjectRootElementTest() {
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
			Microsoft.Build.Construction.ProjectImportGroupElement pimportge;
			Microsoft.Build.Construction.ProjectItemDefinitionElement pide;
			Microsoft.Build.Construction.ProjectItemDefinitionGroupElement pidge;
			Microsoft.Build.Construction.ProjectOnErrorElement poee;
			Microsoft.Build.Construction.ProjectOutputElement poutpute;
			Microsoft.Build.Construction.ProjectTargetElement pte;
			Microsoft.Build.Construction.ProjectTaskElement ptaske;
			Microsoft.Build.Construction.ProjectUsingTaskBodyElement putbe;
			Microsoft.Build.Construction.ProjectUsingTaskElement pute;
			Microsoft.Build.Construction.ProjectUsingTaskParameterElement putpe;
			Microsoft.Build.Construction.UsingTaskParameterGroupElement utpge;
		}
	}
}
