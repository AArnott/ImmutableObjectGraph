//-----------------------------------------------------------------------
// <copyright file="ProjectTreeNodeTestBase.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A base class for testing of red and green trees.
    /// </summary>
    public abstract class ProjectTreeNodeTestBase : IDisposable
    {
        internal const string Caption = "some caption.txt";

        internal const string ModifiedCaption = "some other caption.txt";

        internal const string ItemType = "itemType";

        internal const string ItemName = "some item name.txt";

        private int nodeCounter;

        internal ImmutableSortedSet<ProjectTree> Children { get; set; }

        public ProjectTreeNodeTestBase()
        {
            this.nodeCounter = 0;
            this.Children = ImmutableSortedSet.Create(ProjectTreeSort.Default);
        }

        public void Dispose()
        {
            this.Cleanup();
        }

        protected virtual void Cleanup()
        {
        }

        internal ProjectTree NewNode(params ProjectTree[] children)
        {
            this.nodeCounter++;
            var tree = ProjectTree.Create(Caption + this.nodeCounter);
            if (children != null)
            {
                tree = tree.WithChildren(children);
            }

            return tree;
        }

        internal ProjectTree NewTree(string caption, ProjectTree singleChild)
        {
            return this.NewTree(caption, new[] { singleChild });
        }

        internal ProjectTree NewTree(string caption, IEnumerable<ProjectTree> children = null)
        {
            var tree = ProjectTree.Create(caption);
            if (children != null)
            {
                tree = tree.WithChildren(children);
            }

            return tree;
        }
    }
}
