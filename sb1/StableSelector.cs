using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sbx
{
    public class StableSelector<T> : IDisposable
    {
        private readonly int subdivisions;
        private readonly IEnumerable<T> items;
        private IEnumerable<T> selection;

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

        public bool Select(int subdivision)
        {
            if (selection == null) return false;

            var lowerCount = Math.DivRem(selection.Count(), subdivisions, out int rem);
            if (rem > subdivision)
            {
                // incorporate remainder into lowerCount
                lowerCount++;
                rem = 0;
            }

            selection = selection.Skip(lowerCount * subdivision + rem).Take(lowerCount);
            return lowerCount > 1;
        }

        public void Unselect()
        {
            selection = items;
        }

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

        public IEnumerable<T> GetCurrentSubdivision()
        {
            return selection;
        }
    }
}
