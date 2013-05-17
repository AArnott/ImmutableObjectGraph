namespace ImmutableObjectGraph {
	using ImmutableObjectGraph.Adapters;
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public static class Adapter {
		public static ImmutableSetRootAdapter<TUnrooted, TRooted, TRoot> Create<TUnrooted, TRooted, TRoot>(IImmutableSet<TUnrooted> underlyingCollection, Func<TUnrooted, TRoot, TRooted> toRooted, Func<TRooted, TUnrooted> toUnrooted, TRoot rootObject)
			where TRooted : struct
			where TUnrooted : class
			where TRoot : class {
				return new ImmutableSetRootAdapter<TUnrooted, TRooted, TRoot>(underlyingCollection, toRooted, toUnrooted, rootObject);
		}
	}
}
