namespace ImmutableObjectGraph.Adapters {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Validation;

	public class ImmutableListRootAdapter<TUnrooted, TRooted, TRoot> : IImmutableList<TRooted>, IImmutableCollectionAdapter<TUnrooted>
		where TRooted : struct
		where TUnrooted : class
		where TRoot : class {
		private readonly IImmutableList<TUnrooted> underlyingCollection;
		private readonly Func<TUnrooted, TRoot, TRooted> root;
		private readonly Func<TRooted, TUnrooted> unroot;
		private readonly TRoot rootObject;

		internal ImmutableListRootAdapter(IImmutableList<TUnrooted> underlyingCollection, Func<TUnrooted, TRoot, TRooted> toRooted, Func<TRooted, TUnrooted> toUnrooted, TRoot rootObject) {
			this.underlyingCollection = underlyingCollection;
			this.root = toRooted;
			this.unroot = toUnrooted;
			this.rootObject = rootObject;
		}

		public TRooted this[int index] {
			get { throw new NotImplementedException(); }
		}

		public IImmutableList<TUnrooted> UnderlyingCollection {
			get { return this.underlyingCollection; }
		}

		public int Count {
			get { return this.underlyingCollection.Count; }
		}

		IReadOnlyCollection<TUnrooted> IImmutableCollectionAdapter<TUnrooted>.UnderlyingCollection {
			get { return this.underlyingCollection; }
		}

		public IImmutableList<TRooted> Add(TRooted value) {
			return this.Wrap(this.underlyingCollection.Add(this.unroot(value)));
		}

		public IImmutableList<TRooted> Clear() {
			return this.Wrap(this.underlyingCollection.Clear());
		}

		public bool Contains(TRooted value) {
			return this.underlyingCollection.Contains(this.unroot(value));
		}

		public IImmutableList<TRooted> Remove(TRooted value) {
			return this.Wrap(this.underlyingCollection.Remove(this.unroot(value)));
		}

		public IImmutableList<TRooted> AddRange(IEnumerable<TRooted> items) {
			return this.Wrap(this.underlyingCollection.AddRange(items.Select(this.unroot)));
		}

		public int IndexOf(TRooted item, int index, int count, IEqualityComparer<TRooted> equalityComparer) {
			if (equalityComparer != null && equalityComparer != EqualityComparer<TRooted>.Default) {
				throw new NotSupportedException();
			}

			return this.underlyingCollection.IndexOf(this.unroot(item), index, count);
		}

		public IImmutableList<TRooted> Insert(int index, TRooted element) {
			return this.Wrap(this.underlyingCollection.Insert(index, this.unroot(element)));
		}

		public IImmutableList<TRooted> InsertRange(int index, IEnumerable<TRooted> items) {
			return this.Wrap(this.underlyingCollection.InsertRange(index, items.Select(this.unroot)));
		}

		public int LastIndexOf(TRooted item, int index, int count, IEqualityComparer<TRooted> equalityComparer) {
			if (equalityComparer != null && equalityComparer != EqualityComparer<TRooted>.Default) {
				throw new NotSupportedException();
			}

			return this.underlyingCollection.LastIndexOf(this.unroot(item), index, count);
		}

		public IImmutableList<TRooted> Remove(TRooted value, IEqualityComparer<TRooted> equalityComparer) {
			if (equalityComparer != null && equalityComparer != EqualityComparer<TRooted>.Default) {
				throw new NotSupportedException();
			}

			return this.Wrap(this.underlyingCollection.Remove(this.unroot(value)));
		}

		public IImmutableList<TRooted> RemoveAll(Predicate<TRooted> match) {
			return this.Wrap(this.underlyingCollection.RemoveAll(unrooted => match(this.root(unrooted, this.rootObject))));
		}

		public IImmutableList<TRooted> RemoveAt(int index) {
			return this.Wrap(this.underlyingCollection.RemoveAt(index));
		}

		public IImmutableList<TRooted> RemoveRange(int index, int count) {
			return this.Wrap(this.underlyingCollection.RemoveRange(index, count));
		}

		public IImmutableList<TRooted> RemoveRange(IEnumerable<TRooted> items, IEqualityComparer<TRooted> equalityComparer) {
			if (equalityComparer != null && equalityComparer != EqualityComparer<TRooted>.Default) {
				throw new NotSupportedException();
			}

			return this.Wrap(this.underlyingCollection.RemoveRange(items.Select(this.unroot)));
		}

		public IImmutableList<TRooted> Replace(TRooted oldValue, TRooted newValue, IEqualityComparer<TRooted> equalityComparer) {
			if (equalityComparer != null && equalityComparer != EqualityComparer<TRooted>.Default) {
				throw new NotSupportedException();
			}

			return this.Wrap(this.underlyingCollection.Replace(this.unroot(oldValue), this.unroot(newValue)));
		}

		public IImmutableList<TRooted> SetItem(int index, TRooted value) {
			return this.Wrap(this.underlyingCollection.SetItem(index, this.unroot(value)));
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

		private ImmutableListRootAdapter<TUnrooted, TRooted, TRoot> Wrap(IImmutableList<TUnrooted> underlyingCollection) {
			return this.underlyingCollection == underlyingCollection
				? this
				: new ImmutableListRootAdapter<TUnrooted, TRooted, TRoot>(underlyingCollection, this.root, this.unroot, this.rootObject);
		}
	}
}
