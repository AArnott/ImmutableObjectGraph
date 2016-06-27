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
    }
}
