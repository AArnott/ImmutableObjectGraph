namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using Xunit;

	public class OptionalTests {
		private static readonly Optional<string> defaultOptional;
		private static readonly Optional<string> optionalWithNull = Optional.For<string>(null);
		private static readonly Optional<string> optionalWithNonNull = Optional.For(string.Empty);

		[Fact]
		public void DefaultInstance() {
			Optional<string> defaultValue = default(Optional<string>);
			Assert.False(defaultValue.IsDefined);
			Assert.Null(defaultValue.Value);
		}

		[Fact]
		public void ImplicitOperator() {
			Optional<string> optional = null;
			Assert.True(optional.IsDefined);
			Assert.Null(optional.Value);

			optional = "a";
			Assert.True(optional.IsDefined);
			Assert.Equal("a", optional.Value);
		}

		[Fact]
		public void GetValueOrDefault() {
			Assert.Equal(null, defaultOptional.GetValueOrDefault(null));
			Assert.Equal("a", defaultOptional.GetValueOrDefault("a"));

			Assert.Equal(null, optionalWithNull.GetValueOrDefault(null));
			Assert.Equal(null, optionalWithNull.GetValueOrDefault("a"));

			Assert.Equal(string.Empty, optionalWithNonNull.GetValueOrDefault(null));
			Assert.Equal(string.Empty, optionalWithNonNull.GetValueOrDefault("a"));
		}

		[Fact]
		public void EqualsTest() {
			Assert.Equal(defaultOptional, defaultOptional);
			Assert.Equal(optionalWithNull, optionalWithNull);
			Assert.Equal(optionalWithNonNull, optionalWithNonNull);
			Assert.NotEqual(defaultOptional, optionalWithNull);
			Assert.NotEqual(defaultOptional, optionalWithNonNull);
			Assert.NotEqual(optionalWithNull, optionalWithNonNull);
		}

		[Fact]
		public void ConcreteOptionalFactoryMethod() {
			Optional<string> value = Optional.For("");
			Assert.True(value.IsDefined);
			Assert.Equal("", value.Value);
		}
	}
}
