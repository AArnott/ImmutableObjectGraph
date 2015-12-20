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
        public void Create0()
        {
            var value = Generations.Create("first", "last");
            Assert.Equal(0, value.Age);
            Assert.Equal("first", value.FirstName);
            Assert.Equal("last", value.LastName);
        }

        [Fact]
        public void Create2()
        {
            var value = Generations.Create2(5, "first", "last");
            Assert.Equal(5, value.Age);
            Assert.Equal("first", value.FirstName);
            Assert.Equal("last", value.LastName);
        }

        [Fact]
        public void With0()
        {
            var value = Generations.Create()
                .With("first", "last");
            Assert.Equal(0, value.Age);
            Assert.Equal("first", value.FirstName);
            Assert.Equal("last", value.LastName);
        }

        [Fact]
        public void With_PreservesFieldsFromLaterGenerations()
        {
            var value = Generations.Create2(age: 5)
                .With("first");
            Assert.Equal(5, value.Age);
        }

        [Fact]
        public void With2()
        {
            var value = Generations.Create()
                .With2(5, "first", "last");
            Assert.Equal(5, value.Age);
            Assert.Equal("first", value.FirstName);
            Assert.Equal("last", value.LastName);
        }

        [Fact]
        public void Derived_Create0()
        {
            var value = GenerationsDerived.Create("first", "last", "position");
            Assert.Equal(0, value.Age);
            Assert.Equal("first", value.FirstName);
            Assert.Equal("last", value.LastName);
            Assert.Null(value.Title);
            Assert.Equal("position", value.Position);
        }

        [Fact]
        public void Derived_Create2()
        {
            var value = GenerationsDerived.Create2(5, "first", "last", "title", "position");
            Assert.Equal(5, value.Age);
            Assert.Equal("first", value.FirstName);
            Assert.Equal("last", value.LastName);
            Assert.Equal("title", value.Title);
            Assert.Equal("position", value.Position);
        }

        [Fact]
        public void Derived_With0()
        {
            var value = GenerationsDerived.Create()
                .With("first", "last", "position");
            Assert.Equal(0, value.Age);
            Assert.Equal("first", value.FirstName);
            Assert.Equal("last", value.LastName);
            Assert.Null(value.Title);
            Assert.Equal("position", value.Position);
        }

        [Fact]
        public void Derived_With_PreservesFieldsFromLaterGenerations()
        {
            var value = GenerationsDerived.Create2(age: 5, title: "title")
                .With("first", position: "position");
            Assert.Equal(5, value.Age);
            Assert.Equal("title", value.Title);
        }

        [Fact]
        public void Derived_With2()
        {
            var value = GenerationsDerived.Create()
                .With2(5, "first", "last", "title", "position");
            Assert.Equal(5, value.Age);
            Assert.Equal("first", value.FirstName);
            Assert.Equal("last", value.LastName);
            Assert.Equal("title", value.Title);
            Assert.Equal("position", value.Position);
        }
    }
}
