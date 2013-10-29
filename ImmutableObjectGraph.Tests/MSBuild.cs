namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	partial class ProjectRootElement {
		static partial void CreateDefaultTemplate(ref Template template) {
			template.Children = ImmutableList.Create<ProjectElement>();
		}
	}

	partial class ProjectPropertyGroupElement {
		static partial void CreateDefaultTemplate(ref Template template) {
			template.Children = ImmutableList.Create<ProjectElement>();
		}
	}

	partial class ProjectItemGroupElement {
		static partial void CreateDefaultTemplate(ref Template template) {
			template.Children = ImmutableList.Create<ProjectElement>();
		}
	}
}
