namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;
    using Xunit;
    using Xunit.Abstractions;

    public class ProjectTreeTests
    {
        private readonly ITestOutputHelper logger;

        public ProjectTreeTests(ITestOutputHelper logger)
        {
            Requires.NotNull(logger, nameof(logger));
            this.logger = logger;
        }

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
            this.logger.WriteLine("Random seed: {0}", seed);
            var random = new Random(seed);
            var largeTree = ConstructVeryLargeTree(random, 4, 500, 10000);
            int actualSize = largeTree.ProjectTree.GetSelfAndDescendents().Count();
            this.logger.WriteLine("Total tree size: {0} nodes", actualSize);
            IRecursiveParent lt = largeTree.ProjectTree;
            //lt.Write(Console.Out);

            // Pick one random node to change.
            var changedNodeIdentity = ((uint)random.Next(actualSize)) + largeTree.Identity;
            var originalNode = largeTree.Find(changedNodeIdentity);
            var changedNode = originalNode.WithCaption("Changed!");

            // Now diff the two versions.
            var changes = largeTree.ChangesSince(changedNode.Root);
            Assert.Equal(1, changes.Count);
            Assert.Equal(ChangeKind.Replaced, changes[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.Caption, changes[0].Changes & ~ProjectTreeChangedProperties.PositionUnderParent);
            Assert.Equal(changedNodeIdentity, changes[0].Identity);
        }

        [Fact]
        public void ConstructLargeTreeOptimal()
        {
            this.ConstructLargeTreeOptimalHelper(null, 1000);
        }

        /// <summary>
        /// Constructs a tree based on a template in top down order (root to leaf),
        /// instead of the optimal leaf to roof order.
        /// </summary>
        [Fact]
        public void ConstructLargeTreeSuboptimalWithoutBuilder()
        {
            this.CloneProjectTreeRootToLeafWithoutBuilders(ConstructLargeTreeOptimalHelper());
        }

        /// <summary>
        /// Constructs a tree based on a template in top down order (root to leaf),
        /// instead of the optimal leaf to roof order.
        /// </summary>
        [Fact]
        public void ConstructLargeTreeSuboptimalUsingBuilder()
        {
            this.CloneProjectTreeRootToLeafWithBuilders(ConstructLargeTreeOptimalHelper());
        }

        internal RootedProjectTree ConstructLargeTreeOptimalHelper(int? seed = null, int maxSize = 1000)
        {
            int randSeed = seed.HasValue ? seed.Value : Environment.TickCount;
            this.logger.WriteLine("Random seed: {0}", randSeed);
            var random = new Random(randSeed);
            return ConstructVeryLargeTree(random, 4, 100, maxSize);
        }

        internal RootedProjectTree CloneProjectTreeLeafToRoot(RootedProjectTree templateTree)
        {
            var clone = ProjectTree.Create(templateTree.Caption).AddChildren(templateTree.ProjectTree.Children.Select(this.CloneProjectTreeLeafToRoot));
            return clone.AsRoot;
        }

        internal ProjectTree CloneProjectTreeLeafToRoot(ProjectTree templateTree)
        {
            var clone = ProjectTree.Create(templateTree.Caption).AddChildren(templateTree.Children.Select(this.CloneProjectTreeLeafToRoot));
            return clone;
        }

        internal RootedProjectTree CloneProjectTreeRootToLeafWithoutBuilders(RootedProjectTree templateTree)
        {
            var root = RootedProjectTree.Create(templateTree.Caption);
            var rootWithChildren = RecursiveAddChildren(templateTree.ProjectTree, root);
            return rootWithChildren;
        }

        internal RootedProjectTree CloneProjectTreeRootToLeafWithBuilders(RootedProjectTree templateTree)
        {
            var rootBuilder = ProjectTree.Create(templateTree.Caption).ToBuilder();
            RecursiveAddChildren(templateTree.ProjectTree, rootBuilder);
            var root = rootBuilder.ToImmutable();
            return root.AsRoot;
        }

        private static void RecursiveAddChildren(ProjectTree template, ProjectTree.Builder receiver)
        {
            foreach (var templateChild in template)
            {
                var clonedTemplateChild = ProjectTree.Create(templateChild.Caption).ToBuilder();
                RecursiveAddChildren(templateChild, clonedTemplateChild);
                receiver.Children.Add(clonedTemplateChild.ToImmutable());
            }
        }

        private static RootedProjectTree RecursiveAddChildren(ProjectTree template, RootedProjectTree receiver)
        {
            RootedProjectTree latest = receiver;
            foreach (var templateChild in template)
            {
                var clonedTemplateChild = ProjectTree.Create(templateChild.Caption);
                var asChild = latest.AddChild(clonedTemplateChild).Value;
                var childWithChildren = RecursiveAddChildren(templateChild, asChild);
                latest = childWithChildren.Parent;
            }

            return latest;
        }

        internal static RootedProjectTree ConstructVeryLargeTree(Random random, int depth, int maxImmediateChildrenCount, int totalNodeCount, Func<string> counter = null)
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
                    tree = tree.AddChildren(Enumerable.Range(1, childrenCount).Select(n => ConstructVeryLargeTree(random, depth - 1, maxImmediateChildrenCount, sizePerBranch, counter).ProjectTree));
                }
            }

            return tree;
        }
    }
}
