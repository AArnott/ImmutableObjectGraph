namespace ImmutableObjectGraph.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    partial class ProjectTree
    {
        public bool Contains(int identity)
        {
			return this.Find(identity) != null;
        }
    }
}
