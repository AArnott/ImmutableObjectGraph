namespace DirectoryTree {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Xunit;

	public class TreeNodeTests {
		[Fact]
		public void TreeConstruction() {
			var root = TreeNode.Create("temp", @"c:\temp\")
				.WithChildren(TreeNode.Create("a.cs", @"c:\temp\a.cs"));

			Assert.Equal("temp", root.Caption);
			Assert.Equal("a.cs", root.Children[0].Caption);
		}
	}

	[DebuggerDisplay("{FilePath}")]
	partial class TreeNode {
		static partial void CreateDefaultTemplate(ref TreeNode.Template template) {
			template.Children = ImmutableSortedSet.Create<TreeNode>();
			template.Visible = true;
			template.Attributes = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);
		}

		[DebuggerDisplay("{FilePath}")]
		partial class Builder {
		}
	}
}
