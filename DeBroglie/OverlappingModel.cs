using System.Collections.Generic;
using System.Linq;

namespace DeBroglie
{

    public class OverlappingModel<T> : TileModel<T>
    {
        private List<PatternArray<T>> patternArrays;

        private int n;
        private bool periodic;
        int rotationalSymmetry;
        bool reflectionalSymmetry;

        private int groundPattern;

        private IReadOnlyDictionary<int, T> patternsToTiles;
        private ILookup<T, int> tilesToPatterns;
        private IEqualityComparer<T> comparer;

        public OverlappingModel(T[,] sample, int n, bool periodic, int symmetries)
            :this(new TopArray2D<T>(sample, periodic), n, symmetries)
        {

        }

        public OverlappingModel(ITopArray<T> sample, int n, int symmetries)
            : this(sample, n, symmetries > 1 ? symmetries / 2 : 1, symmetries > 1)

        {

        }


        public OverlappingModel(ITopArray<T> sample, int n, int rotationalSymmetry, bool reflectionalSymmetry)
        {
            this.n = n;
            this.periodic = sample.Topology.Periodic;
            this.rotationalSymmetry = rotationalSymmetry;
            this.reflectionalSymmetry = reflectionalSymmetry;

            this.comparer = EqualityComparer<T>.Default;

            List<double> frequencies;

            OverlappingAnalysis.GetPatterns(sample, n, periodic, rotationalSymmetry, reflectionalSymmetry, comparer, out patternArrays, out frequencies, out groundPattern);

            this.Frequencies = frequencies.ToArray();

            var directions = sample.Topology.Directions;

            Propagator = new int[patternArrays.Count][][];

            for (var p = 0; p < patternArrays.Count; p++)
            {
                Propagator[p] = new int[directions.Count][];
                for (var d = 0; d < directions.Count; d++)
                {
                    var l = new List<int>();
                    for (var p2 = 0; p2 < patternArrays.Count; p2++)
                    {
                        var dx = directions.DX[d];
                        var dy = directions.DY[d];
                        var dz = directions.DZ[d];
                        if (Aggrees(patternArrays[p], patternArrays[p2], dx, dy, dz))
                        {
                            l.Add(p2);
                        }
                    }
                    Propagator[p][d] = l.ToArray();
                }
            }

            patternsToTiles = patternArrays
                .Select((x, i) => new KeyValuePair<int, T>(i, x.Values[0, 0, 0]))
                .ToDictionary(x => x.Key, x => x.Value);

            tilesToPatterns = patternsToTiles.ToLookup(x => x.Value, x => x.Key, comparer);
        }

        public override IReadOnlyDictionary<int, T> PatternsToTiles => patternsToTiles;
        public override ILookup<T, int> TilesToPatterns => tilesToPatterns;
        public override IEqualityComparer<T> Comparer => comparer;

        public int N => n;

        public IReadOnlyList<PatternArray<T>> PatternArrays => patternArrays;

        /**
          * Return true if the pattern1 is compatible with pattern2
          * when pattern2 is at a distance (dy,dx) from pattern1.
          */
        private bool Aggrees(PatternArray<T> a, PatternArray<T> b, int dx, int dy, int dz)
        {
            var xmin = dx < 0 ? 0 : dx;
            var xmax = dx < 0 ? dx + b.Width : a.Width;
            var ymin = dy < 0 ? 0 : dy;
            var ymax = dy < 0 ? dy + b.Height : a.Height;
            var zmin = dz < 0 ? 0 : dz;
            var zmax = dz < 0 ? dz + b.Depth : a.Depth;
            for (var x = xmin; x < xmax; x++)
            {
                for (var y = ymin; y < ymax; y++)
                {
                    for (var z = zmin; z < zmax; z++)
                    {
                        if (!comparer.Equals(a.Values[x, y, z], b.Values[x - dx, y - dy, z - dz]))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public GroundConstraint GetGroundConstraint()
        {
            return new GroundConstraint(groundPattern);
        }
    }

}
