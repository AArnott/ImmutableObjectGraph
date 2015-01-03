namespace ImmutableObjectGraph.CodeGeneration.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;
    using Xunit;

    public class FileSystemTests
    {
        private FileSystemDirectory root;

        public FileSystemTests()
        {
            this.root = FileSystemDirectory.Create("c:").AddChildren(
                FileSystemFile.Create("a.cs"),
                FileSystemFile.Create("b.cs"),
                FileSystemDirectory.Create("c").AddChildren(
                    FileSystemFile.Create("d.cs")));
        }

        [Fact]
        public void RecursiveDirectories()
        {
            var emptyRoot = FileSystemDirectory.Create("c:");
            Assert.True(emptyRoot is IEnumerable<FileSystemEntry>);
            Assert.Equal(0, emptyRoot.Count()); // using Linq exercises the enumerable

            Assert.Equal(3, this.root.Count());  // use Linq to exercise enumerator
            Assert.Equal(1, this.root.OfType<FileSystemDirectory>().Single().Count());
        }

        [Fact]
        public void NonRecursiveFiles()
        {
            Assert.False(FileSystemFile.Create("a") is System.Collections.IEnumerable);
        }

        [Fact]
        public void TypeConversion()
        {
            FileSystemFile file = FileSystemFile.Create("a");
            FileSystemDirectory folder = file.ToFileSystemDirectory();
            Assert.Equal(file.PathSegment, folder.PathSegment);
            FileSystemFile fileAgain = folder.ToFileSystemFile();
            Assert.Equal(file.PathSegment, fileAgain.PathSegment);
        }

        [Fact]
        public void ReplaceDescendentUpdatesProperty()
        {
            var leafToModify = this.root.OfType<FileSystemDirectory>().Single(c => c.PathSegment == "c").Children.Single();
            var updatedLeaf = leafToModify.WithPathSegment("e.cs");
            var updatedTree = this.root.ReplaceDescendent(leafToModify, updatedLeaf);
            Assert.Equal(this.root.PathSegment, updatedTree.PathSegment);
            var leafFromUpdatedTree = updatedTree.OfType<FileSystemDirectory>().Single(c => c.PathSegment == "c").Children.Single();
            Assert.Equal(updatedLeaf.PathSegment, leafFromUpdatedTree.PathSegment);
        }

        [Fact]
        public void ReplaceDescendentUpdatesProperty_OneArgVersion()
        {
            var leafToModify = this.root.OfType<FileSystemDirectory>().Single(c => c.PathSegment == "c").Children.Single();
            var updatedLeaf = leafToModify.WithPathSegment("e.cs");
            var updatedTree = this.root.ReplaceDescendent(updatedLeaf);
            Assert.Equal(this.root.PathSegment, updatedTree.PathSegment);
            var leafFromUpdatedTree = updatedTree.OfType<FileSystemDirectory>().Single(c => c.PathSegment == "c").Children.Single();
            Assert.Equal(updatedLeaf.PathSegment, leafFromUpdatedTree.PathSegment);
        }

        [Fact]
        public void ReplaceDescendentChangesType()
        {
            var leafToModify = this.root.OfType<FileSystemDirectory>().Single(c => c.PathSegment == "c").Children.Single();
            var updatedLeaf = leafToModify.ToFileSystemDirectory().WithPathSegment("f");
            var updatedTree = this.root.ReplaceDescendent(leafToModify, updatedLeaf);
            var leafFromUpdatedTree = updatedTree.OfType<FileSystemDirectory>().Single(c => c.PathSegment == "c").Children.Single();
            Assert.IsType<FileSystemDirectory>(leafFromUpdatedTree);
            Assert.Equal(updatedLeaf.PathSegment, leafFromUpdatedTree.PathSegment);
        }

        [Fact]
        public void ReplaceDescendentNotFound()
        {
            Assert.Throws<ArgumentException>(() => this.root.ReplaceDescendent(FileSystemFile.Create("nonexistent"), FileSystemFile.Create("replacement")));
        }

        [Fact]
        public void AddDescendentWithLookupTableFixup()
        {
            var root = this.GetRootWithLookupTable();
            FileSystemDirectory subdir = root.OfType<FileSystemDirectory>().First();
            FileSystemFile newLeaf = FileSystemFile.Create("added.txt");
            FileSystemDirectory updatedRoot = root.AddDescendent(newLeaf, subdir);
            Assert.Equal(root.Identity, updatedRoot.Identity);
            FileSystemDirectory updatedSubdir = updatedRoot.OfType<FileSystemDirectory>().First();
            Assert.True(updatedSubdir.Contains(newLeaf));
        }

        [Fact]
        public void AddDescendent()
        {
            FileSystemDirectory subdir = this.root.OfType<FileSystemDirectory>().First();
            FileSystemFile newLeaf = FileSystemFile.Create("added.txt");
            FileSystemDirectory updatedRoot = this.root.AddDescendent(newLeaf, subdir);
            Assert.Equal(this.root.Identity, updatedRoot.Identity);
            FileSystemDirectory updatedSubdir = updatedRoot.OfType<FileSystemDirectory>().First();
            Assert.True(updatedSubdir.Contains(newLeaf));
        }

        [Fact]
        public void RemoveDescendentWithLookupTableFixup()
        {
            var root = this.GetRootWithLookupTable();
            FileSystemDirectory subdir = root.OfType<FileSystemDirectory>().First(d => d.Children.OfType<FileSystemFile>().Any());
            FileSystemFile fileUnderSubdir = subdir.Children.OfType<FileSystemFile>().First();
            FileSystemDirectory updatedRoot = root.RemoveDescendent(fileUnderSubdir);
            Assert.Equal(root.Identity, updatedRoot.Identity);
            FileSystemDirectory updatedSubdir = (FileSystemDirectory)updatedRoot.Single(c => c.Identity == subdir.Identity);
            Assert.False(updatedSubdir.Contains(fileUnderSubdir));
        }

        [Fact]
        public void RemoveDescendent()
        {
            FileSystemDirectory subdir = this.root.OfType<FileSystemDirectory>().First(d => d.Children.OfType<FileSystemFile>().Any());
            FileSystemFile fileUnderSubdir = subdir.Children.OfType<FileSystemFile>().First();
            FileSystemDirectory updatedRoot = this.root.RemoveDescendent(fileUnderSubdir);
            Assert.Equal(this.root.Identity, updatedRoot.Identity);
            FileSystemDirectory updatedSubdir = (FileSystemDirectory)updatedRoot.Single(c => c.Identity == subdir.Identity);
            Assert.False(updatedSubdir.Contains(fileUnderSubdir));
        }

        [Fact(Skip = "Only works in debug")] // TODO: We need validation to be able to run in release too!
        public void ChildAddedTwiceThrowsWithMutation()
        {
            var root = FileSystemDirectory.Create("c:");
            var child = FileSystemFile.Create("a.txt");
            var mutatedChild = child.WithPathSegment("b.txt");   // same identity since we mutated an existing one.
            Assert.Throws<RecursiveChildNotUniqueException>(() => root.AddChildren(child).AddChildren(mutatedChild));
        }

        [Fact]
        public void Indexer()
        {
            Assert.Equal("a.cs", this.root["a.cs"].PathSegment);
            Assert.Equal("d.cs", ((FileSystemDirectory)this.root["c"])["d.cs"].PathSegment);
        }
        
        [Fact]
        public void EmptyPathSegment()
        {
            Assert.Throws<ArgumentNullException>(() => FileSystemDirectory.Create(null));
        }

        [Fact]
        public void HasDescendent()
        {
            FileSystemFile file;
            var root =
                FileSystemDirectory.Create(@"c:")
                    .AddChild(
                        FileSystemDirectory.Create("dir")
                            .AddChild(file = FileSystemFile.Create("file")));
            var otherFile = FileSystemFile.Create("file2");
            Assert.True(root.HasDescendent(file));
            Assert.False(root.HasDescendent(otherFile));
            Assert.False(root.HasDescendent(root));
        }

        private FileSystemDirectory GetRootWithLookupTable()
        {
            // Fill in a bunch of children to force the creation of a lookup table.
            var root = this.root.AddChildren(Enumerable.Range(100, 30).Select(n => FileSystemFile.Create("filler" + n)));
            return root;
        }
    }

    [DebuggerDisplay("{FullPath}")]
    partial class FileSystemFile
    {
        static partial void CreateDefaultTemplate(ref FileSystemFile.Template template)
        {
            template.Attributes = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    [DebuggerDisplay("{PathSegment}")]
    partial class FileSystemDirectory
    {
        public FileSystemEntry this[string pathSegment]
        {
            get
            {
                int index = this.children.IndexOf(FileSystemFile.Create(pathSegment));
                if (index < 0)
                {
                    throw new IndexOutOfRangeException();
                }

                return this.children[index];
            }
        }

        static partial void CreateDefaultTemplate(ref FileSystemDirectory.Template template)
        {
            template.Children = ImmutableSortedSet.Create<FileSystemEntry>(SiblingComparer.Instance);
        }

        partial void Validate()
        {
            Requires.NotNullOrEmpty(this.PathSegment, "PathSegment");
        }
    }

    [DebuggerDisplay("{PathSegment}")]
    partial class FileSystemEntry
    {
        public class SiblingComparer : IComparer<FileSystemEntry>
        {
            public static SiblingComparer Instance = new SiblingComparer();

            private SiblingComparer()
            {
            }

            public int Compare(FileSystemEntry x, FileSystemEntry y)
            {
                return StringComparer.OrdinalIgnoreCase.Compare(x.PathSegment, y.PathSegment);
            }
        }

        public override string ToString()
        {
            return this.PathSegment;
        }
    }
}
