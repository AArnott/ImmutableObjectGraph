namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public enum ChangeKind {
		/// <summary>
		/// A node was added.
		/// </summary>
		Added,

		/// <summary>
		/// A node's own properties were changed.
		/// </summary>
		Replaced,

		/// <summary>
		/// A node was removed.
		/// </summary>
		Removed,
	}
}
