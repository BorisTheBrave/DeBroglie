using System.Collections.Generic;
using System.Linq;

namespace DeBroglie
{
    /**
     * A TileModel is a model with a well defined mapping from 
     * "tiles" (arbitrary identifiers of distinct tiles)
     * with patterns (dense integers that correspond to particular
     * arrangements of tiles).
     */
    public abstract class TileModel<T> : Model
    {
        public abstract IReadOnlyDictionary<int, T> PatternsToTiles { get; }
        public abstract ILookup<T, int> TilesToPatterns { get; }
        public abstract IEqualityComparer<T> Comparer { get; }

        public virtual T[,] ToArray(WavePropagator wavePropagator, T undecided = default(T), T contradiction = default(T))
        {
            T MapPatternOrStatus(int pattern)
            {
                if (pattern == (int)CellStatus.Contradiction)
                {
                    return contradiction;
                }
                else if (pattern == (int)CellStatus.Undecided)
                {
                    return undecided;
                }
                else
                {
                    return PatternsToTiles[pattern];
                }
            }

            var a = wavePropagator.ToArray();
            var width = a.GetLength(0);
            var height = a.GetLength(1);
            var results = new T[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    results[x, y] = MapPatternOrStatus(a[x, y]);
                }
            }
            return results;
        }

        public virtual List<T>[,] ToArraySets(WavePropagator wavePropagator)
        {
            List<T> Map(List<int> patterns)
            {
                HashSet<T> set = new HashSet<T>(Comparer);
                foreach (var pattern in patterns)
                {
                    set.Add(PatternsToTiles[pattern]);
                }
                return set.ToList();
            }

            var a = wavePropagator.ToArraySets();
            var width = a.GetLength(0);
            var height = a.GetLength(1);
            var results = new List<T>[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    results[x, y] = Map(a[x, y]);
                }
            }
            return results;
        }
    }
}
