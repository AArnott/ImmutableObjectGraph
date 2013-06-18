namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Microsoft.VisualStudio.ProjectSystem.Properties;
	using Validation;
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

		[Fact]
		public void ProjectTreeChangesSinceLargeTreeTest() {
			int seed = Environment.TickCount;
			Console.WriteLine("Random seed: {0}", seed);
			var random = new Random(seed);
			var largeTree = ConstructVeryLargeTree(random, 4, 500, 10000);
			int actualSize = largeTree.ProjectTree.GetSelfAndDescendents().Count();
			Console.WriteLine("Total tree size: {0} nodes", actualSize);
			IRecursiveParent lt = largeTree.ProjectTree;
			//lt.Write(Console.Out);

			// Pick one random node to change.
			int changedNodeIdentity = random.Next(actualSize) + largeTree.Identity;
			var originalNode = largeTree.Find(changedNodeIdentity);
			var changedNode = originalNode.WithCaption("Changed!");

			// Now diff the two versions.
			var changes = largeTree.ChangesSince(changedNode.Root);
			Assert.Equal(1, changes.Count);
			Assert.Equal(ChangeKind.Replaced, changes[0].Kind);
			Assert.Equal(ProjectTreeChangedProperties.Caption, changes[0].Changes &~ ProjectTreeChangedProperties.PositionUnderParent);
			Assert.Equal(changedNodeIdentity, changes[0].Identity);
		}

		private static RootedProjectTree ConstructVeryLargeTree(Random random, int depth, int maxImmediateChildrenCount, int totalNodeCount, Func<string> counter = null) {
			Requires.NotNull(random, "random");
			Requires.Range(depth > 0, "maxDepth");
			Requires.Range(totalNodeCount > 0, "totalNodeCount");

			if (counter == null) {
				int counterPosition = 0;
				counter = () => "Node " + ++counterPosition;
			}

			var tree = RootedProjectTree.Create(counter());
			int nodesAllocated = 1;

			int maxChildrenCount = Math.Min(maxImmediateChildrenCount, totalNodeCount - nodesAllocated);
			if (depth == 1) {
				tree = tree.AddChildren(Enumerable.Range(1, maxChildrenCount).Select(n => ProjectTree.Create(counter())));
				nodesAllocated += maxChildrenCount;
			} else {
				int childrenCount = random.Next(maxChildrenCount) + 1;
				int sizePerBranch = (totalNodeCount - nodesAllocated) / childrenCount;
				if (sizePerBranch > 0) {
					tree = tree.AddChildren(Enumerable.Range(1, childrenCount).Select(n => ConstructVeryLargeTree(random, depth - 1, maxImmediateChildrenCount, sizePerBranch, counter)));
				}
			}

			return tree;
		}
	}
}
