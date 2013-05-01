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
		[Fact]
		public void RecursiveDirectories() {
			var root = FileSystemDirectory.Create("c:");
			Assert.True(root is System.Collections.IEnumerable);
			Assert.Equal(0, root.Count()); // using Linq implicitly proves it is enumerable

			var rootWithChildren = root.AddChildren(
				FileSystemFile.Create("a.cs"),
				FileSystemFile.Create("b.cs"));
			Assert.Equal(2, rootWithChildren.Count());  // use Linq to exercise enumerator
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
			template.Children = ImmutableHashSet.Create<FileSystemEntry>(SiblingComparer.Instance);
		}
	}

	partial class FileSystemEntry {
		public virtual string FullPath {
			get {
				// TODO: when we get properties that point back to the root, fix this to include the full path.
				return this.PathSegment;
			}
		}

		public class SiblingComparer : IEqualityComparer<FileSystemEntry> {
			public static SiblingComparer Instance = new SiblingComparer();

			private SiblingComparer() {
			}

			public bool Equals(FileSystemEntry x, FileSystemEntry y) {
				return StringComparer.OrdinalIgnoreCase.Equals(x.PathSegment, y.PathSegment);
			}

			public int GetHashCode(FileSystemEntry obj) {
				return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.PathSegment);
			}
		}
	}
}
