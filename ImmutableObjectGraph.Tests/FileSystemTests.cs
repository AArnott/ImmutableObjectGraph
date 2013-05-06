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

		[Fact(Skip = "It currently fails")]
		public void ReplaceDescendentNotFound() {
			Assert.Throws<ArgumentException>(() => this.root.ReplaceDescendent(FileSystemFile.Create("nonexistent"), FileSystemFile.Create("replacement")));
		}

		[Fact]
		public void RedNodeStuff() {
			var redRoot = this.root.AsRoot;
			Assert.Equal(this.root.PathSegment, redRoot.PathSegment);
			Assert.Equal(this.root.Children.Count, redRoot.Children.Count);
			Assert.True(redRoot.Children.Any(c => c.PathSegment == "a.cs"));
			Assert.True(redRoot.Children.Any(c => c.PathSegment == "b.cs"));

			RootedFileSystemDirectory subdir = ((FileSystemDirectory)redRoot.Children.Last().FileSystemEntry).WithRoot(redRoot.FileSystemDirectory);
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

		private static void VerifyDescendentsShareRoot(RootedFileSystemDirectory directory) {
			foreach (var child in directory) {
				Assert.Same(directory.Root.FileSystemDirectory, child.Root.FileSystemDirectory);

				var subdirectory = child.FileSystemEntry as FileSystemDirectory;
				if (subdirectory != null) {
					VerifyDescendentsShareRoot(subdirectory.WithRoot(directory.Root.FileSystemDirectory));
				}
			}
		}
	}

	[DebuggerDisplay("{FullPath}")]
	partial class FileSystemFile {
		static partial void CreateDefaultTemplate(ref FileSystemFile.Template template) {
			template.Attributes = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);
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
