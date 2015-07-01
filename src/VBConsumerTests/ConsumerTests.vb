Imports Xunit
Imports ImmutableObjectGraph.Tests

Public Class ConsumerTests
	<Fact>
	Public Sub ConsumerTest1()
		Dim xe = XmlElement.Create("TagName").WithChildren(
			XmlAttribute.Create("Att1"),
			XmlElement.Create("ChildElement"))
		Assert.Equal("TagName", xe.LocalName)
		Dim xe2 = xe.WithLocalName("TagName2")
		Assert.Equal("TagName2", xe2.LocalName)
	End Sub
End Class
