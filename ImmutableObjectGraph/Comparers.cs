namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public static class Comparers {
		public static IEqualityComparer<IRecursiveType> Identity {
			get { return IdentityEqualityComparer.Default; }
		}

		public static IEqualityComparer<ParentedRecursiveType<TRecursiveParent, TRecursiveType>> Parented<TRecursiveParent, TRecursiveType>()
			where TRecursiveType : class, IRecursiveType
			where TRecursiveParent : class, IRecursiveParent {
			return ParentedIdentityEqualityComparer<TRecursiveParent, TRecursiveType>.Default;
		}

		/// <summary>An equatable and sorting comparer that considers only the persistent identity of a pair of values.</summary>
		private class IdentityEqualityComparer : IEqualityComparer<IRecursiveType> {
			internal static readonly System.Collections.Generic.IEqualityComparer<IRecursiveType> Default = new IdentityEqualityComparer();
			private IdentityEqualityComparer() {
			}

			public bool Equals(IRecursiveType x, IRecursiveType y) {
				return x.Identity == y.Identity;
			}

			public int GetHashCode(IRecursiveType obj) {
				return obj.Identity.GetHashCode();
			}
		}

		private class ParentedIdentityEqualityComparer<TRecursiveParent, TRecursiveType> : IEqualityComparer<ParentedRecursiveType<TRecursiveParent, TRecursiveType>>
			where TRecursiveType : class, IRecursiveType
			where TRecursiveParent : class, IRecursiveParent {
			internal static readonly IEqualityComparer<ParentedRecursiveType<TRecursiveParent, TRecursiveType>> Default = new ParentedIdentityEqualityComparer<TRecursiveParent, TRecursiveType>();

			private ParentedIdentityEqualityComparer() {
			}

			public bool Equals(ParentedRecursiveType<TRecursiveParent, TRecursiveType> x, ParentedRecursiveType<TRecursiveParent, TRecursiveType> y) {
				return x.Value.Identity == y.Value.Identity;
			}

			public int GetHashCode(ParentedRecursiveType<TRecursiveParent, TRecursiveType> obj) {
				return obj.Value.Identity;
			}
		}
	}
}
