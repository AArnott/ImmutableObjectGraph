namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Xunit;

	public class EmptyTests {
		[Fact]
		public void EmptyConstruction() {
			var empty= Empty.Create();
			Assert.NotNull(empty);
		}
	}
}
