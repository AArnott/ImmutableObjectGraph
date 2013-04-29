namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using Xunit;

	public class PersonBuilderTests {
		[Fact]
		public void ToBuilderReturnsSimilarObject() {
			var person = Person.Create("bill", age: 10);

			var personBuilder = person.ToBuilder();
			Assert.NotNull(personBuilder);
			Assert.Equal(person.Name, personBuilder.Name);
			Assert.Equal(person.Age, personBuilder.Age);
		}

		[Fact]
		public void ToBuilderToImmutableRoundtripReusesInstance() {
			var person = Person.Create("bill", age: 10);
			var personBuilder = person.ToBuilder();
			var roundTripPerson = personBuilder.ToImmutable();
			Assert.Same(person, roundTripPerson);
		}

		[Fact]
		public void MutablePropertiesRetainChanges() {
			var person = Person.Create("bill", age: 10);
			var personBuilder = person.ToBuilder();

			personBuilder.Name = "billy";
			personBuilder.Age = 8;

			Assert.Equal("billy", personBuilder.Name);
			Assert.Equal(8, personBuilder.Age);
		}

		[Fact]
		public void ToImmutableReturnsSimilarObject() {
			var person = Person.Create("bill", age: 10);
			var personBuilder = person.ToBuilder();

			personBuilder.Name = "billy";
			personBuilder.Age = 8;

			var recreatedPerson = personBuilder.ToImmutable();
			Assert.Equal(personBuilder.Name, recreatedPerson.Name);
			Assert.Equal(personBuilder.Age, recreatedPerson.Age);
		}

		[Fact]
		public void ToImmutableCalledRepeatedlyAfterChangesReusesInstance() {
			var person = Person.Create(null);
			var builder = person.ToBuilder();
			Assert.Same(person, builder.ToImmutable());
			builder.Name = "bill";
			var bill1 = builder.ToImmutable();
			var bill2 = builder.ToImmutable();
			Assert.NotSame(person, bill1);
			Assert.Same(bill1, bill2);
		}

		[Fact]
		public void PropertiesAreAlsoBuilders() {
			var person = Person.Create("bill", watch: Watch.Create());
			var personBuilder = person.ToBuilder();
			personBuilder.Watch.Color = "Red";
			var modifiedPerson = personBuilder.ToImmutable();
			Assert.Equal("Red", modifiedPerson.Watch.Color);

			personBuilder.Watch = null;
			var personWithoutWatch = personBuilder.ToImmutable();
			Assert.Null(personWithoutWatch.Watch);
		}

		[Fact]
		public void PropertyBuildersAreNullIfImmutableIsNull() {
			var person = Person.Create("bill");
			var builder = person.ToBuilder();
			Assert.Null(builder.Watch);
		}
	}
}
