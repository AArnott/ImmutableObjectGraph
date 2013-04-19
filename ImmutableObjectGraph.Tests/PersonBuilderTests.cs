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
			var person = Person.Default.With(name: "bill", age: 10);

			var personBuilder = person.ToBuilder();
			Assert.NotNull(personBuilder);
			Assert.Equal(person.Name, personBuilder.Name);
			Assert.Equal(person.Age, personBuilder.Age);
		}

		[Fact]
		public void ToBuilderToImmutableRoundtripReusesInstance() {
			var person = Person.Default.With(name: "bill", age: 10);
			var personBuilder = person.ToBuilder();
			var roundTripPerson = personBuilder.ToImmutable();
			Assert.Same(person, roundTripPerson);
		}

		[Fact]
		public void MutablePropertiesRetainChanges() {
			var person = Person.Default.With(name: "bill", age: 10);
			var personBuilder = person.ToBuilder();

			personBuilder.Name = "billy";
			personBuilder.Age = 8;

			Assert.Equal("billy", personBuilder.Name);
			Assert.Equal(8, personBuilder.Age);
		}

		[Fact]
		public void ToImmutableReturnsSimilarObject() {
			var person = Person.Default.With(name: "bill", age: 10);
			var personBuilder = person.ToBuilder();

			personBuilder.Name = "billy";
			personBuilder.Age = 8;

			var recreatedPerson = personBuilder.ToImmutable();
			Assert.Equal(personBuilder.Name, recreatedPerson.Name);
			Assert.Equal(personBuilder.Age, recreatedPerson.Age);
		}
	}
}
