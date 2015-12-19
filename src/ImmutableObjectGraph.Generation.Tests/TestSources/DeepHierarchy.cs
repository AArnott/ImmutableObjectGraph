namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    [GenerateImmutable(GenerateBuilder = true, DefineInterface = true)]
    partial class A
    {
        readonly int field1;
    }

    [GenerateImmutable(GenerateBuilder = true, DefineInterface = true)]
    partial class B : A
    {
        readonly int field2;
    }

    [GenerateImmutable(GenerateBuilder = true, DefineInterface = true)]
    partial class C1 : B
    {
        readonly int field3;
    }

    [GenerateImmutable(GenerateBuilder = true, DefineInterface = true)]
    partial class C2 : B
    {
        readonly int field3;
    }
}