using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Helpers.Commons
{
    public static class CollectionsHelper
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return true;
            }

            if (enumerable is ICollection<T> collection)
            {
                return collection.Count < 1;
            }
            return !enumerable.Any();
        }
    }
}
