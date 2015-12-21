namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    using Xunit;

    public class PersonTests
    {
        /// <summary>
        /// Immutable types should have 1 public constructor to support deserialization.
        /// </summary>
        [Fact]
        public void PublicConstructor()
        {
#pragma warning disable CS0618
            var p = new Person(Name: "Andrew", Age: 15, Watch: null);
#pragma warning restore CS0618
            Assert.Equal("Andrew", p.Name);
            Assert.Equal(15, p.Age);
            Assert.Null(p.Watch);
        }

        [Fact]
        public void DefaultInstance()
        {
            var defaultPerson = Person.Create(null);
            Assert.NotNull(defaultPerson);
            Assert.Equal(0, defaultPerson.Age);
            Assert.Equal(null, defaultPerson.Name);
        }

        [Fact]
        public void CreateWithArguments()
        {
            var billyAge10 = Person.Create(name: "billy", age: 10);
            Assert.Equal("billy", billyAge10.Name);
            Assert.Equal(10, billyAge10.Age);
        }

        [Fact]
        public void SetScalarReferenceTypeProperty()
        {
            var original = Person.Create(null);
            var modified = original.WithName("bill");
            Assert.Equal("bill", modified.Name);
            Assert.Null(original.Name);
        }

        [Fact]
        public void SetScalarValueTypeProperty()
        {
            var original = Person.Create(null);
            var modified = original.WithAge(8);
            Assert.Equal(8, modified.Age);
            Assert.Equal(0, original.Age);
        }

        [Fact]
        public void SetScalarReferenceTypePropertyToSameValueReturnsSameInstance()
        {
            var expected = Person.Create(null).WithName("bill");
            var actual = expected.WithName(expected.Name);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetScalarValueTypePropertyToSameValueReturnsSameInstance()
        {
            var expected = Person.Create(null).WithAge(8);
            var actual = expected.WithAge(expected.Age);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void WithSetsNonDefaultValues()
        {
            // Initialize
            var billAge10 = Person.Create(null).With(name: "bill", age: 10);
            Assert.Equal("bill", billAge10.Name);
            Assert.Equal(10, billAge10.Age);

            // Full modification
            var jillAge9 = billAge10.With(name: "jill", age: 9);
            Assert.Equal("jill", jillAge9.Name);
            Assert.Equal(9, jillAge9.Age);

            // Partial modification
            var billAge12 = billAge10.With(age: 12);
            Assert.Equal("bill", billAge12.Name);
            Assert.Equal(12, billAge12.Age);

            var billyAge10 = billAge10.With(name: "billy");
            Assert.Equal("billy", billyAge10.Name);
            Assert.Equal(10, billyAge10.Age);
        }

        [Fact]
        public void WithRevertsToDefaultValues()
        {
            var billAge10 = Person.Create(name: "bill", age: 10);

            var billAge0 = billAge10.With(age: 0);
            Assert.Equal(0, billAge0.Age);
            Assert.Equal("bill", billAge0.Name);

            var age10 = billAge10.With(name: null);
            Assert.Equal(10, age10.Age);
            Assert.Equal(null, age10.Name);
        }

        [Fact]
        public void ReferenceToOtherImmutable()
        {
            var blackWatch = Watch.Create(color: "black", size: 10);
            var personWithBlackWatch = Person.Create(null, watch: blackWatch);

            var silverWatch = blackWatch.WithColor("silver");
            var personWithSilverWatch = personWithBlackWatch.WithWatch(silverWatch);
            Assert.Equal(silverWatch, personWithSilverWatch.Watch);
        }

        [Fact]
        public void WithPreservesInstanceWhenNoChangesMade()
        {
            var bill = Person.Create(name: "bill");
            Assert.Same(bill, bill.With());
            Assert.Same(bill, bill.With(name: "bill"));
        }

        [Fact]
        public void DefaultValues()
        {
            // We expect Members to be non-null because we have a partial class defined that specifies that.
            var family = Family.Create();
            Assert.NotNull(family.Members);
            Assert.Equal(0, family.Members.Count);
        }

        [Fact]
        public void DefaultValuesCanBeOverriddenWithTypeDefaults()
        {
            Assert.NotNull(Family.Create().Members); // the test is only valid if the default value is non-null
            Assert.Null(Family.Create(members: null).Members);
            Assert.Null(Family.Create().WithMembers((ImmutableSortedSet<Person>)null).Members);
        }

        [Fact]
        public void DefaultValuesCanBeOverriddenWithOtherValue()
        {
            Assert.NotNull(Family.Create().Members); // the test is only valid if the default value is non-null

            var otherMembers = ImmutableSortedSet.Create(Person.Create("bill"));
            Assert.Same(otherMembers, Family.Create(members: otherMembers).Members);
            Assert.Same(otherMembers, Family.Create().WithMembers(otherMembers).Members);
        }

        [Fact]
        public void ImmutableCollection()
        {
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
        public void CollectionsAlternateMutationMethods()
        {
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
