namespace ImmutableObjectGraph.Adapters {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public interface IImmutableCollectionAdapter<T> {
		IReadOnlyCollection<T> UnderlyingCollection { get; }
	}
}
