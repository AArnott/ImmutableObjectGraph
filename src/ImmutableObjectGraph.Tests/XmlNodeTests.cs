namespace ImmutableObjectGraph.Tests {
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

		[Fact]
		public void Polymorphism() {
			XmlElement e = XmlElement.Create("ln1", "ns1");
			XmlNode n = e;
			XmlNode n2 = n.WithLocalName("newName");
			Assert.Equal("newName", n2.LocalName);
			XmlElement e2 = (XmlElement)n2;
			Assert.Equal(n2.LocalName, e2.LocalName);
			Assert.Equal(e.NamespaceName, e2.NamespaceName);
		}

		[Fact]
		public void TypeConversion() {
			XmlElement ordinaryElement = XmlElement.Create("TagName");
			Assert.IsNotType(typeof(XmlElementWithContent), ordinaryElement);

			// Switch to derived type, without extra data.
			XmlElementWithContent elementWithContent = ordinaryElement.ToXmlElementWithContent();
			Assert.Equal(ordinaryElement.LocalName, elementWithContent.LocalName);

			// Switch to derived type, including extra data.
			elementWithContent = ordinaryElement.ToXmlElementWithContent("SomeContent");
			Assert.Equal(ordinaryElement.LocalName, elementWithContent.LocalName);
			Assert.Equal("SomeContent", elementWithContent.Content);

			// Switch back to base type.
			XmlElement backAgain = elementWithContent.ToXmlElement();
			Assert.IsNotType(typeof(XmlElementWithContent), backAgain);
			Assert.Equal(ordinaryElement.LocalName, backAgain.LocalName);
		}
	}

	[DebuggerDisplay("<{LocalName,nq}>")]
	partial class XmlElement {
		static partial void CreateDefaultTemplate(ref Template template) {
			template.Children = ImmutableList.Create<XmlNode>();
		}
	}

	[DebuggerDisplay("<{LocalName,nq}>{Content}</{LocalName,nq}>")]
	partial class XmlElementWithContent {
		static partial void CreateDefaultTemplate(ref Template template) {
			template.Children = ImmutableList.Create<XmlNode>();
		}
	}
}
