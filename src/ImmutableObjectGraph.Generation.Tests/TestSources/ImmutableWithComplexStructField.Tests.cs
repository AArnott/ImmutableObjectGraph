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
        public void StructWithoutOperatorsAlwaysRecreatesObjectWithChangedValue()
        {
            var originalStruct = new SomeStructWithMultipleFields(5, 0);
            var structWithModifiedSecondField = new SomeStructWithMultipleFields(5, 1);
            var v1 = ImmutableWithComplexStructField.Create(someStructField: originalStruct);
            var v2 = v1.With(someStructField: structWithModifiedSecondField);
            Assert.Equal(structWithModifiedSecondField.Field2, v2.SomeStructField.Field2);
        }

        [Fact]
        public void StructWithoutOperatorsAlwaysRecreatesObjectWithSameValue()
        {
            var s1 = new SomeStructWithMultipleFields(1, 2);
            var v1 = ImmutableWithComplexStructField.Create(someStructField: s1);
            var v2 = v1.With(someStructField: s1);

            // The object should have been recreated since equality between the two struct values
            // cannot be determined without their operator defined.
            Assert.NotSame(v1, v2);
            Assert.Equal(s1.Field1, v2.SomeStructField.Field1);
        }

        [Fact]
        public void StructWithoutOperatorsAlwaysRecreatesObjectWithoutValue()
        {
            var s1 = new SomeStructWithMultipleFields(1, 2);
            var v1 = ImmutableWithComplexStructField.Create(someStructField: s1);
            var v2 = v1.With(); // omit the struct value altogether
            Assert.Same(v1, v2);
        }

        [Fact]
        public void StructWithOperatorsRecreatesObjectWithChangedValue()
        {
            var s12 = new SomeStructWithMultipleFieldsAndOperator(1, 2);
            var s13 = new SomeStructWithMultipleFieldsAndOperator(1, 3);
            var v1 = ImmutableWithComplexStructField.Create(someStructFieldWithOperator: s12);
            var v2 = v1.With(someStructFieldWithOperator: s13);
            Assert.NotSame(v1, v2);
            Assert.Equal(s13.Field2, v2.SomeStructFieldWithOperator.Field2);
        }

        [Fact]
        public void StructWithOperatorsRecyclesObjectWithSameValue()
        {
            var s12 = new SomeStructWithMultipleFieldsAndOperator(1, 2);
            var v1 = ImmutableWithComplexStructField.Create(someStructFieldWithOperator: s12);
            var v2 = v1.With(someStructFieldWithOperator: s12);
            Assert.Same(v1, v2);
        }

        [Fact]
        public void GenericStructWithOperatorsRecyclesObjectWithSameValue()
        {
            var v = new object();
            var s12 = new SomeGenericStructWithOperator<object>(v);
            var v1 = ImmutableWithComplexStructField.Create(someGenericStructFieldWithOperator: s12);
            var v2 = v1.With(someGenericStructFieldWithOperator: s12);
            Assert.Same(v1, v2);
        }
    }
}
