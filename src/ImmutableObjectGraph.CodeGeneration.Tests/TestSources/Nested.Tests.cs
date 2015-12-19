using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    public class NestedTests
    {
        [Fact]
        public void NestedCreateBuilder()
        {
            var nestedBuilder = Nested.NestedClass.CreateBuilder();
            Assert.NotNull(nestedBuilder);
        }

        [Fact]
        public void NestedConstruction()
        {
            var nested = Nested.NestedClass.Create("a name");
            Assert.NotNull(nested);
        }
    }
}
