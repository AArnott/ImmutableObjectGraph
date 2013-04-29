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
			Assert.Null(Family.Create().WithMembers((ImmutableSortedSet<Person>)null).Members);
		}

		[Fact]
		public void DefaultValuesCanBeOverriddenWithOtherValue() {
			Assert.NotNull(Family.Create().Members); // the test is only valid if the default value is non-null

			var otherMembers = ImmutableSortedSet.Create(Person.Create("bill"));
			Assert.Same(otherMembers, Family.Create(members: otherMembers).Members);
			Assert.Same(otherMembers, Family.Create().WithMembers(otherMembers).Members);
		}

		[Fact]
		public void ImmutableCollection() {
			var members = ImmutableSortedSet.Create<Person>();
			var family = Family.Create(members: members);
			Assert.Same(members, family.Members);
			var newMembers = family.Members.Add(Person.Create("bill"));
			Assert.Same(members, family.Members);
			Assert.NotSame(newMembers, members);

			var newFamily = family.WithMembers(newMembers);
			Assert.Same(newMembers, newFamily.Members);
		}

		[Fact]
		public void CollectionsAlternateMutationMethods() {
			var family = Family.Create();
			var familyAdd1 = family.AddMembers(Person.Create("billy", age: 5));
			Assert.Equal(0, family.Members.Count);
			Assert.Equal(1, familyAdd1.Members.Count);

			var familyAdd1More = familyAdd1.AddMembers(Person.Create("sally", age: 8));
			Assert.Equal(2, familyAdd1More.Members.Count);

			var familyRemove1 = familyAdd1More.RemoveMembers(familyAdd1.Members[0]);
			Assert.Equal(2, familyAdd1More.Members.Count);
			Assert.Equal(1, familyRemove1.Members.Count);

			var familyAddMany = familyAdd1.AddMembers(
				Person.Create("sally", age: 8),
				Person.Create("sam", age: 4));
			Assert.Equal(3, familyAddMany.Members.Count);

			var familyRemoveMany = familyAddMany.RemoveMembers(familyAdd1More.Members);
			Assert.Equal(1, familyRemoveMany.Members.Count);

			var familyCleared = familyAddMany.RemoveMembers();
			Assert.Equal(0, familyCleared.Members.Count);
		}
	}
}
