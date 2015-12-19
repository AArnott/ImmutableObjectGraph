namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    [GenerateImmutable]
    partial class ReqAndHierL1
    {
        readonly string l1Field1;
        [Required]
        readonly string l1Field2;
    }

    [GenerateImmutable]
    partial class ReqAndHierL2 : ReqAndHierL1
    {
        readonly string l2Field1;
        [Required]
        readonly string l2Field2;
    }
}
