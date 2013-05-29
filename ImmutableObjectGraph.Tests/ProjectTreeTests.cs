namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Microsoft.VisualStudio.ProjectSystem.Properties;
	using Xunit;

	public class ProjectTreeTests {
		[Fact]
		public void ProjectTreeValueComparer() {
			var tree = ProjectTree.Create("root");
			Assert.Equal(tree, tree, ProjectTree.Comparers.Identity);
			Assert.Equal(tree, tree, ProjectTree.Comparers.ByValue);

			var newPath = tree.WithFilePath("c:\\some\\path");
			Assert.Equal(tree, newPath, ProjectTree.Comparers.Identity);
			Assert.NotEqual(tree, newPath, ProjectTree.Comparers.ByValue);

			var changedBackToOriginal = newPath.WithFilePath(tree.FilePath);
			Assert.Equal(tree, changedBackToOriginal, ProjectTree.Comparers.Identity);
			Assert.Equal(tree, changedBackToOriginal, ProjectTree.Comparers.ByValue);

			var derivedType = tree.ToProjectItemTree(new ProjectPropertiesContext());
			Assert.Equal(tree, derivedType, ProjectTree.Comparers.Identity);
			Assert.NotEqual(tree, derivedType, ProjectTree.Comparers.ByValue);
		}
	}
}
