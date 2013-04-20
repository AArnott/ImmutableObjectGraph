namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using Xunit;

	public class FamilyBuilderTests {
		[Fact]
		public void BuildersWithCollectionsShouldUseCollectionBuilders() {
			var billy = Person.Create(name: "billy");
			var familyBuilder = Family.Create().ToBuilder();
			familyBuilder.Members.Add(billy);
			var family = familyBuilder.ToImmutable();
			Assert.Contains(billy, family.Members);
		}
	}
}
