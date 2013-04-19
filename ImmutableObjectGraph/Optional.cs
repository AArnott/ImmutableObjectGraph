namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public static class Optional {
		public static Optional<T> For<T>(T value) {
			return value;
		}
	}
}
