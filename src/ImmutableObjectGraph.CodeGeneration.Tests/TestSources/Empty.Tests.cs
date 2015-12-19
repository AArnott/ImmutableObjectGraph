namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class EmptyTests
    {
        [Fact]
        public void EmptyCreateBuilder()
        {
            var emptyBuilder = Empty.CreateBuilder();
            Assert.NotNull(emptyBuilder);
        }

        [Fact]
        public void EmptyConstruction()
        {
            var empty = Empty.Create();
            Assert.NotNull(empty);
        }

        [Fact]
        public void EmptyDerivedConstruction()
        {
            var derivedEmpty = EmptyDerived.Create();
            Assert.NotNull(derivedEmpty);
        }

        [Fact]
        public void EmptyWithRegeneratesType()
        {
            var empty = EmptyDerivedFromAbstract.Create();
            AbstractNonEmpty emptyAsBase = empty;

            EmptyDerivedFromAbstract newInstance = empty.With(oneField: true);
            AbstractNonEmpty newInstanceAsBase = emptyAsBase.With(oneField: true);

            Assert.Equal(empty.Identity, newInstance.Identity);
            Assert.Equal(empty.Identity, newInstanceAsBase.Identity);
        }

        [Fact]
        public void EmptyDerivedFromNonEmptyBaseReinstantiatesCorrectType()
        {
            EmptyDerivedFromNonEmptyBase value = EmptyDerivedFromNonEmptyBase.Create();
            EmptyDerivedFromNonEmptyBase updatedValue = value.WithOneField(true);
            EmptyDerivedFromNonEmptyBase updatedValue2 = value.With(true);
        }
    }
}
