namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class ImmutableDictionaryHelpersTests
    {
        private ImmutableDictionaryHelpers obj = ImmutableDictionaryHelpers.Create();

        [Fact]
        public void AddMatch()
        {
            obj = obj.AddMatch("five", 5);
            Assert.Equal(5, obj.Matches["five"]);

            // Add should throw if the entry already exists.
            Assert.Throws<ArgumentException>(() => obj.AddMatch("five", 8));

            var rooted = obj.AsRoot.AddMatch("six", 6);
            Assert.Equal(6, rooted.Matches["six"]);
        }

        [Fact]
        public void SetMatch()
        {
            obj = obj.SetMatch("five", 5);
            Assert.Equal(5, obj.Matches["five"]);

            obj = obj.SetMatch("five", 8);
            Assert.Equal(8, obj.Matches["five"]);

            var rooted = obj.AsRoot.SetMatch("five", 6);
            Assert.Equal(6, rooted.Matches["five"]);
        }

        [Fact]
        public void RemoveMatch()
        {
            obj = obj.AddMatch("five", 5)
                .AddMatch("six", 6);
            Assert.Equal(2, obj.Matches.Count);
            obj = obj.RemoveMatch("five");
            Assert.Equal(1, obj.Matches.Count);

            var rooted = obj.AsRoot.RemoveMatch("six");
            Assert.Equal(0, rooted.Matches.Count);
        }
    }
}
