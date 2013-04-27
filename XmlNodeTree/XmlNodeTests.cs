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
			XmlNode root = XmlElement.Create("Root").WithChildren(
				XmlElement.Create("Child1"),
				XmlElement.Create("Child2"));

			var xe = XmlElement.Create();
			XmlElement result = xe.With("hi");
		}
	}

	[DebuggerDisplay("<{TagName,nq}>")]
	partial class XmlElement {
		static partial void CreateDefaultTemplate(ref Template template) {
			template.Children = ImmutableList.Create<XmlNode>();
		}
	}
}
