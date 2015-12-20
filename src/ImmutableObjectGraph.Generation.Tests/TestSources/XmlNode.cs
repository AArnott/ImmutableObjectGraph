namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [GenerateImmutable(DefineWithMethodsPerProperty = true)]
    abstract partial class XmlNode
    {
        readonly string localName;
    }

    [GenerateImmutable(DefineWithMethodsPerProperty = true)]
    partial class XmlElement : XmlNode
    {
        readonly string namespaceName;
        readonly ImmutableList<XmlNode> children;
    }

    [GenerateImmutable(DefineWithMethodsPerProperty = true)]
    partial class XmlElementWithContent : XmlElement
    {
        readonly string content;
    }

    [GenerateImmutable(DefineWithMethodsPerProperty = true)]
    partial class XmlAttribute : XmlNode
    {
        readonly string namespaceName;
        readonly string value;
    }
}
