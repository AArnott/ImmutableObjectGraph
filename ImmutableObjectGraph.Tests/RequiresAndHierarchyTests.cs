namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using Xunit;

	public class RequiresAndHierarchyTests {
		[Fact]
		public void RequiredFieldNotDeclaredFirst() {
			ReqAndHierL1 value = ReqAndHierL1.Create("value2");
			Assert.Null(value.L1Field1);
			Assert.Equal("value2", value.L1Field2);
		}
	}
}
