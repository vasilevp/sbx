using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sbx
{
    public class MultiSelector<T>
    {
        private int subdivisions;
        private IList<T> items;
        private List<int> selections = new List<int>();

        public MultiSelector(IList<T> items, int subdivisions)
        {
            this.items = items ?? new List<T>();
            this.subdivisions = subdivisions;
        }

        public bool Select(int subdivision)
        {
            selections.Add(subdivision);

            int divisor = subdivisions;
            int start = 0;
            foreach (var subdiv in selections)
            {
                start += subdiv * ((items.Count - 1) / divisor + 1);
                divisor *= subdivisions;
            }

            if (start >= items.Count - 1) return false;

            var curCount = (items.Count - 1) * subdivisions / divisor + 1;
            return curCount > 1;
        }

        public void Unselect()
        {
            selections.Clear();
        }

        public IEnumerable<IEnumerable<T>> GetSubdivisions()
        {
            var result = new List<IEnumerable<T>>();
            int divisor = subdivisions;
            int start = 0;
            foreach (var subdiv in selections)
            {
                start += subdiv * ((items.Count - 1) / divisor + 1);
                divisor *= subdivisions;
            }

            int subSubDivLength = (items.Count - 1) / divisor + 1;

            int validSubdivisions = subdivisions;

            if (subSubDivLength == 1)
            {
                // if less than <subdivisions> items, only return the max amount
                validSubdivisions = (items.Count - 1) * subdivisions / divisor + 1;
            }

            for (int subdiv = 0; subdiv < validSubdivisions; subdiv++)
            {
                int s = start + subdiv * subSubDivLength;
                if (s >= items.Count) break;
                if (s + subSubDivLength >= items.Count)
                {
                    result.Add(items.Skip(s));
                    break;
                };
                result.Add(items.Skip(s).Take(subSubDivLength));
            }

            return result;
        }

        public IEnumerable<T> GetCurrentSubdivision()
        {
            int divisor = subdivisions;
            int start = 0;
            foreach (var subdiv in selections)
            {
                start += subdiv * ((items.Count - 1) / divisor + 1);
                divisor *= subdivisions;
            }

            return items.Skip(start).Take((items.Count - 1) / divisor + 1);
        }
    }
}
