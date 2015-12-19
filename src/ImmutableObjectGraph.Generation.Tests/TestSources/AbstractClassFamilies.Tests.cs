namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class AbstractClassFamiliesTests
    {
        /// <summary>
        /// Verifies that With methods in a type hierarchy all perform as expected.
        /// </summary>
        [Fact]
        public void AbstractFamilyTreeWithTypes()
        {
            ConcreteOf2Abstracts ca = ConcreteOf2Abstracts.Create(1, 2, 3, 4, 5, 6);
            Abstract1 a1a = ca;
            Abstract2 a2a = ca;

            ConcreteOf2Abstracts cb = ca.With(abstract1Field1: 7, abstract2Field1: 8, concreteField1: 9);
            Assert.Equal(7, cb.Abstract1Field1);
            Assert.Equal(2, cb.Abstract1Field2);
            Assert.Equal(8, cb.Abstract2Field1);
            Assert.Equal(4, cb.Abstract2Field2);
            Assert.Equal(9, cb.ConcreteField1);
            Assert.Equal(6, cb.ConcreteField2);

            Abstract1 a1b = a1a.With(abstract1Field1: 7);
            Assert.Equal(7, a1b.Abstract1Field1);
            Assert.Equal(2, a1b.Abstract1Field2);
            Assert.Equal(3, ((ConcreteOf2Abstracts)a1b).Abstract2Field1);
            Assert.Equal(4, ((ConcreteOf2Abstracts)a1b).Abstract2Field2);
            Assert.Equal(5, ((ConcreteOf2Abstracts)a1b).ConcreteField1);
            Assert.Equal(6, ((ConcreteOf2Abstracts)a1b).ConcreteField2);

            Abstract2 a2b = a2a.With(abstract1Field1: 7, abstract2Field1: 8);
            Assert.Equal(7, a2b.Abstract1Field1);
            Assert.Equal(2, a2b.Abstract1Field2);
            Assert.Equal(8, a2b.Abstract2Field1);
            Assert.Equal(4, a2b.Abstract2Field2);
            Assert.Equal(5, ((ConcreteOf2Abstracts)a2b).ConcreteField1);
            Assert.Equal(6, ((ConcreteOf2Abstracts)a2b).ConcreteField2);
        }
    }
}
