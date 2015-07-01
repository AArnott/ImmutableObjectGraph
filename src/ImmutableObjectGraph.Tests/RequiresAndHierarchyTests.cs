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

		[Fact]
		public void RequiredFieldsAppearInMultipleRelatedTypes() {
			ReqAndHierL2 value = ReqAndHierL2.Create("l1f2Value", "l2f2Value");
			Assert.Equal("l1f2Value", value.L1Field2);
			Assert.Equal("l2f2Value", value.L2Field2);
			Assert.Null(value.L1Field1);
			Assert.Null(value.L2Field1);
		}
	}
}
