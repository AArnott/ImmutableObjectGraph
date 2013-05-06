namespace ImmutableObjectGraph.Adapters {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public class ImmutableSetRootAdapter<TUnrooted, TRooted, TRoot> : IImmutableSet<TRooted>, IImmutableCollectionAdapter<TUnrooted>
		where TRooted : struct
		where TUnrooted : class
		where TRoot : class {
		private readonly IImmutableSet<TUnrooted> underlyingCollection;
		private readonly Func<TUnrooted, TRoot, TRooted> root;
		private readonly Func<TRooted, TUnrooted> unroot;
		private readonly TRoot rootObject;

		internal ImmutableSetRootAdapter(IImmutableSet<TUnrooted> underlyingCollection, Func<TUnrooted, TRoot, TRooted> toRooted, Func<TRooted, TUnrooted> toUnrooted, TRoot rootObject) {
			this.underlyingCollection = underlyingCollection;
			this.root = toRooted;
			this.unroot = toUnrooted;
			this.rootObject = rootObject;
		}

		public IImmutableSet<TUnrooted> UnderlyingCollection {
			get { return this.underlyingCollection; }
		}

		IReadOnlyCollection<TUnrooted> IImmutableCollectionAdapter<TUnrooted>.UnderlyingCollection {
			get { return this.underlyingCollection; }
		}

		public IImmutableSet<TRooted> Add(TRooted value) {
			return this.Wrap(this.underlyingCollection.Add(this.unroot(value)));
		}

		public IImmutableSet<TRooted> Clear() {
			return this.Wrap(this.underlyingCollection.Clear());
		}

		public bool Contains(TRooted value) {
			return this.underlyingCollection.Contains(this.unroot(value));
		}

		public IImmutableSet<TRooted> Except(IEnumerable<TRooted> other) {
			return this.Wrap(this.underlyingCollection.Except(other.Select(this.unroot)));
		}

		public IImmutableSet<TRooted> Intersect(IEnumerable<TRooted> other) {
			return this.Wrap(this.underlyingCollection.Intersect(other.Select(this.unroot)));
		}

		public bool IsProperSubsetOf(IEnumerable<TRooted> other) {
			return this.underlyingCollection.IsProperSubsetOf(other.Select(unroot));
		}

		public bool IsProperSupersetOf(IEnumerable<TRooted> other) {
			return this.underlyingCollection.IsProperSupersetOf(other.Select(unroot));
		}

		public bool IsSubsetOf(IEnumerable<TRooted> other) {
			return this.underlyingCollection.IsSubsetOf(other.Select(unroot));
		}

		public bool IsSupersetOf(IEnumerable<TRooted> other) {
			return this.underlyingCollection.IsSupersetOf(other.Select(unroot));
		}

		public bool Overlaps(IEnumerable<TRooted> other) {
			return this.underlyingCollection.Overlaps(other.Select(unroot));
		}

		public IImmutableSet<TRooted> Remove(TRooted value) {
			return this.Wrap(this.underlyingCollection.Remove(this.unroot(value)));
		}

		public bool SetEquals(IEnumerable<TRooted> other) {
			return this.underlyingCollection.SetEquals(other.Select(unroot));
		}

		public IImmutableSet<TRooted> SymmetricExcept(IEnumerable<TRooted> other) {
			return this.Wrap(this.underlyingCollection.SymmetricExcept(other.Select(this.unroot)));
		}

		public IImmutableSet<TRooted> Union(IEnumerable<TRooted> other) {
			return this.Wrap(this.underlyingCollection.Union(other.Select(this.unroot)));
		}

		public int Count {
			get { return this.underlyingCollection.Count; }
		}

		public IEnumerator<TRooted> GetEnumerator() {
			foreach (var element in this.underlyingCollection) {
				yield return this.root(element, this.rootObject);
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			foreach (var element in this.underlyingCollection) {
				yield return this.root(element, this.rootObject);
			}
		}

		private ImmutableSetRootAdapter<TUnrooted, TRooted, TRoot> Wrap(IImmutableSet<TUnrooted> underlyingCollection) {
			return this.underlyingCollection == underlyingCollection
				? this
				: new ImmutableSetRootAdapter<TUnrooted, TRooted, TRoot>(underlyingCollection, this.root, this.unroot, this.rootObject);
		}
	}
}
