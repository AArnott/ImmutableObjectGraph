namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public static class ImmutableDeque {
		public static ImmutableDeque<T> Create<T>() {
			return ImmutableDeque<T>.Empty;
		}

		public static ImmutableDeque<T> Create<T>(T value) {
			return ImmutableDeque<T>.Empty.EnqueueBack(value);
		}

		public static ImmutableDeque<T> Create<T>(ImmutableStack<T> stack) {
			return new ImmutableDeque<T>(stack.ToImmutableList());
		}
	}
}
