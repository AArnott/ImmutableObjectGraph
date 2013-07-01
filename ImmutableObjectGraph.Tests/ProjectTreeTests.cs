namespace ImmutableObjectGraph.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.ProjectSystem.Properties;
    using Validation;
    using Xunit;

    public class ProjectTreeTests
    {
        [Fact]
        public void ProjectTreeValueComparer()
        {
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
        public void ProjectTreeChangesSinceLargeTreeTest()
        {
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
            Assert.Equal(ProjectTreeChangedProperties.Caption, changes[0].Changes & ~ProjectTreeChangedProperties.PositionUnderParent);
            Assert.Equal(changedNodeIdentity, changes[0].Identity);
        }

        /// <summary>
        /// Constructs a tree based on a template in top down order (root to leaf),
        /// instead of the optimal leaf to roof order.
        /// </summary>
        [Fact]
        public void ConstructLargeTreeSuboptimal()
        {
            this.ConstructLargeTreeSuboptimalHelper(null, 1000);
        }

        public void ConstructLargeTreeSuboptimalHelper(int? seed = null, int maxSize = 1000)
        {
            int randSeed = seed.HasValue ? seed.Value : Environment.TickCount;
            Console.WriteLine("Random seed: {0}", randSeed);
            var random = new Random(randSeed);
            var templateTree = ConstructVeryLargeTree(random, 4, 100, maxSize);

            var root = RootedProjectTree.Create(templateTree.Caption);
            var rootWithChildren = RecursiveAddChildren(templateTree.ProjectTree, root);
        }

        private static RootedProjectTree RecursiveAddChildren(ProjectTree template, RootedProjectTree receiver)
        {
            RootedProjectTree latest = receiver;
            foreach (var templateChild in template)
            {
                var clonedTemplateChild = ProjectTree.Create(templateChild.Caption);
                var asChild = latest.AddChildren(clonedTemplateChild).Find(clonedTemplateChild.Identity);
                var childWithChildren = RecursiveAddChildren(templateChild, asChild);
                latest = childWithChildren.Parent;
            }

            return latest;
        }

        private static RootedProjectTree ConstructVeryLargeTree(Random random, int depth, int maxImmediateChildrenCount, int totalNodeCount, Func<string> counter = null)
        {
            Requires.NotNull(random, "random");
            Requires.Range(depth > 0, "maxDepth");
            Requires.Range(totalNodeCount > 0, "totalNodeCount");

            if (counter == null)
            {
                int counterPosition = 0;
                counter = () => "Node " + ++counterPosition;
            }

            var tree = RootedProjectTree.Create(counter());
            int nodesAllocated = 1;

            int maxChildrenCount = Math.Min(maxImmediateChildrenCount, totalNodeCount - nodesAllocated);
            if (depth == 1)
            {
                tree = tree.AddChildren(Enumerable.Range(1, maxChildrenCount).Select(n => ProjectTree.Create(counter())));
                nodesAllocated += maxChildrenCount;
            }
            else
            {
                int childrenCount = random.Next(maxChildrenCount) + 1;
                int sizePerBranch = (totalNodeCount - nodesAllocated) / childrenCount;
                if (sizePerBranch > 0)
                {
                    tree = tree.AddChildren(Enumerable.Range(1, childrenCount).Select(n => ConstructVeryLargeTree(random, depth - 1, maxImmediateChildrenCount, sizePerBranch, counter)));
                }
            }

            return tree;
        }
    }
}
