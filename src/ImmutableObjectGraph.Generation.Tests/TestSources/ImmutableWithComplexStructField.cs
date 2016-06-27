namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    [GenerateImmutable]
    partial class ImmutableWithComplexStructField
    {
        readonly SomeStructWithMultipleFields someStructField;
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

        ////public static bool operator ==(SomeStructWithMultipleFields one, SomeStructWithMultipleFields two)
        ////{
        ////    return one.Field1 == two.Field1 && one.Field2 == two.Field2;
        ////}

        ////public static bool operator !=(SomeStructWithMultipleFields one, SomeStructWithMultipleFields two)
        ////{
        ////    return !(one == two);
        ////}
    }
}
