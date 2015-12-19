using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ImmutableObjectGraph.Generation.Tests.TestSources
{
    public class SealedTests
    {
        [Fact]
        public void SealedCreateBuilder()
        {
            Sealed.CreateBuilder();
        }

        [Fact]
        public void SealedWithBaseCreateBuilder()
        {
            Sealed_WithBase.CreateBuilder();
        }
    }
}
