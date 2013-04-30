namespace ImmutableObjectGraph.Tests {
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
			var root = TreeNode.Create("temp", @"c:\temp\").WithChildren(
				TreeNode.Create("a.cs", @"c:\temp\a.cs"),
				TreeNode.Create("subfolder", @"c:\temp\subfolder\").WithChildren(
					TreeNode.Create("g.h", @"c:\temp\subfolder\g.h")));

			Assert.Equal("temp", root.Caption);
			Assert.Equal("a.cs", root.Children[0].Caption);
			Assert.Equal("subfolder", root.Children[1].Caption);
			Assert.Equal("g.h", root.Children[1].Children[0].Caption);

			var root2 = root.AddChildren(TreeNode.Create("b.cs", @"c:\temp\b.cs"));
			Assert.Equal(3, root2.Children.Count);

			var root3 = root2.RemoveChildren(root2.Children[0]);
			Assert.Equal(2, root3.Children.Count);
			Assert.Equal(root2.Children[1], root3.Children[0]);
		}
	}

	[DebuggerDisplay("{FilePath}")]
	partial class TreeNode {
		static partial void CreateDefaultTemplate(ref TreeNode.Template template) {
			template.Children = ImmutableList.Create<TreeNode>();
			template.Visible = true;
			template.Attributes = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);
		}

		[DebuggerDisplay("{FilePath}")]
		partial class Builder {
		}
	}
}
