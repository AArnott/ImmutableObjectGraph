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
			XmlElement root = XmlElement.Create("Root").WithChildren(
				XmlElement.Create("Child1"),
				XmlElement.Create("Child2"));

			XmlElement xe = XmlElement.Create("l1", "n1");
			XmlElement xel2 = xe.With("l2");
			Assert.Equal("l2", xel2.LocalName);
			Assert.Equal("n1", xel2.NamespaceName);
			XmlElement xel3n3 = xe.With("l3", "n3");
			Assert.Equal("l3", xel3n3.LocalName);
			Assert.Equal("n3", xel3n3.NamespaceName);
		}

		/// <summary>
		/// Verifies that the With overloads don't cause compiler errors due to ambiguity.
		/// </summary>
		[Fact]
		public void WithNoArguments() {
			XmlElement e = XmlElement.Create("ln1", "ns1");
			XmlElement e2 = e.With();
			Assert.Equal("ln1", e2.LocalName);
			Assert.Equal("ns1", e2.NamespaceName);
		}
	}

	[DebuggerDisplay("<{TagName,nq}>")]
	partial class XmlElement {
		static partial void CreateDefaultTemplate(ref Template template) {
			template.Children = ImmutableList.Create<XmlNode>();
		}
	}
}
