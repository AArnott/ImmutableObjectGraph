namespace XmlNodeTree {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Xunit;

	public class XmlNodeTests {
		[Fact]
		public void XmlTreeConstruction() {
			XmlNode root = XmlNode.Create("Root").WithChildren(
				XmlNode.Create("Child1"),
				XmlNode.Create("Child2"));
		}
	}

	[DebuggerDisplay("<{TagName,nq}>")]
	partial class XmlNode {
		static partial void CreateDefaultTemplate(ref Template template) {
			template.Children = ImmutableList.Create<XmlNode>();
		}
	}
}
