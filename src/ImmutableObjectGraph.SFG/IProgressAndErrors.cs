namespace ImmutableObjectGraph.SFG
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IProgressAndErrors
    {
        void Error(string message, uint line, uint column);

        void Warning(string message, uint line, uint column);

        void Report(uint progress, uint total);
    }
}
