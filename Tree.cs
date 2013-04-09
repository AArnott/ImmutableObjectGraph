using System;

namespace ImmutableObjectGraph
{
	public class Tree : ICloneable {
		public string Name { get; private set; }

		public Tree(string name) {
			Name = name;
		}

		public object Clone() {
			return this;
		}
	}
}