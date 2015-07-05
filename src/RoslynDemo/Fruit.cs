namespace RoslynDemo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ImmutableObjectGraph.CodeGeneration;

    [GenerateImmutable]
    partial class Fruit
    {
        readonly string color;
        readonly int skinThickness;
    }
}
