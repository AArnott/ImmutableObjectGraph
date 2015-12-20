namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class GenerationsTests
    {
        [Fact]
        public void FirstGenerationAccessible()
        {
            var value = Generations.Create("first", "last");
            Assert.Equal(0, value.Age);
            Assert.Equal("first", value.FirstName);
            Assert.Equal("last", value.LastName);
        }

        [Fact]
        public void SecondGenerationAccessible()
        {
            var value = Generations.Create2(5, "first", "last");
            Assert.Equal(5, value.Age);
            Assert.Equal("first", value.FirstName);
            Assert.Equal("last", value.LastName);
        }
    }
}
