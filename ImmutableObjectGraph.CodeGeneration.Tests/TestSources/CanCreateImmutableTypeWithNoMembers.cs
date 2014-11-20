[ImmutableObjectGraph.CodeGeneration.GenerateImmutable]
partial class Empty { }

class User
{
    static void Test()
    {
        Empty e = Empty.Create();
    }
}
