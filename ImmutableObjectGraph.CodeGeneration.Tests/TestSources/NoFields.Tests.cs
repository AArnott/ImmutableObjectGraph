namespace ImmutableObjectGraph.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class NoFieldsTests
    {
        [Fact]
        public void Create()
        {
            var empty = Empty.Create();
            Assert.NotNull(empty);
        }

        [Fact]
        public void CreateBuilder()
        {
            var emptyBuilder = Empty.CreateBuilder();
            Assert.NotNull(emptyBuilder);
        }
    }
}
