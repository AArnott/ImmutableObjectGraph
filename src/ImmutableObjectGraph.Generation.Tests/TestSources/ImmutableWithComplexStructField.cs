namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    using System;

    [GenerateImmutable(GenerateBuilder = true)]
    partial class ImmutableWithComplexStructField
    {
        readonly SomeStructWithMultipleFields someStructField;

        readonly SomeStructWithMultipleFieldsAndOperator someStructFieldWithOperator;

        readonly SomeGenericStructWithOperator<object> someGenericStructFieldWithOperator;
    }

    struct SomeStructWithMultipleFields
    {
        internal SomeStructWithMultipleFields(int field1, int field2)
        {
            this.Field1 = field1;
            this.Field2 = field2;
        }

        internal int Field1 { get; }

        internal int Field2 { get; }
    }

    struct SomeStructWithMultipleFieldsAndOperator
    {
        internal SomeStructWithMultipleFieldsAndOperator(int field1, int field2)
        {
            this.Field1 = field1;
            this.Field2 = field2;
        }

        internal int Field1 { get; }

        internal int Field2 { get; }

        public static bool operator ==(SomeStructWithMultipleFieldsAndOperator one, SomeStructWithMultipleFieldsAndOperator two)
        {
            return one.Field1 == two.Field1 && one.Field2 == two.Field2;
        }

        public static bool operator !=(SomeStructWithMultipleFieldsAndOperator one, SomeStructWithMultipleFieldsAndOperator two)
        {
            return !(one == two);
        }

        public override bool Equals(object obj)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    struct SomeGenericStructWithOperator<T> where T : class
    {
        internal SomeGenericStructWithOperator(T field1)
        {
            Field1 = field1;
        }

        internal T Field1 { get; }

        public static bool operator ==(SomeGenericStructWithOperator<T> one, SomeGenericStructWithOperator<T> two)
        {
            return one.Field1 == two.Field1;
        }

        public static bool operator !=(SomeGenericStructWithOperator<T> one, SomeGenericStructWithOperator<T> two)
        {
            return !(one == two);
        }

        public override bool Equals(object obj)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
