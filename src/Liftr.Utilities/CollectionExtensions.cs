//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr
{
    /// <summary>
    /// A set of extensions around <see cref="System.Collections.Generic"/>.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// <para>
        /// NOTE: DO NOT use this method outside of the Contracts assembly. There is an identical method in Common.
        /// The reason this class exists is so that the Contracts assembly  does not take any dependencies on an
        /// external assembly (other than <see cref="System"/> and <c>mscorlib</c>).
        /// </para>
        /// Returns whether or not two different collections contain an identical count and
        /// composition of elements irrespective of the ordering of any individual element in either collection.
        /// </summary>
        /// <typeparam name="T">
        /// The type that all the elements in <paramref name="thisCollection"/> and <paramref name="otherCollection"/>
        /// inherit from or are an instance of.
        /// </typeparam>
        /// <param name="thisCollection">The first collection to compare. </param>
        /// <param name="otherCollection">The second collection to compare. </param>
        /// <param name="comparer">The comparer that will be used to check equivalence of instances of</param>
        /// <returns>
        /// true if <paramref name="thisCollection"/> and <paramref name="otherCollection"/> have:
        /// 1. The same size
        /// 2. Identical counts of any given element in the collection
        /// <para/>
        /// In all other cases, false.
        /// </returns>
        public static bool IsEquivalentTo<T>(this IEnumerable<T> thisCollection, IEnumerable<T> otherCollection, IEqualityComparer<T> comparer)
        {
            var itemToCountMapping = new Dictionary<T, int>(comparer);
            return thisCollection.IsEquivalentTo(otherCollection, itemToCountMapping);
        }

        /// <summary>
        /// <para>
        /// NOTE: DO NOT use this method outside of the Contracts assembly. There is an identical method in Common.
        /// The reason this class exists is so that the Contracts assembly  does not take any dependencies on an
        /// external assembly (other than <see cref="System"/> and <c>mscorlib</c>).
        /// </para>
        /// Returns whether or not two different collections contain an identical count and
        /// composition of elements irrespective of the ordering of any individual element in either collection.
        /// </summary>
        /// <typeparam name="T">
        /// The type that all the elements in <paramref name="thisCollection"/> and <paramref name="otherCollection"/>
        /// inherit from or are an instance of.
        /// </typeparam>
        /// <param name="thisCollection">The first collection to compare. </param>
        /// <param name="otherCollection">The second collection to compare. </param>
        /// <returns>
        /// true if <paramref name="thisCollection"/> and <paramref name="otherCollection"/> have:
        /// 1. The same size as given by
        /// 2. Identical counts of any given element in the collection
        /// <para/>
        /// In all other cases, false.
        /// </returns>
        public static bool IsEquivalentTo<T>(this IEnumerable<T> thisCollection, IEnumerable<T> otherCollection)
        {
            var itemToCountMapping = new Dictionary<T, int>();
            return thisCollection.IsEquivalentTo(otherCollection, itemToCountMapping);
        }

        /// <summary>
        /// Returns whether or not two different collections contain an identical count and
        /// composition of elements irrespective of the ordering of any individual element in either collection.
        /// </summary>
        private static bool IsEquivalentTo<T>(this IEnumerable<T> thisCollection, IEnumerable<T> otherCollection, IDictionary<T, int> itemToCountMapping)
        {
            var thisList = thisCollection.ToList();
            var otherList = otherCollection.ToList();

            if (thisList.Count() != otherList.Count())
            {
                return false;
            }

            foreach (var item in thisList)
            {
                var count = itemToCountMapping.ContainsKey(item)
                    ? itemToCountMapping[item]
                    : 0;
                itemToCountMapping[item] = ++count;
            }

            foreach (var item in otherList)
            {
                int count;

                if (!itemToCountMapping.TryGetValue(item, out count) ||
                    count == 0)
                {
                    return false;
                }

                itemToCountMapping[item] = --count;
            }

            return itemToCountMapping.All(kvp => kvp.Value == 0);
        }
    }
}
