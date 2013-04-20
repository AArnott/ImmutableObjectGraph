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
		public void DefaultValues() {
			// We expect Members to be non-null because we have a partial class defined that specifies that.
			var family = Family.Create();
			Assert.NotNull(family.Members);
			Assert.Equal(0, family.Members.Count);
		}

		[Fact]
		public void DefaultValuesCanBeOverriddenWithTypeDefaults() {
			Assert.NotNull(Family.Create().Members); // the test is only valid if the default value is non-null
			Assert.Null(Family.Create(members: null).Members);
			Assert.Null(Family.Create().WithMembers(null).Members);
		}

		[Fact]
		public void DefaultValuesCanBeOverriddenWithOtherValue() {
			Assert.NotNull(Family.Create().Members); // the test is only valid if the default value is non-null

			var otherMembers = ImmutableList.Create(Person.Create());
			Assert.Same(otherMembers, Family.Create(members: otherMembers).Members);
			Assert.Same(otherMembers, Family.Create().WithMembers(otherMembers).Members);
		}

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
