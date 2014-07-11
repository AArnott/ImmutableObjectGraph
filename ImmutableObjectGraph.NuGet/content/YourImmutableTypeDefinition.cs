namespace YourNamespace {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	[DebuggerDisplay("{FullPath}")]
	partial class FileSystemFile {
		static partial void CreateDefaultTemplate(ref FileSystemFile.Template template) {
			template.Attributes = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	partial struct RootedFileSystemFile {
		public string FullPath {
			get {
				var path = new StringBuilder(this.PathSegment);
				var parent = this.Parent;
				while (parent.FileSystemDirectory != null) {
					path.Insert(0, Path.DirectorySeparatorChar);
					path.Insert(0, parent.PathSegment);
					parent = parent.Parent;
				}

				return path.ToString();
			}
		}

		public override string ToString() {
			return this.FullPath;
		}
	}

	partial struct RootedFileSystemDirectory {
		public RootedFileSystemEntry this[string pathSegment] {
			get {
				int index = this.greenNode.Children.IndexOf(FileSystemFile.Create(pathSegment));
				if (index < 0) {
					throw new IndexOutOfRangeException();
				}

				return this.greenNode.Children[index].WithRoot(this.root);
			}
		}

		public string FullPath {
			get {
				var path = new StringBuilder(this.PathSegment);
				path.Append(Path.DirectorySeparatorChar);
				var parent = this.Parent;
				while (parent.FileSystemDirectory != null) {
					path.Insert(0, Path.DirectorySeparatorChar);
					path.Insert(0, parent.PathSegment);
				}

				return path.ToString();
			}
		}

		public override string ToString() {
			return this.FullPath;
		}
	}

	partial struct RootedFileSystemEntry {
		public string FullPath {
			get {
				return this.IsFileSystemDirectory ? this.AsFileSystemDirectory.FullPath : this.AsFileSystemFile.FullPath;
			}
		}

		public override string ToString() {
			return this.FullPath;
		}
	}

	[DebuggerDisplay("{PathSegment}")]
	partial class FileSystemDirectory {
		public FileSystemEntry this[string pathSegment] {
			get {
				int index = this.children.IndexOf(FileSystemFile.Create(pathSegment));
				if (index < 0) {
					throw new IndexOutOfRangeException();
				}

				return this.children[index];
			}
		}

		static partial void CreateDefaultTemplate(ref FileSystemDirectory.Template template) {
			template.Children = ImmutableSortedSet.Create<FileSystemEntry>(SiblingComparer.Instance);
		}
	}

	[DebuggerDisplay("{PathSegment}")]
	partial class FileSystemEntry {
		public class SiblingComparer : IComparer<FileSystemEntry> {
			public static SiblingComparer Instance = new SiblingComparer();

			private SiblingComparer() {
			}

			public int Compare(FileSystemEntry x, FileSystemEntry y) {
				return StringComparer.OrdinalIgnoreCase.Compare(x.PathSegment, y.PathSegment);
			}
		}

		public override string ToString() {
			return this.PathSegment;
		}
	}
}
