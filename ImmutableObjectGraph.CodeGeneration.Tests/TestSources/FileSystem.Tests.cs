namespace ImmutableObjectGraph.CodeGeneration.Tests.TestSources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class FileSystem
    {
        [Fact]
        public void Create()
        {
            FileSystemFile file = FileSystemFile.Create("a");
            Assert.NotNull(file);
            Assert.Equal("a", file.PathSegment);
        }
    }
}
