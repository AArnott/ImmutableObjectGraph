namespace ImmutableObjectGraph.Tests
{
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
			Microsoft.Build.Construction.ProjectElement pe;
			Microsoft.Build.Construction.ProjectElementContainer pec;
			Microsoft.Build.Construction.ProjectPropertyGroupElement ppge;
			Microsoft.Build.Construction.ProjectPropertyElement ppe;
			Microsoft.Build.Construction.ProjectMetadataElement pme;
			Microsoft.Build.Construction.ProjectItemGroupElement pige;
			Microsoft.Build.Construction.ProjectItemElement pie;

			ProjectRootElement ipre;
			ProjectElementContainer ipec;
			ProjectItemElement ipie;
			
			//ProjectPropertyGroupElement ippge;
		}
	}
}
