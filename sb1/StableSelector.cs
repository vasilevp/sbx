using System;
using System.Collections.Generic;
using System.Linq;

namespace sbx
{
    public class StableSelector<T> : IDisposable
    {
        private readonly int subdivisions;
        private readonly IEnumerable<T> items;
        private IEnumerable<T> selection;

        /// <summary>
        /// Create a StableSelector from a set of items and number of subdivisions
        /// </summary>
        /// <param name="items">Items to select from</param>
        /// <param name="subdivisions">Number of subdivisions for each selection</param>
        public StableSelector(IEnumerable<T> items, int subdivisions)
        {
            this.items = items;
            selection = items;
            this.subdivisions = subdivisions;
        }

        public void Dispose()
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                (item as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// Select a subset of elements by number
        /// </summary>
        /// <param name="subdivision">Index of the target subdivision</param>
        /// <returns>Return value is a boolean indicating whether there are more than 1 elements in the resulting selection</returns>
        public bool Select(int subdivision)
        {
            if (selection == null) return false;

            // get the minimum amount of elements in a subdivision
            var lowerCount = Math.DivRem(selection.Count(), subdivisions, out int rem);

            // if there's more extra elements than N, split them among first subdivisions instead of bunching into the first one
            if (rem > subdivision)
            {
                // incorporate remainder into lowerCount
                lowerCount++;
                rem = 0;
            }

            // narrow down selection to the target subdivision
            selection = selection.Skip(lowerCount * subdivision + rem).Take(lowerCount);

            // if >1, then we can subdivide further
            return lowerCount > 1;
        }

        /// <summary>
        /// Reset selection
        /// </summary>
        public void Unselect()
        {
            selection = items;
        }

        /// <summary>
        /// Get possible subdivisions
        /// </summary>
        /// <returns>All possible subdivisions</returns>
        public IEnumerable<IEnumerable<T>> GetSubdivisions()
        {
            if (selection == null || !selection.Any()) yield break;

            var lowerCount = Math.DivRem(selection.Count(), subdivisions, out int rem);
            var piece = selection;
            for (var i = 0; i < subdivisions; i++)
            {
                var cnt = rem > i ? lowerCount + 1 : lowerCount;
                yield return piece.Take(cnt);
                piece = piece.Skip(cnt);
            }
        }

        /// <summary>
        /// Get all elements in the current subdivision
        /// </summary>
        /// <returns>All elements in the current subdivision</returns>
        public IEnumerable<T> GetCurrentSubdivision()
        {
            return selection;
        }
    }
}
