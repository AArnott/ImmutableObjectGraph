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
