namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
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

        [Fact]
        public void RedNodeStuff()
        {
            var redRoot = this.root.AsRoot;
            Assert.Equal(this.root.PathSegment, redRoot.PathSegment);
            Assert.Equal(this.root.Children.Count, redRoot.Children.Count);
            Assert.True(redRoot.Children.Any(c => c.PathSegment == "a.cs" && c.IsFileSystemFile));
            Assert.True(redRoot.Children.Any(c => c.PathSegment == "b.cs" && c.IsFileSystemFile));

            RootedFileSystemDirectory subdir = redRoot.Children.Last().AsFileSystemDirectory;
            Assert.Equal("d.cs", subdir.Children.Single().PathSegment);
        }

        [Fact]
        public void AllRedDescendentsShareRoot()
        {
            VerifyDescendentsShareRoot(this.root.AsRoot);
        }

        [Fact]
        public void RedNodeEquality()
        {
            var greenLeaf = (FileSystemDirectory)this.root.Children.Last();
            var redLeaf = greenLeaf.WithRoot(this.root);
            var redLeafAsRoot = greenLeaf.AsRoot;

            Assert.Equal(redLeafAsRoot, redLeafAsRoot);
            Assert.Equal(redLeaf, redLeaf);
            Assert.NotEqual(redLeaf, redLeafAsRoot);

            IEquatable<RootedFileSystemDirectory> redLeafAsEquatable = redLeaf;
            Assert.True(redLeafAsEquatable.Equals(redLeaf));
        }

        [Fact]
        public void GetHashCodeMatchesGreenNode()
        {
            var greenLeaf = (FileSystemDirectory)this.root.Children.Last();
            var redLeaf = greenLeaf.WithRoot(this.root);
            var redLeafAsRoot = greenLeaf.AsRoot;

            Assert.Equal(greenLeaf.GetHashCode(), redLeaf.GetHashCode());
            Assert.Equal(greenLeaf.GetHashCode(), redLeafAsRoot.GetHashCode());
        }

        [Fact]
        public void FindWithLookupTable()
        {
            var root = this.GetRootWithLookupTable();
            Assert.Same(root, root.Find(root.Identity));
            var immediateChild = root.Children.First();
            Assert.Same(immediateChild, root.Find(immediateChild.Identity));
            var grandchild = root.Children.OfType<FileSystemDirectory>().First().Children.First();
            Assert.Same(grandchild, root.Find(grandchild.Identity));
        }

        [Fact]
        public void Find()
        {
            var root = this.root;
            Assert.Same(root, root.Find(root.Identity));
            var immediateChild = root.Children.First();
            Assert.Same(immediateChild, root.Find(immediateChild.Identity));
            var grandchild = root.Children.OfType<FileSystemDirectory>().First().Children.First();
            Assert.Same(grandchild, root.Find(grandchild.Identity));
        }

        [Fact]
        public void FindFailure()
        {
            Assert.Throws<KeyNotFoundException>(() => this.root.Find(1025890195));
        }

        [Fact]
        public void RedNodeFind()
        {
            var root = this.root.AsRoot;
            Assert.Equal(root, root.Find(root.Identity).AsFileSystemDirectory);
            var immediateChild = root.Children.First();
            Assert.Equal(immediateChild, root.Find(immediateChild.Identity));
            var grandchild = root.Children.First(c => c.IsFileSystemDirectory).AsFileSystemDirectory.Children.First();
            Assert.Equal(grandchild, root.Find(grandchild.Identity));

            RootedFileSystemEntry found;
            Assert.True(root.TryFind(grandchild.Identity, out found));
            Assert.Equal(grandchild, found);
        }

        [Fact]
        public void RedNodeFindFailure()
        {
            var root = this.root.AsRoot;
            Assert.Throws<KeyNotFoundException>(() => root.Find(1082591875));

            RootedFileSystemEntry found;
            Assert.False(root.TryFind(1082591875, out found));
            Assert.Null(found.FileSystemEntry);
        }

        [Fact]
        public void DefaultRootedFileSystemEntry()
        {
            var missing = default(RootedFileSystemEntry);
            Assert.False(missing.IsFileSystemFile);
            Assert.False(missing.IsFileSystemDirectory);
            Assert.Null(missing.FileSystemEntry);
            Assert.Null(missing.Root.FileSystemDirectory);
            Assert.Null(missing.AsFileSystemFile.FileSystemFile);
            Assert.Null(missing.AsFileSystemDirectory.FileSystemDirectory);

            Assert.Throws<InvalidOperationException>(() => missing.Identity);
            Assert.Throws<InvalidOperationException>(() => missing.PathSegment);
            Assert.Throws<InvalidOperationException>(() => missing.With());
            Assert.Throws<InvalidOperationException>(() => missing.WithPathSegment("q"));

            Assert.True(missing.Equals(missing));
            missing.GetHashCode();   // we don't care what the result is, so long as it doesn't throw.
        }

        [Fact]
        public void DefaultRootedFileSystemDirectory()
        {
            var missing = default(RootedFileSystemDirectory);
            Assert.Throws<InvalidOperationException>(() => missing.WithChildren(Enumerable.Empty<FileSystemEntry>()));
            Assert.Throws<InvalidOperationException>(() => missing.AddChildren(Enumerable.Empty<FileSystemEntry>()));
            Assert.Throws<InvalidOperationException>(() => missing.RemoveChildren(Enumerable.Empty<FileSystemEntry>()));
            Assert.Throws<InvalidOperationException>(() => missing.Children);
        }

        [Fact]
        public void DefaultRootedFileSystemFile()
        {
            var missing = default(RootedFileSystemFile);
            Assert.Throws<InvalidOperationException>(() => missing.WithAttributes(Enumerable.Empty<string>()));
            Assert.Throws<InvalidOperationException>(() => missing.AddAttributes(Enumerable.Empty<string>()));
            Assert.Throws<InvalidOperationException>(() => missing.RemoveAttributes(Enumerable.Empty<string>()));
            Assert.Throws<InvalidOperationException>(() => missing.Attributes);
        }

        [Fact]
        public void ModifyPropertyInLeafRewritesSpine()
        {
            var redRoot = this.root.AsRoot;
            var leaf = redRoot.Children.Last().AsFileSystemDirectory.Children.First().AsFileSystemFile;
            var newLeaf = leaf.WithPathSegment("changed");
            var leafFromNewRoot = newLeaf.Root.Children.Last().AsFileSystemDirectory.Children.First().AsFileSystemFile;
            Assert.Equal(newLeaf, leafFromNewRoot);
        }

        [Fact]
        public void ModifyPropertyInLeafRewritesSpineWithLookupTable()
        {
            var root = this.GetRootWithLookupTable();
            var redRoot = root.AsRoot;
            var leaf = redRoot.Children.Single(l => l.IsFileSystemDirectory).AsFileSystemDirectory.Children.First().AsFileSystemFile;
            var newLeaf = leaf.WithPathSegment("changed");
            var leafFromNewRoot = newLeaf.Root.Children.Single(l => l.IsFileSystemDirectory).AsFileSystemDirectory.Children.First().AsFileSystemFile;
            Assert.Equal(newLeaf, leafFromNewRoot);
        }

        [Fact]
        public void ModifyPropertyInRootWithLookupTablePreservesLookupTable()
        {
            var root = this.GetRootWithLookupTable();
            var redRoot = root.AsRoot;
            root.Children.First().WithRoot(root);  // force lazy construction of lookup table
            var newRoot = redRoot.WithPathSegment("changed");
        }

        [Fact]
        public void WithRootInUnrelatedTreeThrows()
        {
            var leaf = FileSystemDirectory.Create("z");
            Assert.Throws<ArgumentException>(() => leaf.WithRoot(this.root));
        }

        [Fact]
        public void RedNodeWithBulkMethodOnChild()
        {
            var redRoot = this.root.AsRoot;
            var firstChild = redRoot.Children.First();
            RootedFileSystemEntry modifiedChild = firstChild.With(pathSegment: "g");
            Assert.Equal("g", modifiedChild.PathSegment);
        }

        [Fact]
        public void RedNodeWithBulkMethodOnRoot()
        {
            var redRoot = this.root.AsRoot;
            RootedFileSystemDirectory modifiedRoot = redRoot.With(pathSegment: "g");
            Assert.Equal("g", modifiedRoot.PathSegment);
        }

        [Fact]
        public void ConvertRootedFileToDirectory()
        {
            RootedFileSystemFile redFile = this.root.AsRoot.Children.First().AsFileSystemFile;
            RootedFileSystemDirectory redDirectory = redFile.ToFileSystemDirectory();
            Assert.True(redDirectory.Root.Children.Contains(redDirectory.AsFileSystemEntry));
        }

        [Fact]
        public void ConvertRootedDirectoryToFile()
        {
            RootedFileSystemDirectory redDirectory = this.root.AsRoot.Children.Last().AsFileSystemDirectory;
            var redFile = redDirectory.ToFileSystemFile();
            Assert.True(redFile.Root.Children.Contains(redFile.AsFileSystemEntry));
        }

        [Fact]
        public void ConvertFileAsEntryToFileRetainsIdentity()
        {
            RootedFileSystemEntry redEntry = this.root.AsRoot.Children.First();
            var redFile = redEntry.ToFileSystemFile();
            Assert.True(redFile.Root.Children.Contains(redFile.AsFileSystemEntry));
            Assert.Same(redEntry.FileSystemEntry, redFile.FileSystemFile);
        }

        [Fact]
        public void LookupTableIntactAfterMutatingNonRecursiveField()
        {
            var root = this.GetRootWithLookupTable();
            var modifiedRoot = root.WithPathSegment("d:");
        }

        [Fact]
        public void RedNodeRecursiveParentCreate()
        {
            var drive = RootedFileSystemDirectory.Create("c:");
            Assert.Equal("c:", drive.PathSegment);
            Assert.True(drive.IsRoot);
            Assert.Equal(drive, drive.Root);
        }

        [Fact]
        public void RedNodeNonRecursiveCollectionHelpers()
        {
            var redRoot = this.root.AsRoot;
            var redFile = redRoot.Children.First(c => c.IsFileSystemFile).AsFileSystemFile;
            var modifiedFile = redFile.AddAttributes("three", "new", "attributes");
            Assert.Equal(3, modifiedFile.Attributes.Count);
            Assert.Equal(redRoot.Identity, modifiedFile.Root.Identity);

            // Verify that the root of the new file points to the modified file.
            Assert.Equal(3, modifiedFile.Root.Find(modifiedFile.Identity).AsFileSystemFile.Attributes.Count);
        }

        [Fact]
        public void RedNodeConstructionAPI()
        {
            var redRoot = RootedFileSystemDirectory.Create("c:").AddChildren(
                FileSystemFile.Create("a.cs"),
                FileSystemFile.Create("b.cs"),
                FileSystemDirectory.Create("c")
                    .AddChildren(FileSystemFile.Create("d.cs")));
            Assert.Equal("c:", redRoot.PathSegment);
            Assert.Equal(3, redRoot.Children.Count);
            Assert.Equal("d.cs", redRoot.Children.Single(c => c.IsFileSystemDirectory).AsFileSystemDirectory.Children.Single().PathSegment);
        }

        [Fact]
        public void ParentOfRootIsDefault()
        {
            var redRoot = this.root.AsRoot;
            Assert.Null(redRoot.Parent.FileSystemDirectory);
        }

        [Fact]
        public void ParentOfChildIsCorrect()
        {
            var redRoot = this.root.AsRoot;
            foreach (var child in redRoot)
            {
                Assert.Equal(redRoot, child.Parent);
                if (child.IsFileSystemDirectory)
                {
                    var recursiveChild = child.AsFileSystemDirectory;
                    foreach (var grandchild in recursiveChild)
                    {
                        Assert.Equal(recursiveChild, grandchild.Parent);
                    }
                }
            }
        }

        [Fact]
        public void FullPath()
        {
            Assert.Equal(@"c:\", this.root.AsRoot.FullPath);
            Assert.Equal(@"c:\a.cs", this.root.AsRoot.Children.First().FullPath);
            Assert.Equal(@"c:\c\d.cs", this.root.AsRoot.Children.Last().AsFileSystemDirectory.Children.Single().FullPath);
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
        public void ChangesSinceLoneFile()
        {
            // try a case-only change to the path segment.
            var file1 = FileSystemFile.Create("a.txt");
            var file2 = file1.WithPathSegment("A.txt");
            var changes = file2.ChangesSince(file1);
            Assert.Equal(1, changes.Count);
            Assert.Equal(ChangeKind.Replaced, changes[0].Kind);
            Assert.Equal(FileSystemEntryChangedProperties.PathSegment, changes[0].Changes);

            var file3 = file2.AddAttributes("someattribute");
            changes = file3.ChangesSince(file1);
            Assert.Equal(1, changes.Count);
            Assert.Equal(ChangeKind.Replaced, changes[0].Kind);
            Assert.Equal(FileSystemEntryChangedProperties.PathSegment | FileSystemEntryChangedProperties.Attributes, changes[0].Changes);

            var file3_reverted = file2.RemoveAttributes("someattribute");
            changes = file3_reverted.ChangesSince(file1);
            Assert.Equal(1, changes.Count);
            Assert.Equal(ChangeKind.Replaced, changes[0].Kind);
            Assert.Equal(FileSystemEntryChangedProperties.PathSegment, changes[0].Changes);

            changes = file3_reverted.ChangesSince(file3);
            Assert.Equal(1, changes.Count);
            Assert.Equal(ChangeKind.Replaced, changes[0].Kind);
            Assert.Equal(FileSystemEntryChangedProperties.Attributes, changes[0].Changes);
        }

        [Fact]
        public void ChangesSinceSignificantPathSegmentChangeInChild()
        {
            var changedRoot = this.root.ReplaceDescendent(this.root["a.cs"], this.root["a.cs"].WithPathSegment("g.cs"));
            var changes = changedRoot.ChangesSince(this.root);
            Assert.Equal(1, changes.Count);
            Assert.Equal(ChangeKind.Replaced, changes[0].Kind);
            Assert.Equal(FileSystemEntryChangedProperties.PathSegment | FileSystemEntryChangedProperties.PositionUnderParent, changes[0].Changes);
        }

        [Fact]
        public void ChangesSinceSpanTypeChange()
        {
            var changedRoot = this.root.ReplaceDescendent(this.root["a.cs"], this.root["a.cs"].ToFileSystemDirectory());
            var changes = changedRoot.ChangesSince(this.root);
            Assert.Equal(1, changes.Count);
            Assert.Equal(ChangeKind.Replaced, changes[0].Kind);
            Assert.Equal(FileSystemEntryChangedProperties.Type, changes[0].Changes);
        }

        [Fact]
        public void ChangesSinceWithPropertyChangeInChild()
        {
            var root1 = FileSystemDirectory.Create("c:").AddChildren(
                FileSystemFile.Create("file1.txt").AddAttributes("att1")).AsRoot;
            var root2 = root1["file1.txt"].AsFileSystemFile.AddAttributes("att2").Root;
            IReadOnlyList<FileSystemEntry.DiffGram> changes = root2.ChangesSince(root1);
            var changesList = changes.ToList();
            Assert.Equal(1, changesList.Count);
            Assert.Same(root1["file1.txt"].FileSystemEntry, changesList[0].Before);
            Assert.Same(root2["file1.txt"].FileSystemEntry, changesList[0].After);
            Assert.Equal(ChangeKind.Replaced, changesList[0].Kind);
            Assert.Equal(FileSystemEntryChangedProperties.Attributes, changesList[0].Changes);
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

        [Fact]
        public void RootedStruct_IsDefault()
        {
            var file = new RootedFileSystemDirectory();
            Assert.True(file.IsDefault);
            file = FileSystemDirectory.Create("c:").AsRoot;
            Assert.False(file.IsDefault);
        }

        [Fact]
        public void RootedStruct_ImplicitConversionToGreenNode()
        {
            RootedFileSystemDirectory rootedDrive = RootedFileSystemDirectory.Create("c:");
            FileSystemDirectory unrootedDrive = rootedDrive;
            Assert.Same(rootedDrive.FileSystemDirectory, unrootedDrive);
            FileSystemEntry unrootedEntry = rootedDrive;
            Assert.Same(rootedDrive.FileSystemDirectory, unrootedEntry);
        }

        [Fact]
        public void RootedStruct_EqualityOperators()
        {
            var r1a = RootedFileSystemDirectory.Create("foo");
            var r1b = r1a; // struct copy
            var r2 = RootedFileSystemDirectory.Create("foo");

            // Compare two structs with the same underlying green node reference.
            Assert.True(r1a == r1b);
            Assert.False(r1a != r1b);

            // Compare two structs with different underlying green node references.
            Assert.False(r1a == r2);
            Assert.True(r1a != r2);

            // Now verify the root node reference aspect to it.
            var newRoot = RootedFileSystemDirectory.Create("c:")
                .AddChild(r1a.FileSystemDirectory).Parent;
            var r1Rerooted = r1a.FileSystemDirectory.WithRoot(newRoot);
            Assert.False(r1a == r1Rerooted);
            Assert.True(r1a != r1Rerooted);
        }

        private static void VerifyDescendentsShareRoot(RootedFileSystemDirectory directory)
        {
            foreach (var child in directory)
            {
                Assert.Same(directory.Root.FileSystemDirectory, child.Root.FileSystemDirectory);

                if (child.IsFileSystemDirectory)
                {
                    VerifyDescendentsShareRoot(child.AsFileSystemDirectory);
                }
            }
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

    partial struct RootedFileSystemFile
    {
        public string FullPath
        {
            get
            {
                var path = new StringBuilder(this.PathSegment);
                var parent = this.Parent;
                while (parent.FileSystemDirectory != null)
                {
                    path.Insert(0, Path.DirectorySeparatorChar);
                    path.Insert(0, parent.PathSegment);
                    parent = parent.Parent;
                }

                return path.ToString();
            }
        }

        public override string ToString()
        {
            return this.FullPath;
        }
    }

    partial struct RootedFileSystemDirectory
    {
        public RootedFileSystemEntry this[string pathSegment]
        {
            get
            {
                int index = this.greenNode.Children.IndexOf(FileSystemFile.Create(pathSegment));
                if (index < 0)
                {
                    throw new IndexOutOfRangeException();
                }

                return this.greenNode.Children[index].WithRoot(this.root);
            }
        }

        public string FullPath
        {
            get
            {
                var path = new StringBuilder(this.PathSegment);
                path.Append(Path.DirectorySeparatorChar);
                var parent = this.Parent;
                while (parent.FileSystemDirectory != null)
                {
                    path.Insert(0, Path.DirectorySeparatorChar);
                    path.Insert(0, parent.PathSegment);
                }

                return path.ToString();
            }
        }

        public override string ToString()
        {
            return this.FullPath;
        }
    }

    partial struct RootedFileSystemEntry
    {
        public string FullPath
        {
            get
            {
                return this.IsFileSystemDirectory ? this.AsFileSystemDirectory.FullPath : this.AsFileSystemFile.FullPath;
            }
        }

        public override string ToString()
        {
            return this.FullPath;
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
