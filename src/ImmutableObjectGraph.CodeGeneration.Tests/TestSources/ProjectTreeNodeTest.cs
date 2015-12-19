//-----------------------------------------------------------------------
// <copyright file="UnattachedProjectTreeNodeTest.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// This is a test class for ProjectTree and is intended
    /// to contain all ProjectTree Unit Tests
    /// </summary>
    public class UnattachedProjectTreeNodeTest : ProjectTreeNodeTestBase
    {
        [Fact]
        public void AddDirectChild()
        {
            var tree = this.NewTree("Some tree");
            var newChild = this.NewTree(Caption);
            var newTree = tree.AddChildren(newChild);
            Assert.Equal(0, tree.Children.Count);
            Assert.Equal(1, newTree.Children.Count);
            Assert.Same(newChild, newTree.Children[0]);
        }

        [Fact]
        public void NewNodeUniqueIdentity()
        {
            var tree = this.NewTree("Some tree");
            var child1 = this.NewTree("some caption");
            var child2 = this.NewTree("some caption");
            Assert.NotEqual(child1.Identity, child2.Identity);
        }

        [Fact]
        public void AddDeepDescendent()
        {
            var tree = this.NewTree("Some tree");
            var child = this.NewTree("some caption");
            tree = tree.AddChildren(child);
            var newChild = this.NewTree(Caption);
            var newTree = tree.AddDescendent(newChild, child);
            Assert.Equal(1, tree.Children.Count);
            Assert.Equal(1, newTree.Children.Count);
            Assert.Equal(0, tree.Children[0].Children.Count);
            Assert.Equal(1, newTree.Children[0].Children.Count);
            Assert.Same(newChild, newTree.Children[0].Children[0]);
            Assert.Equal(tree.Children[0].Caption, newTree.Children[0].Caption);
        }

        [Fact]
        public void RemoveDirectChild()
        {
            var child = this.NewNode();
            var tree = this.NewTree("Some tree", new[] { child });
            var newTree = tree.RemoveChildren(child);
            Assert.Equal(1, tree.Children.Count);
            Assert.Equal(0, newTree.Children.Count);
        }

        [Fact]
        public void RemoveDeepDescendent()
        {
            var tree = this.NewTree("Some tree");
            var child = this.NewTree(Caption);
            var grandchild = this.NewTree(Caption);
            tree = tree.AddChildren(child).AddDescendent(grandchild, child);
            var newTree = tree.RemoveDescendent(grandchild);
            Assert.Equal(1, tree.Children.Count);
            Assert.Equal(1, newTree.Children.Count);
            Assert.Equal(1, tree.Children[0].Children.Count);
            Assert.Equal(0, newTree.Children[0].Children.Count);
        }

        [Fact]
        public void ReplaceDirectChild()
        {
            var child = this.NewNode();
            var tree = this.NewTree("Some tree", child);
            var newChild = child.WithCaption(ModifiedCaption);
            var newTree = tree.ReplaceDescendent(child, newChild);
            Assert.Equal(1, tree.Children.Count);
            Assert.Equal(1, newTree.Children.Count);
            Assert.Equal(child.Identity, newChild.Identity);
            Assert.Same(child, tree.Children[0]);
            Assert.Same(newChild, newTree.Children[0]);
        }

        [Fact]
        public void ReplaceDeepDescendent()
        {
            var tree = this.NewTree("Some tree");
            var child = this.NewTree(Caption);
            var grandchild = this.NewTree(Caption);
            tree = tree.AddChildren(child).AddDescendent(grandchild, child);
            var newGrandchild = grandchild.WithCaption(ModifiedCaption);
            var newTree = tree.ReplaceDescendent(grandchild, newGrandchild);
            Assert.Equal(1, tree.Children[0].Children.Count);
            Assert.Equal(1, newTree.Children[0].Children.Count);
            Assert.Equal(grandchild.Identity, newGrandchild.Identity);
            Assert.Same(grandchild, tree.Children[0].Children[0]);
            Assert.Same(newGrandchild, newTree.Children[0].Children[0]);
        }

        [Fact]
        public void RemoveChildWorksBasedOnIdentity()
        {
            var tree1 = this.NewTree("Some tree");
            var child = this.NewTree(Caption);
            var tree2 = tree1.AddChildren(child);
            var newChild = child.WithCaption(ModifiedCaption);
            var tree3 = tree2.ReplaceDescendent(child, newChild);
            var tree4 = tree3.RemoveChild(child);
            Assert.Equal(0, tree4.Children.Count);
        }

        [Fact]
        public void RemoveChildrenWorksBasedOnIdentity()
        {
            var tree1 = this.NewTree("Some tree");
            var child = this.NewTree(Caption);
            var tree2 = tree1.AddChildren(child);
            var newChild = child.WithCaption(ModifiedCaption);
            var tree3 = tree2.ReplaceDescendent(child, newChild);
            var tree4 = tree3.RemoveChildren(new[] { child });
            Assert.Equal(0, tree4.Children.Count);
        }

        [Fact]
        public void RemoveDescendentWorksBasedOnIdentity()
        {
            var tree1 = this.NewTree("Some tree");
            var child = this.NewTree(Caption);
            var tree2 = tree1.AddChildren(child);
            var newChild = child.WithCaption(ModifiedCaption);
            var tree3 = tree2.ReplaceDescendent(child, newChild);
            var tree4 = tree3.RemoveDescendent(child);
            Assert.Equal(0, tree4.Children.Count);
        }

        [Fact]
        public void CreateProjectItemNode()
        {
            var tree = this.NewTree("some tree");
            IProjectPropertiesContext context = new ProjectPropertiesContext();
            var itemNode = ProjectItemTree.Create(Caption, context);
            Assert.Equal(Caption, itemNode.Caption);
            Assert.Same(context, itemNode.ProjectPropertiesContext);
        }

        [Fact]
        public void HistoryOmitsChangesAfterAdd()
        {
            var head = this.NewTree("some tree");
            var treeRevisions = new List<ProjectTree>();
            treeRevisions.Add(head);
            ProjectTree node1a, node1b;
            treeRevisions.Add(head = head.AddChildren(node1a = this.NewTree("node1a")));
            treeRevisions.Add(head = head.ReplaceDescendent(node1a, node1b = node1a.WithCapabilities("node1b")));

            var differences = ProjectTree.GetDelta((ProjectTree)treeRevisions.First(), (ProjectTree)treeRevisions.Last()).ToList();
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Added, differences[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.None, differences[0].Changes);
            Assert.Equal(differences[0].Identity, node1b.Identity); // The added node in history should be identity-equal to the most recent node.
        }

        [Fact]
        public void HistoryOmitsChangesBeforeDelete()
        {
            var head = this.NewTree("some tree");
            var treeRevisions = new List<ProjectTree>();
            treeRevisions.Add(head);
            ProjectTree node1a, node1b;
            treeRevisions.Add(head = head.AddChildren(node1a = this.NewTree("node1a")));
            treeRevisions.Add(head = head.ReplaceDescendent(node1a, node1b = node1a.WithCaption("node1b")));
            treeRevisions.Add(head = head.RemoveChildren(node1b));

            var differences = ProjectTree.GetDelta((ProjectTree)treeRevisions[1], (ProjectTree)treeRevisions.Last()).ToList();
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Removed, differences[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.None, differences[0].Changes);
            Assert.Equal(differences[0].Identity, node1b.Identity); // The added node in history should be identity-equal to the most recent node.
        }

        [Fact]
        public void HistoryOmitsAddedThenRemovedItems()
        {
            var head = this.NewTree("some tree");
            var treeRevisions = new List<ProjectTree>();
            treeRevisions.Add(head);
            ProjectTree node1a, node1b;
            treeRevisions.Add(head = head.AddChildren(node1a = this.NewTree("node1a")));
            treeRevisions.Add(head = head.ReplaceDescendent(node1a, node1b = node1a.WithCaption("node1b")));
            treeRevisions.Add(head = head.RemoveDescendent(node1b));

            var differences = ProjectTree.GetDelta((ProjectTree)treeRevisions.First(), (ProjectTree)treeRevisions.Last()).ToList();
            Assert.Equal(0, differences.Count);
        }

        [Fact]
        public void HistoryIncludesChangesToNodeProperties()
        {
            var head = this.NewTree("some tree");
            var treeRevisions = new List<ProjectTree>();
            treeRevisions.Add(head);
            ProjectTree node1a, node1b;
            treeRevisions.Add(head = head.AddChildren(node1a = this.NewTree("node1a")));
            treeRevisions.Add(head = head.ReplaceDescendent(node1a, node1b = node1a.WithCaption("node1b")));

            var differences = ProjectTree.GetDelta((ProjectTree)treeRevisions[1], (ProjectTree)treeRevisions.Last()).ToList();
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Replaced, differences[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.Caption, differences[0].Changes);
        }

        /// <summary>
        /// Verifies that adding a large sub-tree generates a history event only for the "root" of the sub-tree.
        /// </summary>
        [Fact]
        public void HistoryIncludesOnlyTopLevelAdds()
        {
            var head = this.NewTree("some tree");
            var treeRevisions = new List<ProjectTree>();
            treeRevisions.Add(head);
            ProjectTree child, grandchild;
            treeRevisions.Add(head = head.AddChildren(child = this.NewTree("child")));
            treeRevisions.Add(head = head.AddDescendent(grandchild = this.NewTree("grand-child"), child));

            // Add a sub-tree all at once to the tree.
            var greatX2grandChild1 = this.NewTree("great-great-grand-child1");
            var greatX2grandChild2 = this.NewTree("great-great-grand-child2");
            treeRevisions.Add(head = head.AddDescendent(this.NewTree("great-grand-child", new ProjectTree[] { greatX2grandChild1, greatX2grandChild2 }), grandchild));

            // Now delete one of those discretly added nodes. (the idea here is to do our best to generate a funky history)
            treeRevisions.Add(head = head.RemoveDescendent(greatX2grandChild2));

            var differences = ProjectTree.GetDelta((ProjectTree)treeRevisions.First(), (ProjectTree)treeRevisions.Last()).ToList();
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Added, differences[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.None, differences[0].Changes);
            Assert.Equal(differences[0].Identity, child.Identity); // The added node in history should be identity-equal to the most recent node.
        }

        /// <summary>
        /// Verifies that changing a grandchild node and then adding its parent node to another tree shows history for just the toplevel add.
        /// </summary>
        [Fact]
        public void HistoryIncludesOnlyTopLevelAddsWhenDescendentsChanged()
        {
            var head = this.NewTree("some tree");
            var treeRevisions = new List<ProjectTree>();
            treeRevisions.Add(head);
            ProjectTree child, grandchild;
            treeRevisions.Add(head = head.AddChildren(child = this.NewTree("child")));
            treeRevisions.Add(head = head.AddDescendent(grandchild = this.NewTree("grand-child"), child));
            grandchild = head.Find(grandchild.Identity);
            treeRevisions.Add(head = head.ReplaceDescendent(grandchild, grandchild.WithVisible(false)));

            // Verify that from beginning to very end, still only one change is reported.
            var differences = ProjectTree.GetDelta(treeRevisions[0], treeRevisions.Last()).ToList();
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Added, differences[0].Kind);
            Assert.Equal(differences[0].Identity, child.Identity); // Expected the removed node to be the child node
        }

        /// <summary>
        /// Verifies that changing a grandchild node and then removing its parent node works.
        /// </summary>
        [Fact]
        public void HistoryRemovalOfParentWithChangedChildExpectOnlyRemove()
        {
            var head = this.NewTree("some tree");
            var treeRevisions = new List<ProjectTree>();
            treeRevisions.Add(head);

            ProjectTree child, grandchild;
            treeRevisions.Add(head = head.AddChildren(child = this.NewTree("child")));
            treeRevisions.Add(head = head.AddDescendent(grandchild = this.NewTree("grand-child"), child));
            ProjectTree originalTree = head;

            grandchild = head.Find(grandchild.Identity);
            treeRevisions.Add(head = head.ReplaceDescendent(grandchild, grandchild.WithVisible(false)));

            child = head.Find(child.Identity);
            treeRevisions.Add(head = head.RemoveChildren(child));

            var differences = ProjectTree.GetDelta(originalTree, head).ToList();
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Removed, differences[0].Kind);
            Assert.Equal(differences[0].Identity, child.Identity); // Expected the removed node to be the child node
        }

        /// <summary>
        /// Verifies that changing a grandchild node and then adding its parent node, then changing the grandchild again, generates expected history.
        /// </summary>
        [Fact]
        public void HistoryAddOfParentWithChangedChildExpectsOnlyAdd()
        {
            var head = this.NewTree("some tree");
            var treeRevisions = new List<ProjectTree>();
            treeRevisions.Add(head);
            ProjectTree originalTree = head;

            ProjectTree child, grandchild;
            child = this.NewTree("child");
            child = child.AddChildren(grandchild = this.NewTree("grand-child"));
            grandchild = child.Find(grandchild.Identity);
            child = child.ReplaceDescendent(grandchild, grandchild.WithVisible(false));

            treeRevisions.Add(head = head.AddChildren(child)); // 1

            // Also change the grandchild again
            grandchild = head.Find(grandchild.Identity);
            treeRevisions.Add(head = head.ReplaceDescendent(grandchild, grandchild.WithCapabilities("sc"))); // 2

            // Verify that up to when the subtree was added, only one change is reported.
            var differences = ProjectTree.GetDelta(originalTree, treeRevisions[1]).ToList();
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Added, differences[0].Kind);
            Assert.Equal(differences[0].Identity, child.Identity); // Expected the removed node to be the child node

            // Verify that from beginning to very end, still only one change is reported.
            differences = ProjectTree.GetDelta(originalTree, head).ToList();
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Added, differences[0].Kind);
            Assert.Equal(differences[0].Identity, child.Identity); // Expected the removed node to be the child node
        }

        /// <summary>
        /// Verifies that removing a sub-tree generates a history event only for the "root" of the sub-tree.
        /// </summary>
        [Fact]
        public void HistoryIncludesOnlyTopLevelRemovals()
        {
            var head = this.NewTree("some tree");
            var treeRevisions = new List<ProjectTree>();
            treeRevisions.Add(head);
            ProjectTree child, grandchild1, grandchild2;
            treeRevisions.Add(head = head.AddChildren(child = this.NewTree("child")));
            treeRevisions.Add(head = head.AddDescendent(grandchild1 = this.NewTree("grand-child1"), child));
            treeRevisions.Add(head = head.ReplaceDescendent(grandchild1, grandchild2 = grandchild1.WithCaption("grand-child2")));

            // Add a sub-tree all at once to the tree.
            var greatX2grandChild1 = this.NewTree("great-great-grand-child1");
            var greatX2grandChild2 = this.NewTree("great-great-grand-child2");
            treeRevisions.Add(head = head.AddDescendent(this.NewTree("great-grand-child", children: new ProjectTree[] { greatX2grandChild1, greatX2grandChild2 }), grandchild1));

            // Now delete one of those discretly added nodes. (the idea here is to do our best to generate a funky history)
            treeRevisions.Add(head = head.RemoveDescendent(greatX2grandChild2));

            // And finally remove the top-level child node.
            treeRevisions.Add(head = head.RemoveDescendent(child));

            var differences = ProjectTree.GetDelta((ProjectTree)treeRevisions.First(), (ProjectTree)treeRevisions.Last()).ToList();
            Assert.Equal(0, differences.Count);

            differences = ProjectTree.GetDelta((ProjectTree)treeRevisions[3], (ProjectTree)treeRevisions.Last()).ToList();
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Removed, differences[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.None, differences[0].Changes);
            Assert.Equal(differences[0].Identity, child.Identity); // The added node in history should be identity-equal to the most recent node.
        }

        /// <summary>
        /// Verifies that properties that were changed, and then changed back, all in one history are reported as having never changed.
        /// </summary>
        [Fact]
        public void HistoryOmitsSelfCancelingPropertyChanges()
        {
            var head = this.NewTree("some tree");
            var treeRevisions = new List<ProjectTree>();
            treeRevisions.Add(head);
            ProjectTree node1a, node1b, node1c, node1d;
            treeRevisions.Add(head = head.AddChildren(node1a = this.NewTree("node1")));
            treeRevisions.Add(head = head.ReplaceDescendent(node1a, node1b = node1a.With("node1", visible: false)));
            treeRevisions.Add(head = head.ReplaceDescendent(node1b, node1c = node1b.With("node1", visible: true)));
            treeRevisions.Add(head = head.ReplaceDescendent(node1c, node1d = node1c.With("node1", visible: false).WithCapabilities(ProjectTreeCapabilities.IncludeInProjectCandidate)));

            // span the visible: true -> false change
            var differences = ProjectTree.GetDelta(treeRevisions[1], treeRevisions[2]).ToList();
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Replaced, differences[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.Visible, differences[0].Changes);

            // span the visible: true -> false -> true change
            differences = ProjectTree.GetDelta(treeRevisions[1], treeRevisions[3]).ToList();
            Assert.Equal(0, differences.Count);

            // span the visible: true -> false+capabilities change.
            differences = ProjectTree.GetDelta(treeRevisions[3], treeRevisions[4]).ToList();
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Replaced, differences[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.Capabilities | ProjectTreeChangedProperties.Visible, differences[0].Changes);

            // span the visible: false -> true -> false+capabilities change.
            differences = ProjectTree.GetDelta(treeRevisions[2], treeRevisions[4]).ToList();
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Replaced, differences[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.Capabilities, differences[0].Changes);
        }

        /// <summary>
        /// Verifies that changing a node multiple times doesn't generate a history that claims
        /// that each change was made multiple times.
        /// </summary>
        [Fact]
        public void HistoryClaimsAtMostOneChangePerPropertyPerNode()
        {
            var head = this.NewTree("some tree");
            var treeRevisions = new List<ProjectTree>();
            treeRevisions.Add(head);
            ProjectTree node1a, node1b, node1c, node1d;
            treeRevisions.Add(head = head.AddChildren(node1a = this.NewTree("node1")));
            treeRevisions.Add(head = head.ReplaceDescendent(node1a, node1b = node1a.With("node1", visible: false)));
            treeRevisions.Add(head = head.ReplaceDescendent(node1b, node1c = node1b.With("node1", visible: false).WithCapabilities(ProjectTreeCapabilities.IncludeInProjectCandidate)));
            treeRevisions.Add(head = head.ReplaceDescendent(node1c, node1d = node1c.With("node1", visible: false).WithCapabilities(ProjectTreeCapabilities.IncludeInProjectCandidate, ProjectTreeCapabilities.SourceFile)));

            var differences = ProjectTree.GetDelta(treeRevisions[1], treeRevisions.Last()).ToList();

            // The point of this test is however to verify that if a given node changes multiple times along a tree's history
            // that each changed property is only reported once, rather than once each time the node changes at all.
            Assert.Equal(1, differences.Count);
            Assert.Equal(ChangeKind.Replaced, differences[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.Visible | ProjectTreeChangedProperties.Capabilities, differences[0].Changes);
        }

        [Fact]
        public void MovingNodeAroundHierarchy()
        {
            ProjectTree aa, ab;
            var root = ProjectTree.Create("A").WithChildren(
                aa = ProjectTree.Create("AA"),
                ab = ProjectTree.Create("AB"));

            var moved = root.RemoveDescendent(aa).AddDescendent(aa, ab);

            var history = moved.ChangesSince(root);
            Assert.Equal(1, history.Count);
            Assert.Equal(ChangeKind.Replaced, history[0].Kind);
            Assert.Same(aa, history[0].Before);
            Assert.Same(aa, history[0].After);
            Assert.Equal(ProjectTreeChangedProperties.Parent, history[0].Changes);
        }

        [Fact]
        public void MovingNodeAroundHierarchyWithOtherChanges()
        {
            ProjectTree aa, ab;
            var root = ProjectTree.Create("A").WithChildren(
                aa = ProjectTree.Create("AA"),
                ab = ProjectTree.Create("AB"));

            var aaModified = aa.WithVisible(false);
            var moved = root.RemoveDescendent(aa).AddDescendent(aaModified, ab);

            var history = moved.ChangesSince(root);
            Assert.Equal(1, history.Count);
            Assert.Equal(ChangeKind.Replaced, history[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.Parent | ProjectTreeChangedProperties.Visible, history[0].Changes);
            Assert.Same(aa, history[0].Before);
            Assert.Same(aaModified, history[0].After);
            Assert.Equal(aa.Identity, history[0].Identity);
        }

        [Fact]
        public void MovingNodeAroundHierarchyWithChildAdds()
        {
            ProjectTree aa, ab;
            var root = ProjectTree.Create("A").WithChildren(
                aa = ProjectTree.Create("AA"),
                ab = ProjectTree.Create("AB"));

            var aaModified = aa.AddChild(ProjectTree.Create("AAA"));
            var moved = root.RemoveDescendent(aa).AddDescendent(aaModified, ab);

            var history = moved.ChangesSince(root);
            Assert.Equal(2, history.Count);
            Assert.Equal(ChangeKind.Replaced, history[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.Parent, history[0].Changes);
            Assert.Same(aa, history[0].Before);
            Assert.Same(aaModified, history[0].After);
            Assert.Equal(aa.Identity, history[0].Identity);

            Assert.Equal(ChangeKind.Added, history[1].Kind);
            Assert.Same(aaModified.Children[0], history[1].After);
            Assert.Null(history[1].Before);
            Assert.Equal(aaModified.Children[0].Identity, history[1].Identity);
        }

        [Fact]
        public void MovingNodeAroundHierarchyWithChildRemoves()
        {
            ProjectTree aa, ab;
            var root = ProjectTree.Create("A").WithChildren(
                aa = ProjectTree.Create("AA").WithChildren(
                    ProjectTree.Create("AAA")),
                ab = ProjectTree.Create("AB"));

            var aaModified = aa.RemoveChild(aa.Children[0]);
            var moved = root.RemoveDescendent(aa).AddDescendent(aaModified, ab);

            var history = moved.ChangesSince(root);
            Assert.Equal(2, history.Count);
            Assert.Equal(ChangeKind.Removed, history[0].Kind);
            Assert.Same(aa.Children[0], history[0].Before);
            Assert.Null(history[0].After);
            Assert.Equal(aa.Children[0].Identity, history[0].Identity);

            Assert.Equal(ChangeKind.Replaced, history[1].Kind);
            Assert.Equal(ProjectTreeChangedProperties.Parent, history[1].Changes);
            Assert.Same(aa, history[1].Before);
            Assert.Same(aaModified, history[1].After);
            Assert.Equal(aa.Identity, history[1].Identity);
        }

        [Fact]
        public void RepositioningNodeWithinParentsChildren()
        {
            ProjectTree aa, ab;
            var root = ProjectTree.Create("A").WithChildren(
                aa = ProjectTree.Create("AA"),
                ab = ProjectTree.Create("AB"));
            var ac = aa.WithCaption("AC");
            var modified = root.ReplaceDescendent(aa, ac);

            var history = modified.ChangesSince(root);
            Assert.Equal(1, history.Count);
            Assert.Equal(ChangeKind.Replaced, history[0].Kind);
            Assert.Equal(ProjectTreeChangedProperties.Caption | ProjectTreeChangedProperties.PositionUnderParent, history[0].Changes);
            Assert.Same(aa, history[0].Before);
            Assert.Same(ac, history[0].After);
        }
    }
}
