using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sb1
{
    public class StableSelector<T>
    {
        private int subdivisions;
        private IEnumerable<T> items;
        private IEnumerable<T> selection;

        public StableSelector(IEnumerable<T> items, int subdivisions)
        {
            this.items = items;
            selection = items;
            this.subdivisions = subdivisions;
        }

        public bool Select(int subdivision)
        {
            if (selection == null) return false;

            int rem;
            var lowerCount = Math.DivRem(selection.Count(), subdivisions, out rem);
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
            if (!selection.Any()) yield break;

            int rem;
            var lowerCount = Math.DivRem(selection.Count(), subdivisions, out rem);
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
