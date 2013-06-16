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

		/// <summary>Gets an equatable comparer that compares all properties (and possibly descendents) between two instances.</summary>
		public static IEqualityComparer<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> ByValue<TPropertiesEnum, TDiffGram>(bool deep) {
			return deep ? ValueEqualityComparer<TPropertiesEnum, TDiffGram>.Deep : ValueEqualityComparer<TPropertiesEnum, TDiffGram>.Shallow;
		}

		/// <summary>An equatable and sorting comparer that considers only the persistent identity of a pair of values.</summary>
		private class IdentityEqualityComparer : IEqualityComparer<IRecursiveType> {
			internal static readonly IEqualityComparer<IRecursiveType> Default = new IdentityEqualityComparer();
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

		private class ValueEqualityComparer<TPropertiesEnum, TDiffGram> : IEqualityComparer<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> {
			internal static readonly IEqualityComparer<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> Shallow = new ValueEqualityComparer<TPropertiesEnum, TDiffGram>(false);

			internal static readonly IEqualityComparer<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> Deep = new ValueEqualityComparer<TPropertiesEnum, TDiffGram>(true);

			private bool includeRecursiveChildren;

			private ValueEqualityComparer(bool includeRecursiveChildren) {
				this.includeRecursiveChildren = includeRecursiveChildren;
			}

			public bool Equals(IRecursiveDiffingType<TPropertiesEnum, TDiffGram> x, IRecursiveDiffingType<TPropertiesEnum, TDiffGram> y) {
				if (x == null && y == null) {
					return true;
				}

				if (x == null ^ y == null) {
					return false;
				}

				if (this.includeRecursiveChildren) {
					throw new System.NotImplementedException();
				}

				return x.Equals(x.DiffProperties(y), default(TPropertiesEnum));
			}

			public int GetHashCode(IRecursiveDiffingType<TPropertiesEnum, TDiffGram> obj) {
				return obj.Identity.GetHashCode();
			}
		}
	}
}
