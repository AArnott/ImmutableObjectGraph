namespace ImmutableObjectGraph
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;
    using IdentityFieldType = System.UInt32;

    public static class RecursiveTypeExtensions
    {
        /// <summary>
        /// The maximum number of steps allowable for a search to be done among this node's children
        /// before a faster lookup table will be built.
        /// </summary>
        public const int InefficiencyLoadThreshold = 16;

        /// <summary>Checks whether an object with the specified identity is among this object's descendents.</summary>
        public static bool HasDescendent(this IRecursiveParent parent, IdentityFieldType identity)
        {
            Requires.NotNull(parent, "parent");

            IRecursiveType result;
            return parent.TryFind(identity, out result) && result != parent;
        }

        /// <summary>Checks whether a given object is among this object's descendents.</summary>
        public static bool HasDescendent(this IRecursiveParent parent, IRecursiveType possibleDescendent)
        {
            Requires.NotNull(parent, "parent");
            Requires.NotNull(possibleDescendent, "possibleDescendent");

            return HasDescendent(parent, possibleDescendent.Identity);
        }

        public static bool TryFind<TRecursiveParent, TRecursiveType>(this TRecursiveParent parent, IdentityFieldType identity, out TRecursiveType value)
                where TRecursiveParent : class, IRecursiveParent, TRecursiveType
                where TRecursiveType : class, IRecursiveType
        {
            Requires.NotNullAllowStructs(parent, "parent");

            if (parent.Identity.Equals(identity))
            {
                value = parent;
                return true;
            }

            var parentWithLookup = parent as IRecursiveParentWithLookupTable<TRecursiveType>;
            if (parentWithLookup != null)
            {
                KeyValuePair<IRecursiveType, IdentityFieldType> lookupValue;
                if (parentWithLookup.TryLookup(identity, out lookupValue))
                {
                    value = (TRecursiveType)lookupValue.Key;
                    return lookupValue.Key != null;
                }
            }

            // No lookup table (or a failed lookup) means we have to exhaustively search each child and its descendents.
            foreach (var child in parent.Children)
            {
                var recursiveChild = child as TRecursiveParent;
                if (recursiveChild != null)
                {
                    if (recursiveChild.TryFind(identity, out value))
                    {
                        return true;
                    }
                }
                else
                {
                    if (child.Identity.Equals(identity))
                    {
                        value = (TRecursiveType)child;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        public static bool TryFindImmediateChild<TRecursiveParent, TRecursiveType>(this TRecursiveParent parent, IdentityFieldType identity, out TRecursiveType value)
            where TRecursiveParent : class, IRecursiveParent, TRecursiveType
            where TRecursiveType : class, IRecursiveType
        {
            Requires.NotNullAllowStructs(parent, "parent");

            var parentWithLookup = parent as IRecursiveParentWithLookupTable<TRecursiveType>;
            if (parentWithLookup != null)
            {
                KeyValuePair<IRecursiveType, IdentityFieldType> lookupValue;
                if (parentWithLookup.TryLookup(identity, out lookupValue))
                {
                    if (lookupValue.Value == parent.Identity)
                    {
                        value = (TRecursiveType)lookupValue.Key;
                        return lookupValue.Key != null;
                    }
                    else
                    {
                        // It isn't an immediate child. 
                        value = default(TRecursiveType);
                        return false;
                    }
                }
            }

            // No lookup table means we have to exhaustively search each child.
            foreach (var child in parent.Children)
            {
                if (child.Identity.Equals(identity))
                {
                    value = (TRecursiveType)child;
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Tries to lookup a descendent node by its identity in a fast lookup table.
        /// </summary>
        /// <param name="parent">The parent to search.</param>
        /// <param name="identity">The identity of the descendent node to find.</param>
        /// <param name="result">Receives a reference to the sought object and the identity of its immediate parent, if the lookup table exists and the entry is found.</param>
        /// <returns><c>true</c> if the lookup table exists; <c>false</c> otherwise.</returns>
        /// <remarks>
        /// Note that a return value of <c>false</c> does not mean a matching descendent does not exist.
        /// It merely means that no fast lookup table has been initialized.
        /// If the return value is <c>true</c>, then the lookup table exists and the <paramref name="result"/>
        /// will either be empty or non-empty based on the presence of the descendent.
        /// </remarks>
        public static bool TryLookup<TRecursiveType>(this IRecursiveParentWithLookupTable<TRecursiveType> parent, IdentityFieldType identity, out KeyValuePair<IRecursiveType, IdentityFieldType> result)
            where TRecursiveType : IRecursiveType
        {
            if (parent.LookupTable != null)
            {
                KeyValuePair<TRecursiveType, IdentityFieldType> typedResult;
                parent.LookupTable.TryGetValue(identity, out typedResult);
                result = new KeyValuePair<IRecursiveType, IdentityFieldType>(typedResult.Key, typedResult.Value);
                return true;
            }

            result = default(KeyValuePair<IRecursiveType, IdentityFieldType>);
            return false;
        }

        public static TRecursiveType Find<TRecursiveParent, TRecursiveType>(this TRecursiveParent parent, IdentityFieldType identity)
            where TRecursiveParent : class, IRecursiveParent, TRecursiveType
            where TRecursiveType : class, IRecursiveType
        {
            Requires.NotNullAllowStructs(parent, "parent");

            TRecursiveType result;
            if (parent.TryFind(identity, out result))
            {
                return result;
            }

            throw new KeyNotFoundException();
        }

        /// <summary>Gets the recursive parent of the specified value, or <c>null</c> if none could be found.</summary>
        public static ParentedRecursiveType<TRecursiveParent, TRecursiveType> GetParentedNode<TRecursiveParent, TRecursiveType>(this TRecursiveParent parent, IdentityFieldType identity)
            where TRecursiveParent : class, TRecursiveType, IRecursiveParent<TRecursiveType>
            where TRecursiveType : class, IRecursiveType
        {
            Requires.NotNull(parent, "parent");

            if (parent.Identity == identity)
            {
                return new ParentedRecursiveType<TRecursiveParent, TRecursiveType>(parent, null);
            }

            var fastLookup = parent as IRecursiveParentWithLookupTable<TRecursiveType>;
            KeyValuePair<IRecursiveType, uint> nodeLookupResult;
            if (fastLookup != null && fastLookup.TryLookup(identity, out nodeLookupResult))
            {
                if (nodeLookupResult.Key != null)
                {
                    TRecursiveType parentReference;
                    Assumes.True(TryFind(parent, nodeLookupResult.Value, out parentReference));
                    return new ParentedRecursiveType<TRecursiveParent, TRecursiveType>((TRecursiveType)nodeLookupResult.Key, (TRecursiveParent)parentReference);
                }
            }
            else
            {
                // No lookup table means we have to aggressively search each child.
                foreach (var child in parent.Children)
                {
                    if (child.Identity.Equals(identity))
                    {
                        return new ParentedRecursiveType<TRecursiveParent, TRecursiveType>(child, parent);
                    }

                    var recursiveChild = child as TRecursiveParent;
                    if (recursiveChild != null)
                    {
                        var childResult = recursiveChild.GetParentedNode(identity);
                        if (childResult.Value != null)
                        {
                            return new ParentedRecursiveType<TRecursiveParent, TRecursiveType>((TRecursiveType)childResult.Value, (TRecursiveParent)childResult.Parent);
                        }
                    }
                }
            }

            return default(ParentedRecursiveType<TRecursiveParent, TRecursiveType>);
        }

        /// <summary>Gets the recursive parent of the specified value, or <c>null</c> if none could be found.</summary>
        public static TRecursiveParent GetParent<TRecursiveParent, TRecursiveType>(this TRecursiveParent parent, TRecursiveType descendent)
            where TRecursiveParent : class, TRecursiveType, IRecursiveParent<TRecursiveType>
            where TRecursiveType : class, IRecursiveType
        {
            return GetParentedNode<TRecursiveParent, TRecursiveType>(parent, descendent.Identity).Parent;
        }

        /// <summary>
        /// Returns a sequence starting with the given <paramref name="root"/>
        /// followed by its descendents in a depth-first search.
        /// </summary>
        /// <param name="root">The node at which to start enumeration.</param>
        /// <returns>A sequence of nodes beginning with <paramref name="root"/> and including all descendents.</returns>
        public static IEnumerable<TRecursiveType> GetSelfAndDescendents<TRecursiveType>(this IRecursiveParent<TRecursiveType> root)
            where TRecursiveType : IRecursiveType
        {
            yield return (TRecursiveType)root;

            var rootAsParent = root as IRecursiveParent<TRecursiveType>;
            if (rootAsParent != null && rootAsParent.Children != null)
            {
                foreach (TRecursiveType child in rootAsParent.Children)
                {
                    var childAsParent = child as IRecursiveParent<TRecursiveType>;
                    if (childAsParent != null)
                    {
                        foreach (var descendent in childAsParent.GetSelfAndDescendents())
                        {
                            yield return descendent;
                        }
                    }
                    else
                    {
                        yield return child;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a sequence starting with the given <paramref name="root"/>
        /// followed by its descendents in a depth-first search.
        /// </summary>
        /// <param name="root">The node at which to start enumeration.</param>
        /// <returns>A sequence of nodes beginning with <paramref name="root"/> and including all descendents.</returns>
        public static IEnumerable<IRecursiveType> GetSelfAndDescendents(this IRecursiveType root)
        {

            var rootAsParent = root as IRecursiveParent<IRecursiveType>;
            if (rootAsParent != null)
            {
                return GetSelfAndDescendents(rootAsParent);
            }
            else
            {
                return new[] { root };
            }
        }

        /// <summary>
        /// Walks a project tree starting at a given node, using a breadth-first search pattern.
        /// </summary>
        /// <param name="root">The node at which to begin the search.</param>
        /// <returns>A sequence of the given node, and all its descendents.  The given node always comes first in the sequence.</returns>
        public static IEnumerable<TRecursiveType> GetSelfAndDescendentsBreadthFirst<TRecursiveType>(this IRecursiveParent<TRecursiveType> root)
            where TRecursiveType : IRecursiveType
        {
            var nodesToVisit = new Queue<TRecursiveType>();
            nodesToVisit.Enqueue((TRecursiveType)root);

            while (nodesToVisit.Count > 0)
            {
                var visiting = nodesToVisit.Dequeue();
                yield return visiting;

                var visitingAsParent = visiting as IRecursiveParent;
                if (visitingAsParent != null && visitingAsParent.Children != null)
                {
                    foreach (TRecursiveType child in visitingAsParent.Children)
                    {
                        nodesToVisit.Enqueue(child);
                    }
                }
            }
        }

        public static IEnumerable<ParentedRecursiveType<TRecursiveParent, TRecursiveType>> GetSelfAndDescendentsWithParents<TRecursiveParent, TRecursiveType>(this TRecursiveParent root, TRecursiveParent parent = default(TRecursiveParent))
            where TRecursiveParent : class, IRecursiveParent<TRecursiveType>
            where TRecursiveType : class, IRecursiveType
        {
            Requires.NotNull(root, "root");

            IRecursiveType rootAsRecursiveType = root;
            yield return new ParentedRecursiveType<TRecursiveParent, TRecursiveType>((TRecursiveType)rootAsRecursiveType, parent);

            var rootAsParent = root as TRecursiveParent;
            if (rootAsParent != null && rootAsParent.Children != null)
            {
                foreach (TRecursiveType child in rootAsParent.Children)
                {
                    var childAsParent = child as TRecursiveParent;
                    if (childAsParent != null)
                    {
                        foreach (var descendent in childAsParent.GetSelfAndDescendentsWithParents<TRecursiveParent, TRecursiveType>(rootAsParent))
                        {
                            yield return descendent;
                        }
                    }
                    else
                    {
                        yield return new ParentedRecursiveType<TRecursiveParent, TRecursiveType>(child, rootAsParent);
                    }
                }
            }
        }

        public static ImmutableStack<TRecursiveType> GetSpine<TRecursiveParent, TRecursiveType>(this TRecursiveParent self, IdentityFieldType descendent)
            where TRecursiveParent : class, TRecursiveType, IRecursiveParentWithLookupTable<TRecursiveType>
            where TRecursiveType : class, IRecursiveType
        {
            var emptySpine = ImmutableStack.Create<TRecursiveType>();
            if (self.Identity.Equals(descendent))
            {
                return emptySpine.Push(self);
            }

            if (self.LookupTable != null)
            {
                KeyValuePair<TRecursiveType, IdentityFieldType> lookupValue;
                if (self.LookupTable.TryGetValue(descendent, out lookupValue))
                {
                    // Awesome.  We know the node the caller is looking for is a descendent of this node.
                    // Now just string together all the nodes that connect this one with the sought one.
                    var spine = emptySpine;
                    do
                    {
                        spine = spine.Push(lookupValue.Key);
                    }
                    while (self.LookupTable.TryGetValue(lookupValue.Value, out lookupValue));
                    return spine.Push(self);
                }
            }
            else
            {
                // We don't have an efficient lookup table for this node.  Aggressively search every child.
                var spine = emptySpine;
                foreach (var child in self.Children)
                {
                    var recursiveChild = child as TRecursiveParent;
                    if (recursiveChild != null)
                    {
                        spine = recursiveChild.GetSpine<TRecursiveParent, TRecursiveType>(descendent);
                    }
                    else if (child.Identity.Equals(descendent))
                    {
                        spine = spine.Push(child);
                    }

                    if (!spine.IsEmpty)
                    {
                        return spine.Push(self);
                    }
                }
            }

            // The descendent is not in this sub-tree.
            return emptySpine;
        }

        public static ImmutableStack<TRecursiveType> GetSpine<TRecursiveParent, TRecursiveType>(this TRecursiveParent parent, TRecursiveType descendent)
            where TRecursiveParent : class, TRecursiveType, IRecursiveParentWithLookupTable<TRecursiveType>
            where TRecursiveType : class, IRecursiveType
        {
            return GetSpine<TRecursiveParent, TRecursiveType>(parent, descendent.Identity);
        }

        public static System.Collections.Immutable.ImmutableStack<TRecursiveType> ReplaceDescendent<TRecursiveParent, TRecursiveType>(this TRecursiveParent self, System.Collections.Immutable.ImmutableStack<TRecursiveType> spine, System.Collections.Immutable.ImmutableStack<TRecursiveType> replacementStackTip, bool spineIncludesDeletedElement)
            where TRecursiveParent : class, TRecursiveType, IRecursiveParentWithLookupTable<TRecursiveType>, IRecursiveParentWithChildReplacement<TRecursiveType>
            where TRecursiveType : class, IRecursiveType
        {
            Debug.Assert(self == spine.Peek());
            var remainingSpine = spine.Pop();
            if (remainingSpine.IsEmpty || (spineIncludesDeletedElement && remainingSpine.Pop().IsEmpty))
            {
                // self is the instance to be changed.
                return replacementStackTip;
            }

            System.Collections.Immutable.ImmutableStack<TRecursiveType> newChildSpine;
            var child = remainingSpine.Peek();
            var recursiveChild = child as TRecursiveParent;
            if (recursiveChild != null)
            {
                newChildSpine = recursiveChild.ReplaceDescendent(remainingSpine, replacementStackTip, spineIncludesDeletedElement);
            }
            else
            {
                Debug.Assert(remainingSpine.Pop().IsEmpty); // we should be at the tail of the stack, since we're at a leaf.
                Debug.Assert(self.Children.Contains(child));
                newChildSpine = replacementStackTip;
            }

            var newSelf = (TRecursiveParent)self.ReplaceChild(remainingSpine, newChildSpine);
            return newChildSpine.Push(newSelf);
        }

        public static IReadOnlyList<TDiffGram> ChangesSince<TPropertiesEnum, TDiffGram>(this IRecursiveDiffingType<TPropertiesEnum, TDiffGram> current, IRecursiveDiffingType<TPropertiesEnum, TDiffGram> priorVersion)
            where TPropertiesEnum : struct
            where TDiffGram : struct
        {
            Requires.NotNull(current, "current");
            Requires.NotNull(priorVersion, "priorVersion");

            if (current == priorVersion)
            {
                return System.Collections.Immutable.ImmutableList.Create<TDiffGram>();
            }

            if (priorVersion.Identity != current.Identity)
            {
                throw new System.ArgumentException("Not another version of the same node.", nameof(priorVersion));
            }

            var currentAsParent = current as IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>;
            var currentAsRecursiveType = (IRecursiveDiffingType<TPropertiesEnum, TDiffGram>)current;

            var before = new HashSet<ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
            var after = new HashSet<ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());

            var priorVersionAsParent = priorVersion as IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>;
            if (priorVersionAsParent != null)
            {
                before.UnionWith(priorVersionAsParent.GetSelfAndDescendentsWithParents<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
            }
            else
            {
                before.Add(priorVersion.WithParent());
            }

            if (currentAsParent != null)
            {
                after.UnionWith(currentAsParent.GetSelfAndDescendentsWithParents<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
            }
            else
            {
                after.Add(current.WithParent());
            }

            var added = new HashSet<ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
            var removed = new HashSet<ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
            var changed = new Dictionary<ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());

            var descendentsOfAddOrRemove = new HashSet<IRecursiveType>(Comparers.Identity);

            foreach (var fromBefore in before)
            {
                if (after.Contains(fromBefore))
                {
                    ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> fromAfter;
                    if (currentAsParent != null)
                    {
                        var parent = currentAsParent.GetParentedNode(fromBefore.Value.Identity);
                        fromAfter = new ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>(
                            (IRecursiveDiffingType<TPropertiesEnum, TDiffGram>)parent.Value,
                            (IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>)parent.Parent);
                    }
                    else
                    {
                        fromAfter = new ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>(
                            fromBefore.Value.Identity == current.Identity ? (IRecursiveDiffingType<TPropertiesEnum, TDiffGram>)current : null);
                    }

                    if (!object.ReferenceEquals(fromBefore.Value, fromAfter.Value) || fromBefore.Parent.Identity != fromAfter.Parent.Identity)
                    {
                        changed.Add(fromBefore, fromAfter);
                    }
                }
                else
                {
                    removed.Add(fromBefore);
                }
            }

            foreach (var fromAfter in after)
            {
                if (!before.Contains(fromAfter))
                {
                    added.Add(fromAfter);
                }
            }

            foreach (var topLevelOperation in added.Concat(removed))
            {
                descendentsOfAddOrRemove.UnionWith(topLevelOperation.Value.GetSelfAndDescendents().Skip(1));
            }

            var history = new List<TDiffGram>();
            history.AddRange(removed.Where(r => !descendentsOfAddOrRemove.Contains(r.Value)).Select(r => currentAsRecursiveType.Remove(r.Value)));

            foreach (var changedNode in changed)
            {
                var oldNode = changedNode.Key;
                var newNode = changedNode.Value;

                var diff = newNode.DiffProperties(oldNode);
                if (!currentAsRecursiveType.Equals(diff, default(TPropertiesEnum)))
                {
                    history.Add(currentAsRecursiveType.Change(oldNode.Value, newNode.Value, diff));
                }
            }

            history.AddRange(added.Where(a => !descendentsOfAddOrRemove.Contains(a.Value)).Select(a => currentAsRecursiveType.Add(a.Value)));

            return history;
        }

        public static TPropertiesEnum DiffProperties<TPropertiesEnum, TDiffGram>(this ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> self, ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> other)
            where TPropertiesEnum : struct
        {
            TPropertiesEnum changes = self.Value.DiffProperties(other.Value);
            if ((self.Parent == null ^ other.Parent == null) || (self.Parent != null && other.Parent != null && self.Parent.Identity != other.Parent.Identity))
            {
                changes = self.Value.Union(changes, self.Value.ParentProperty);
            }
            else if (self.Value != other.Value && self.Parent != null && other.Parent != null)
            {
                var selfParentOrdered = self.Parent as IRecursiveParentWithOrderedChildren;
                if (selfParentOrdered != null)
                {
                    var selfParentSorted = selfParentOrdered as IRecursiveParentWithSortedChildren;
                    if (selfParentSorted != null)
                    {
                        if (selfParentSorted.Compare(self.Value, other.Value) != 0)
                        {
                            // Calculate where the node was, and where it would go in the old tree.
                            var otherParentSorted = (IRecursiveParentWithSortedChildren)other.Parent;
                            int beforeIndex = otherParentSorted.IndexOf(other.Value);
                            int afterIndex = ~otherParentSorted.IndexOf(self.Value);

                            // If the indices are the same, the new one would come "just before" the old one.
                            // If the new index is just 1 greater than the old index, the new one would come "just after" the old one.
                            // In either of these cases, since the old one will be gone in the new tree, the position hasn't changed.
                            if (afterIndex != beforeIndex && afterIndex != beforeIndex + 1)
                            {
                                changes = self.Value.Union(changes, self.Value.PositionUnderParentProperty);
                            }
                        }
                    }
                    else
                    {
                        // Calculate whether items were reordered without leveraging a sorting comparer.
                        var otherParentOrdered = (IRecursiveParentWithOrderedChildren)other.Parent;
                        int beforeIndex = otherParentOrdered.IndexOf(other.Value);
                        int afterIndex = selfParentOrdered.IndexOf(self.Value);

                        // TODO: review (and add tests for) cases where items are inserted/removed from the parent,
                        // causing all other items after it to apparently shift in position. We probably don't want
                        // to consider that a change in PositionUnderParent, since the removal or add will take care of it.
                        if (afterIndex != beforeIndex)
                        {
                            changes = self.Value.Union(changes, self.Value.PositionUnderParentProperty);
                        }
                    }
                }
            }

            return changes;
        }

        public static class LookupTable<TRecursiveType, TRecursiveParent>
            where TRecursiveType : class, IRecursiveType
            where TRecursiveParent : class, TRecursiveType, IRecursiveParentWithLookupTable<TRecursiveType>
        {
            /// <summary>
            /// The value assigned to the lookup table when we have established we need one, but have not yet initialized it.
            /// </summary>
            public static readonly ImmutableDictionary<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>> LazySentinel =
                ImmutableDictionary.Create<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>>()
                    .Add(default(IdentityFieldType), new KeyValuePair<TRecursiveType, IdentityFieldType>());

            public struct InitializeLookupResult
            {
                public uint InefficiencyLoad { get; set; }

                public ImmutableDictionary<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>> LookupTable { get; set; }
            }

            public static InitializeLookupResult Initialize(TRecursiveParent parent, Optional<ImmutableDictionary<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>>> priorLookupTable = default(Optional<ImmutableDictionary<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>>>))
            {
                var result = new InitializeLookupResult();
                uint inefficiencyLoad = 1; // use local until we know final value since that's faster than field access.
                if (priorLookupTable.IsDefined && priorLookupTable.Value != null)
                {
                    result.LookupTable = priorLookupTable.Value;
                }
                else
                {
                    if (parent.Children != null)
                    {
                        if (parent.Children.Count >= InefficiencyLoadThreshold)
                        {
                            // The number of children alone are enough to put us over the threshold, skip enumeration.
                            inefficiencyLoad = InefficiencyLoadThreshold + 1;
                        }
                        else if (parent.Children.Count > 0)
                        {
                            foreach (var child in parent.Children)
                            {
                                var recursiveChild = child as TRecursiveParent;
                                inefficiencyLoad += recursiveChild?.InefficiencyLoad ?? 1;
                                if (inefficiencyLoad > InefficiencyLoadThreshold)
                                {
                                    break; // It's ok to under-estimate once we're above the threshold since any further would be a waste of time.
                                }
                            }
                        }
                    }

                    if (inefficiencyLoad > InefficiencyLoadThreshold)
                    {
                        inefficiencyLoad = 1;
                        result.LookupTable = LazySentinel;
                    }
                }

                result.InefficiencyLoad = inefficiencyLoad;
                ValidateInternalIntegrityDebugOnly(parent);

                return result;
            }

            /// <summary>
            /// Creates the lookup table that will contain all this node's children.
            /// </summary>
            /// <returns>The lookup table.</returns>
            public static ImmutableDictionary<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>> CreateLookupTable(TRecursiveParent parent)
            {
                var table = System.Collections.Immutable.ImmutableDictionary.Create<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>>().ToBuilder();
                ContributeDescendentsToLookupTable(parent, table);
                return table.ToImmutable();
            }

            /// <summary>
            /// Produces a fast lookup table based on an existing one, if this node has one, to account for an updated spine among its descendents.
            /// </summary>
            /// <param name="updatedSpine">
            /// The spine of this node's new descendents' instances that are created for this change.
            /// The head is an immediate child of the new instance for this node.
            /// The tail is the node that was added or replaced.
            /// </param>
            /// <param name="oldSpine">
            /// The spine of this node's descendents that have been changed in this delta.
            /// The head is an immediate child of this instance.
            /// The tail is the node that was removed or replaced.
            /// </param>
            /// <returns>An updated lookup table.</returns>
            public static ImmutableDictionary<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>> Fixup(TRecursiveParent self, ImmutableDeque<TRecursiveType> updatedSpine, ImmutableDeque<TRecursiveType> oldSpine)
            {
                if (self.LookupTable == null || self.LookupTable == LazySentinel)
                {
                    // We don't already have a lookup table to base this on, so leave it to the new instance to lazily construct.
                    return LazySentinel;
                }

                if ((updatedSpine.IsEmpty && oldSpine.IsEmpty) ||
                    (updatedSpine.Count > 1 && oldSpine.Count > 1 && System.Object.ReferenceEquals(updatedSpine.PeekHead(), oldSpine.PeekHead())))
                {
                    // No changes were actually made.
                    return self.LookupTable;
                }

                var LookupTable = self.LookupTable.ToBuilder();

                // Classify the kind of change that has just occurred.
                var oldSpineTail = oldSpine.PeekTail();
                var newSpineTail = updatedSpine.PeekTail();
                ChangeKind changeKind;
                bool childrenChanged = false;
                if (updatedSpine.Count == oldSpine.Count)
                {
                    changeKind = ChangeKind.Replaced;
                    var oldSpineTailRecursive = oldSpineTail as TRecursiveParent;
                    var newSpineTailRecursive = newSpineTail as TRecursiveParent;
                    if (oldSpineTailRecursive != null || newSpineTailRecursive != null)
                    {
                        // Children have changed if either before or after type didn't have a children property,
                        // or if both did, but the children actually changed.
                        childrenChanged = oldSpineTailRecursive == null || newSpineTailRecursive == null
                            || !ReferenceEquals(oldSpineTailRecursive.Children, newSpineTailRecursive.Children);
                    }
                }
                else if (updatedSpine.Count > oldSpine.Count)
                {
                    changeKind = ChangeKind.Added;
                }
                else // updatedSpine.Count < oldSpine.Count
                {
                    changeKind = ChangeKind.Removed;
                }

                // Trim the lookup table of any entries for nodes that have been removed from the tree.
                if (childrenChanged || changeKind == ChangeKind.Removed)
                {
                    // We need to remove all descendents of the old tail node.
                    LookupTable.RemoveRange(oldSpineTail.GetSelfAndDescendents().Select(n => n.Identity));
                }
                else if (changeKind == ChangeKind.Replaced && oldSpineTail.Identity != newSpineTail.Identity)
                {
                    // The identity of the node was changed during the replacement.  We must explicitly remove the old entry
                    // from our lookup table in this case.
                    LookupTable.Remove(oldSpineTail.Identity);

                    // We also need to update any immediate children of the old spine tail
                    // because the identity of their parent has changed.
                    var oldSpineTailRecursive = oldSpineTail as TRecursiveParent;
                    if (oldSpineTailRecursive != null)
                    {
                        foreach (var child in oldSpineTailRecursive.Children)
                        {
                            LookupTable[child.Identity] = new KeyValuePair<TRecursiveType, IdentityFieldType>(child, newSpineTail.Identity);
                        }
                    }
                }

                // Update our lookup table so that it includes (updated) entries for every member of the spine itself.
                TRecursiveType parent = self;
                foreach (var node in updatedSpine)
                {
                    // Remove and add rather than use the Set method, since the old and new node are equal (in identity) therefore the map class will
                    // assume no change is relevant and not apply the change.
                    LookupTable.Remove(node.Identity);
                    LookupTable.Add(node.Identity, new KeyValuePair<TRecursiveType, IdentityFieldType>(node, parent.Identity));
                    parent = node;
                }

                // There may be children on the added node that we should include.
                if (childrenChanged || changeKind == ChangeKind.Added)
                {
                    var recursiveParent = parent as TRecursiveParent;
                    if (recursiveParent != null)
                    {
                        ContributeDescendentsToLookupTable(recursiveParent, LookupTable);
                    }
                }

                return LookupTable.ToImmutable();
            }

            /// <summary>
            /// Validates this node and all its descendents <em>only in DEBUG builds</em>.
            /// </summary>
            [Conditional("DEBUG")]
            public static void ValidateInternalIntegrityDebugOnly(TRecursiveParent parent)
            {
                ValidateInternalIntegrity(parent);
            }

            /// <summary>
            /// Validates this node and all its descendents.
            /// </summary>
            public static void ValidateInternalIntegrity(TRecursiveParent parent)
            {
                // Each node id appears at most once.
                if (parent.Children?.Count > 0)
                {
                    var observedIdentities = new System.Collections.Generic.HashSet<IdentityFieldType>();
                    foreach (var node in parent.GetSelfAndDescendents())
                    {
                        if (!observedIdentities.Add(node.Identity))
                        {
                            throw new RecursiveChildNotUniqueException(node.Identity);
                        }
                    }
                }

                // The lookup table (if any) accurately describes the contents of this tree.
                if (parent.LookupTable != null && parent.LookupTable != LazySentinel)
                {
                    // The table should have one entry for every *descendent* of this node (not this node itself).
                    int expectedCount = parent.GetSelfAndDescendents().Count() - 1;
                    int actualCount = parent.LookupTable.Count;
                    Assumes.False(actualCount != expectedCount, "Expected {0} entries in lookup table but found {1}.", expectedCount, actualCount);

                    ValidateLookupTable(parent, parent.LookupTable);
                }
            }

            /// <summary>
            /// Adds this node's children (recursively) to the lookup table.
            /// </summary>
            /// <param name="seedLookupTable">The lookup table to add entries to.</param>
            /// <returns>The new lookup table.</returns>
            private static void ContributeDescendentsToLookupTable(TRecursiveParent parent, ImmutableDictionary<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>>.Builder seedLookupTable)
            {
                foreach (var child in parent.Children)
                {
                    seedLookupTable.Add(child.Identity, new KeyValuePair<TRecursiveType, IdentityFieldType>(child, parent.Identity));
                    var recursiveChild = child as TRecursiveParent;
                    if (recursiveChild != null)
                    {
                        ContributeDescendentsToLookupTable(recursiveChild, seedLookupTable);
                    }
                }
            }

            /// <summary>
            /// Validates that the contents of a lookup table are valid for all descendent nodes of this node.
            /// </summary>
            /// <param name="lookupTable">The lookup table being validated.</param>
            private static void ValidateLookupTable(TRecursiveParent parent, ImmutableDictionary<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>> lookupTable)
            {
                const string ErrorString = "Lookup table integrity failure.";

                foreach (var child in parent.Children)
                {
                    var entry = lookupTable[child.Identity];
                    Assumes.True(object.ReferenceEquals(entry.Key, child), ErrorString);
                    Assumes.True(entry.Value == parent.Identity, ErrorString);

                    var recursiveChild = child as TRecursiveParent;
                    if (recursiveChild != null)
                    {
                        ValidateLookupTable(recursiveChild, lookupTable);
                    }
                }
            }
        }

        private static ParentedRecursiveType<IRecursiveParent<TRecursiveType>, TRecursiveType> WithParent<TRecursiveType>(this TRecursiveType value, IRecursiveParent<TRecursiveType> parent = null)
            where TRecursiveType : IRecursiveType
        {
            return new ParentedRecursiveType<IRecursiveParent<TRecursiveType>, TRecursiveType>(value, parent);
        }

        public static void Write(this IRecursiveParent root, TextWriter writer)
        {
            Requires.NotNull(root, "root");
            Requires.NotNull(writer, "writer");
            const string Indent = "  ";

            writer.Write(root);

            writer.NewLine += Indent;
            foreach (var child in root.Children)
            {
                writer.WriteLine();
                var childAsParent = child as IRecursiveParent;
                if (childAsParent != null)
                {
                    Write(childAsParent, writer);
                }
                else
                {
                    writer.Write(child);
                }
            }

            writer.NewLine = writer.NewLine.Substring(0, writer.NewLine.Length - Indent.Length);
        }
    }
}
