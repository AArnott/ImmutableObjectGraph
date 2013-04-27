Imports Xunit

Public Class ConsumerTests
	<Fact>
	Public Sub ConsumerTest1()
		Dim xe = XmlNodeTree.XmlElement.Create("TagName").WithChildren(
			XmlNodeTree.XmlAttribute.Create("Att1"),
			XmlNodeTree.XmlElement.Create("ChildElement"))
		Assert.Equal("TagName", xe.LocalName)
		Dim xe2 = xe.WithLocalName("TagName2")
		Assert.Equal("TagName2", xe2.LocalName)
	End Sub
End Class
