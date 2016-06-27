namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class ImmutableWithComplexStructFieldTests
    {
        [Fact]
        public void AssignVariousValues()
        {
            var originalStruct = new SomeStructWithMultipleFields(5, 0);
            var structWithModifiedSecondField = new SomeStructWithMultipleFields(5, 1);
            var v1 = ImmutableWithComplexStructField.Create(someStructField: originalStruct);
            var v2 = v1.With(someStructField: structWithModifiedSecondField);
            Assert.Equal(structWithModifiedSecondField.Field2, v2.SomeStructField.Field2);
        }
    }
}
