namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Xunit;

	public class FileSystemTests {
		private FileSystemDirectory root;

		public FileSystemTests() {
			this.root = FileSystemDirectory.Create("c:").AddChildren(
				FileSystemFile.Create("a.cs"),
				FileSystemFile.Create("b.cs"),
				FileSystemDirectory.Create("c").AddChildren(
					FileSystemFile.Create("d.cs")));
		}

		[Fact]
		public void RecursiveDirectories() {
			var emptyRoot = FileSystemDirectory.Create("c:");
			Assert.True(emptyRoot is IEnumerable<FileSystemEntry>);
			Assert.Equal(0, emptyRoot.Count()); // using Linq exercises the enumerable

			Assert.Equal(3, this.root.Count());  // use Linq to exercise enumerator
			Assert.Equal(1, this.root.OfType<FileSystemDirectory>().Single().Count());
		}

		[Fact]
		public void NonRecursiveFiles() {
			Assert.False(FileSystemFile.Create("a") is System.Collections.IEnumerable);
		}

		[Fact]
		public void TypeConversion() {
			FileSystemFile file = FileSystemFile.Create("a");
			FileSystemDirectory folder = file.ToFileSystemDirectory();
			Assert.Equal(file.PathSegment, folder.PathSegment);
			FileSystemFile fileAgain = folder.ToFileSystemFile();
			Assert.Equal(file.PathSegment, fileAgain.PathSegment);
		}

		[Fact]
		public void ReplaceDescendentUpdatesProperty() {
			var leafToModify = this.root.OfType<FileSystemDirectory>().Single(c => c.PathSegment == "c").Children.Single();
			var updatedLeaf = leafToModify.WithPathSegment("e.cs");
			var updatedTree = this.root.ReplaceDescendent(leafToModify, updatedLeaf);
			Assert.Equal(this.root.PathSegment, updatedTree.PathSegment);
			var leafFromUpdatedTree = updatedTree.OfType<FileSystemDirectory>().Single(c => c.PathSegment == "c").Children.Single();
			Assert.Equal(updatedLeaf.PathSegment, leafFromUpdatedTree.PathSegment);
		}

		[Fact]
		public void ReplaceDescendentChangesType() {
			var leafToModify = this.root.OfType<FileSystemDirectory>().Single(c => c.PathSegment == "c").Children.Single();
			var updatedLeaf = leafToModify.ToFileSystemDirectory().WithPathSegment("f");
			var updatedTree = this.root.ReplaceDescendent(leafToModify, updatedLeaf);
			var leafFromUpdatedTree = updatedTree.OfType<FileSystemDirectory>().Single(c => c.PathSegment == "c").Children.Single();
			Assert.IsType<FileSystemDirectory>(leafFromUpdatedTree);
			Assert.Equal(updatedLeaf.PathSegment, leafFromUpdatedTree.PathSegment);
		}

		[Fact]
		public void ReplaceDescendentNotFound() {
			Assert.Throws<ArgumentException>(() => this.root.ReplaceDescendent(FileSystemFile.Create("nonexistent"), FileSystemFile.Create("replacement")));
		}

		[Fact]
		public void AddDescendent() {
			FileSystemDirectory subdir = this.root.OfType<FileSystemDirectory>().First();
			FileSystemFile newLeaf = FileSystemFile.Create("added.txt");
			FileSystemDirectory updatedRoot = this.root.AddDescendent(newLeaf, subdir);
			Assert.Equal(this.root.Identity, updatedRoot.Identity);
			FileSystemDirectory updatedSubdir = updatedRoot.OfType<FileSystemDirectory>().First();
			Assert.True(updatedSubdir.Contains(newLeaf));
		}

		[Fact]
		public void RemoveDescendent() {
			FileSystemDirectory subdir = this.root.OfType<FileSystemDirectory>().First(d => d.Children.OfType<FileSystemFile>().Any());
			FileSystemFile fileUnderSubdir = subdir.Children.OfType<FileSystemFile>().First();
			FileSystemDirectory updatedRoot = this.root.RemoveDescendent(fileUnderSubdir);
			Assert.Equal(this.root.Identity, updatedRoot.Identity);
			FileSystemDirectory updatedSubdir = (FileSystemDirectory)updatedRoot.Single(c => c.Identity == subdir.Identity);
			Assert.False(updatedSubdir.Contains(fileUnderSubdir));
		}

		[Fact]
		public void RedNodeStuff() {
			var redRoot = this.root.AsRoot;
			Assert.Equal(this.root.PathSegment, redRoot.PathSegment);
			Assert.Equal(this.root.Children.Count, redRoot.Children.Count);
			Assert.True(redRoot.Children.Any(c => c.PathSegment == "a.cs" && c.IsFileSystemFile));
			Assert.True(redRoot.Children.Any(c => c.PathSegment == "b.cs" && c.IsFileSystemFile));

			RootedFileSystemDirectory subdir = redRoot.Children.Last().AsFileSystemDirectory;
			Assert.Equal("d.cs", subdir.Children.Single().PathSegment);
		}

		[Fact]
		public void AllRedDescendentsShareRoot() {
			VerifyDescendentsShareRoot(this.root.AsRoot);
		}

		[Fact]
		public void RedNodeEquality() {
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
		public void GetHashCodeMatchesGreenNode() {
			var greenLeaf = (FileSystemDirectory)this.root.Children.Last();
			var redLeaf = greenLeaf.WithRoot(this.root);
			var redLeafAsRoot = greenLeaf.AsRoot;

			Assert.Equal(greenLeaf.GetHashCode(), redLeaf.GetHashCode());
			Assert.Equal(greenLeaf.GetHashCode(), redLeafAsRoot.GetHashCode());
		}

		[Fact]
		public void ModifyPropertyInLeafRewritesSpine() {
			var redRoot = this.root.AsRoot;
			var leaf = redRoot.Children.Last().AsFileSystemDirectory.Children.First().AsFileSystemFile;
			var newLeaf = leaf.WithPathSegment("changed");
			var leafFromNewRoot = newLeaf.Root.Children.Last().AsFileSystemDirectory.Children.First().AsFileSystemFile;
			Assert.Equal(newLeaf, leafFromNewRoot);
		}

		[Fact]
		public void ModifyPropertyInLeafRewritesSpineWithLookupTable() {
			var root = this.GetRootWithLookupTable();
			var redRoot = root.AsRoot;
			var leaf = redRoot.Children.Single(l => l.IsFileSystemDirectory).AsFileSystemDirectory.Children.First().AsFileSystemFile;
			var newLeaf = leaf.WithPathSegment("changed");
			var leafFromNewRoot = newLeaf.Root.Children.Single(l => l.IsFileSystemDirectory).AsFileSystemDirectory.Children.First().AsFileSystemFile;
			Assert.Equal(newLeaf, leafFromNewRoot);
		}

		[Fact]
		public void ModifyPropertyInRootWithLookupTablePreservesLookupTable() {
			var root = this.GetRootWithLookupTable();
			var redRoot = root.AsRoot;
			root.Children.First().WithRoot(root); // force lazy construction of lookup table
			var newRoot = redRoot.WithPathSegment("changed");
		}

		[Fact]
		public void WithRootInUnrelatedTreeThrows() {
			var leaf = FileSystemDirectory.Create("z");
			Assert.Throws<ArgumentException>(() => leaf.WithRoot(this.root));
		}

		[Fact]
		public void RedNodeWithBulkMethodOnChild() {
			var redRoot = this.root.AsRoot;
			var firstChild = redRoot.Children.First();
			RootedFileSystemEntry modifiedChild = firstChild.With(pathSegment: "g");
			Assert.Equal("g", modifiedChild.PathSegment);
		}

		[Fact]
		public void RedNodeWithBulkMethodOnRoot() {
			var redRoot = this.root.AsRoot;
			RootedFileSystemDirectory modifiedRoot = redRoot.With(pathSegment: "g");
			Assert.Equal("g", modifiedRoot.PathSegment);
		}

		[Fact]
		public void ConvertRootedFileToDirectory() {
			RootedFileSystemFile redFile = this.root.AsRoot.Children.First().AsFileSystemFile;
			RootedFileSystemDirectory redDirectory = redFile.ToFileSystemDirectory();
			Assert.True(redDirectory.Root.Children.Contains(redDirectory.AsFileSystemEntry));
		}

		[Fact]
		public void ConvertRootedDirectoryToFile() {
			RootedFileSystemDirectory redDirectory = this.root.AsRoot.Children.Last().AsFileSystemDirectory;
			var redFile = redDirectory.ToFileSystemFile();
			Assert.True(redFile.Root.Children.Contains(redFile.AsFileSystemEntry));
		}

		[Fact]
		public void ConvertFileAsEntryToFileRetainsIdentity() {
			RootedFileSystemEntry redEntry = this.root.AsRoot.Children.First();
			var redFile = redEntry.ToFileSystemFile();
			Assert.True(redFile.Root.Children.Contains(redFile.AsFileSystemEntry));
			Assert.Same(redEntry.FileSystemEntry, redFile.FileSystemFile);
		}

		[Fact]
		public void LookupTableIntactAfterMutatingNonRecursiveField() {
			var root = this.GetRootWithLookupTable();
			var modifiedRoot = root.WithPathSegment("d:");
		}

		private static void VerifyDescendentsShareRoot(RootedFileSystemDirectory directory) {
			foreach (var child in directory) {
				Assert.Same(directory.Root.FileSystemDirectory, child.Root.FileSystemDirectory);

				if (child.IsFileSystemDirectory) {
					VerifyDescendentsShareRoot(child.AsFileSystemDirectory);
				}
			}
		}

		private FileSystemDirectory GetRootWithLookupTable() {
			// Fill in a bunch of children to force the creation of a lookup table.
			var root = this.root.AddChildren(Enumerable.Range(100, 30).Select(n => FileSystemFile.Create("filler" + n)));
			return root;
		}
	}

	[DebuggerDisplay("{FullPath}")]
	partial class FileSystemFile {
		static partial void CreateDefaultTemplate(ref FileSystemFile.Template template) {
			template.Attributes = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	partial struct RootedFileSystemFile {
		public override string ToString() {
			return this.PathSegment;
		}
	}

	[DebuggerDisplay("{FullPath}")]
	partial class FileSystemDirectory {
		public override string FullPath {
			get { return base.FullPath + Path.DirectorySeparatorChar; }
		}

		static partial void CreateDefaultTemplate(ref FileSystemDirectory.Template template) {
			template.Children = ImmutableSortedSet.Create<FileSystemEntry>(SiblingComparer.Instance);
		}
	}

	partial class FileSystemEntry {
		public virtual string FullPath {
			get {
				// TODO: when we get properties that point back to the root, fix this to include the full path.
				return this.PathSegment;
			}
		}

		public class SiblingComparer : IComparer<FileSystemEntry> {
			public static SiblingComparer Instance = new SiblingComparer();

			private SiblingComparer() {
			}

			public int Compare(FileSystemEntry x, FileSystemEntry y) {
				return StringComparer.OrdinalIgnoreCase.Compare(x.PathSegment, y.PathSegment);
			}
		}
	}
}
