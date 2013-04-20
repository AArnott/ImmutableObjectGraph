namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using Xunit;

	public class FamilyTests {
		[Fact]
		public void ImmutableCollection() {
			var members = ImmutableList.Create<Person>();
			var family = Family.Create(members: members);
			Assert.Same(members, family.Members);
			var newMembers = family.Members.Add(Person.Create());
			Assert.Same(members, family.Members);
			Assert.NotSame(newMembers, members);

			var newFamily = family.WithMembers(newMembers);
			Assert.Same(newMembers, newFamily.Members);
		}
	}
}
