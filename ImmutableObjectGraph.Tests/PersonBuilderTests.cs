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
	}
}
