//-----------------------------------------------------------------------
// <copyright file="UnattachedProjectTreeNodeTest2.cs" company="Microsoft">
//	 Copyright (c) Microsoft. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// This is a test class for ImmutableNodeTest and is intended
    /// to contain all ImmutableNodeTest Unit Tests
    /// </summary>
    public class UnattachedProjectTreeNodeTest2 : ProjectTreeNodeTestBase
    {
        private ProjectTree node;

        public UnattachedProjectTreeNodeTest2()
        {
            this.node = (ProjectTree)this.NewTree(Caption, children: this.Children);
        }

        protected override void Cleanup()
        {
            RecursiveTypeExtensions.LookupTable<ProjectTree, ProjectTree>.ValidateInternalIntegrity(this.node);
            base.Cleanup();
        }

        /// <summary>
        /// A test for ImmutableNode Constructor
        /// </summary>
        [Fact]
        public void ImmutableNodeConstructorTest()
        {
            this.Children = this.Children.Add(this.NewNode()); // children must be non-empty for the collection to be used in the node.
            this.node = (ProjectTree)this.NewTree(Caption, children: this.Children);

            Assert.Same(Caption, this.node.Caption);
            Assert.Same(Children, this.node.Children);
        }

        [Fact]
        public void GetSelfAndDescendentsBreadthFirst_GreenNode()
        {
            var grandChild1 = this.NewNode();
            var grandChild2 = this.NewNode();
            var child1 = this.NewNode(grandChild1);
            var child2 = this.NewNode(grandChild2);
            var parent = this.NewNode(child1, child2);

            var array = parent.GetSelfAndDescendentsBreadthFirst().ToArray();
            Assert.Same(parent, array[0]);
            Assert.Same(child1, array[1]);
            Assert.Same(child2, array[2]);
            Assert.Same(grandChild1, array[3]);
            Assert.Same(grandChild2, array[4]);
        }

        [Fact]
        public void GetSelfAndDescendentsBreadthFirst_RedNode()
        {
            var grandChild1 = this.NewNode();
            var grandChild2 = this.NewNode();
            var child1 = this.NewNode(grandChild1);
            var child2 = this.NewNode(grandChild2);
            var parent = this.NewNode(child1, child2);

            var array = parent.AsRoot.GetSelfAndDescendentsBreadthFirst().ToArray();
            Assert.Same(parent, array[0].ProjectTree);
            Assert.Same(child1, array[1].ProjectTree);
            Assert.Same(child2, array[2].ProjectTree);
            Assert.Same(grandChild1, array[3].ProjectTree);
            Assert.Same(grandChild2, array[4].ProjectTree);
        }

        [Fact]
        public void AddChild()
        {
            this.Children = this.Children.Add(this.NewNode()); // children must be non-empty for the collection to be used in the node.
            this.node = this.NewTree(Caption, children: this.Children);

            var newChild = this.NewNode();
            var newNode = this.node.AddChildren(newChild);
            Assert.Same(this.Children, this.node.Children);
            Assert.Equal(2, newNode.Children.Count);
            Assert.Same(newChild, newNode.Children[1]);
        }

        [Fact]
        public void RemoveChild()
        {
            var newChild = this.NewNode();
            var nodeWithChildren = this.node.AddChildren(newChild);
            var nodeWithoutChildren = nodeWithChildren.RemoveChildren(newChild);
            Assert.Equal(1, nodeWithChildren.Children.Count);
            Assert.Equal(0, nodeWithoutChildren.Children.Count);
        }

        [Fact]
        public void RemoveChildrenThrowsOnMissingChild()
        {
            var newChild = this.NewNode();
            Assert.Throws<ArgumentException>(() => this.node.RemoveChildren(new[] { newChild }));
        }

        [Fact]
        public void RemoveChildThrowsOnMissingChild()
        {
            var newChild = this.NewNode();
            Assert.Throws<ArgumentException>(() => this.node.RemoveChild(newChild));
        }

        [Fact]
        public void RemoveChildDeep()
        {
            var grandChild1 = this.NewNode();
            var child = this.NewNode(grandChild1);
            var parent = this.NewNode(child);

            var newParent = parent.RemoveDescendent(grandChild1);
            Assert.Equal(1, parent.Children[0].Children.Count);
            Assert.Equal(0, newParent.Children[0].Children.Count);
        }

        [Fact]
        public void ReplaceChild()
        {
            var newChild1 = this.NewNode();
            var newChild2 = this.NewNode();
            var nodeWithChildren = this.node.AddChildren(newChild1);
            var newNodeWithChildren = nodeWithChildren.ReplaceDescendent(newChild1, newChild2);
            Assert.Equal(1, nodeWithChildren.Children.Count);
            Assert.Equal(1, newNodeWithChildren.Children.Count);
            Assert.Same(newChild1, nodeWithChildren.Children[0]);
            Assert.Same(newChild2, newNodeWithChildren.Children[0]);
        }

        [Fact]
        public void ReplaceChildThrowsOnMissingChild()
        {
            var newChild1 = this.NewNode();
            var newChild2 = this.NewNode();
            Assert.Throws<ArgumentException>(() => this.node.ReplaceDescendent(newChild1, newChild2));
        }

        [Fact]
        public void ReplaceChildDeep()
        {
            var grandChild1 = this.NewNode();
            var grandChild2 = this.NewNode();
            var child = this.NewNode(grandChild1);
            var parent = this.NewNode(child);

            var newParent = parent.ReplaceDescendent(grandChild1, grandChild2);
            Assert.Same(grandChild1, parent.Children[0].Children[0]);
            Assert.Equal(1, newParent.Children[0].Children.Count);
            Assert.Same(grandChild2, newParent.Children[0].Children[0]);
        }

        [Fact]
        public void ReplaceNodeWithChildrenAndChangeIdentity()
        {
            var grandchild = this.NewTree("grandchild");
            var child = this.NewTree("child", new[] { grandchild });
            this.node = this.node.AddChildren(child);

            // Ensure we exercise our lookup table update code by filling the tree with enough nodes.
            for (int i = 0; i < RecursiveTypeExtensions.InefficiencyLoadThreshold; i++)
            {
                this.node = this.node.AddChildren(this.NewTree("child " + i));
            }

            // Verify that we can find the interesting child.
            var spine = this.node.GetSpine(child.Identity);
            Assert.Same(this.node, spine.Peek());
            Assert.Same(child, spine.Pop().Peek());

            // Now replace the child with one of a different identity.
            var newChild = this.NewTree("newChild", child.Children);
            this.node = this.node.ReplaceDescendent(child, newChild);

            spine = this.node.GetSpine(newChild.Identity);
            Assert.Same(this.node, spine.Peek());
            Assert.Same(newChild, spine.Last());

            spine = this.node.GetSpine(grandchild.Identity);
            Assert.Same(this.node, spine.Peek());
            Assert.Same(newChild, spine.Pop().Peek());
            Assert.Same(grandchild, spine.Last());
        }

        [Fact]
        public void Contains()
        {
            var leaf = (ProjectTree)this.NewTree("leaf");
            var expectedChain = new List<ProjectTree>();
            var head = leaf;
            expectedChain.Add(head);
            for (int i = 0; i < RecursiveTypeExtensions.InefficiencyLoadThreshold * 3; i++)
            {
                head = (ProjectTree)this.NewTree("step " + (i + 1), children: new[] { head });
                expectedChain.Insert(0, head);
            }

            for (int i = 0; i < expectedChain.Count - 1; i++)
            {
                Assert.True(expectedChain[i].HasDescendent(expectedChain.Last().Identity));
                Assert.False(expectedChain[i].HasDescendent(this.NewTree("missing").Identity));
            }
        }

        [Fact]
        public void Validate()
        {
            var node1 = this.NewNode();
            var node2 = this.NewNode(node1);
            RecursiveTypeExtensions.LookupTable<ProjectTree, ProjectTree>.ValidateInternalIntegrity(node1);
            RecursiveTypeExtensions.LookupTable<ProjectTree, ProjectTree>.ValidateInternalIntegrity(node2);
            Assert.Throws<RecursiveChildNotUniqueException>(() =>
            {
                var cycle = node1.AddChildren(node2);
                RecursiveTypeExtensions.LookupTable<ProjectTree, ProjectTree>.ValidateInternalIntegrity(cycle);
            });
        }

        [Fact]
        public void GetSpine()
        {
            var path = this.node.GetSpine(this.node.Identity).ToList();
            Assert.Equal(1, path.Count);
            Assert.Same(this.node, path[0]);

            ProjectTree child;
            var parent = this.node.AddChildren(child = this.NewTree("child"));
            path = parent.GetSpine(child.Identity).ToList();
            Assert.Equal(2, path.Count);
            Assert.Same(parent, path[0]);
            Assert.Same(child, path[1]);

            var leaf = this.NewTree("leaf");
            for (int steps = 0; steps < RecursiveTypeExtensions.InefficiencyLoadThreshold * 3; steps++)
            {
                var expectedChain = new List<ProjectTree>(steps + 1);
                var head = leaf;
                expectedChain.Add(head);
                for (int i = 0; i < steps; i++)
                {
                    head = (ProjectTree)this.NewTree("step " + (i + 1), head);
                    expectedChain.Insert(0, head);
                }

                // Find every single node along the chain from the head to the tail.
                for (int i = 0; i <= steps; i++)
                {
                    var actualChain = head.GetSpine(expectedChain[i].Identity).ToList();
                    Assert.Equal(expectedChain.Take(i + 1).Select(n => n.Identity).ToList(), actualChain.Select(n => n.Identity).ToList());
                }

                // Now find the tail from every node, starting at the tail to get code coverage on the inner-nodes' lazy search-building capabilities.
                for (int i = steps; i >= 0; i--)
                {
                    var actualChain = expectedChain[i].GetSpine(expectedChain.Last().Identity).ToList();
                    Assert.Equal(expectedChain.Skip(i).Select(n => n.Identity).ToList(), actualChain.Select(n => n.Identity).ToList());
                }

                // And test searches for non-related nodes.
                Assert.True(head.GetSpine(this.node.Identity).IsEmpty);
            }
        }

        [Fact]
        public void AsProjectItemTree_OnNonItem()
        {
            var root = ProjectTree.Create("hi").AsRoot;
            var item = root.AsProjectItemTree;
            Assert.True(item.IsDefault);
        }

        [Fact]
        public void FindThrowsOnMissingNode()
        {
            var child = this.NewTree("child");
            Assert.Throws<KeyNotFoundException>(() => this.node.Find(child.Identity));
        }

        [Fact]
        public void EqualsTest()
        {
            Assert.False(this.node.Equals((object)null));
            Assert.False(this.node.Equals((ProjectTree)null));
            Assert.True(this.node.Equals((object)this.node));
            Assert.True(this.node.Equals((ProjectTree)this.node));
        }

        [Fact]
        public void GetHashCodeTest()
        {
            var newNode = this.node.WithCaption("some caption");
            Assert.NotEqual(this.node.GetHashCode(), newNode.GetHashCode());
        }

        [Fact]
        public void EqualsIdentityComparerTest()
        {
            var newNode = this.node.WithCaption("some caption");
            Assert.NotEqual(this.node, newNode, EqualityComparer<ProjectTree>.Default);
            Assert.Equal(this.node, newNode, ProjectTree.Comparers.Identity);
        }

        [Fact]
        public void GetHashCodeIdentityComparerTest()
        {
            var newNode = this.node.WithCaption("some caption");
            Assert.Equal(ProjectTree.Comparers.Identity.GetHashCode(this.node), ProjectTree.Comparers.Identity.GetHashCode(newNode));
        }
    }
}
