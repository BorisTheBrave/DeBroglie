using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Topo;
using DeBroglie.Trackers;
using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie
{
    public class PriorityAndWeight
    {
        public int Priority { get; set; }
        public double Weight { get; set; }
    }

    public class TilePropagatorOptions
    {
        /// <summary>
        /// Maximum number of steps to backtrack.
        /// Set to 0 to disable backtracking, and -1 for indefinite amounts of backtracking.
        /// </summary>
        public int BackTrackDepth { get; set; }

        /// <summary>
        /// Extra constraints to control the generation process.
        /// </summary>
        public ITileConstraint[] Constraints { get; set; }

        /// <summary>
        /// Overrides the weights set from the model, on a per-position basis.
        /// </summary>
        public ITopoArray<IDictionary<Tile, PriorityAndWeight>> Weights { get; set; }

        /// <summary>
        /// Source of randomness used by generation
        /// </summary>
        public Func<double> RandomDouble { get; set; }

        [Obsolete("Use RandomDouble")]
        public Random Random { get; set; }
    }

    // Implemenation wise, this wraps a WavePropagator to do the majority of the work.
    // The only thing this class handles is conversion of tile objects into sets of patterns
    // And co-ordinate conversion.
    /// <summary>
    /// TilePropagator is the main entrypoint to the DeBroglie library. 
    /// It takes a <see cref="TileModel"/> and an output <see cref="Topology"/> and generates
    /// an output array using those parameters.
    /// </summary>
    public class TilePropagator
    {
        private readonly WavePropagator wavePropagator;

        private readonly ITopology topology;

        private readonly TileModel tileModel;

        private readonly TileModelMapping tileModelMapping;

        /// <summary>
        /// Constructs a TilePropagator.
        /// </summary>
        /// <param name="tileModel">The model to guide the generation.</param>
        /// <param name="topology">The dimensions of the output to generate</param>
        /// <param name="backtrack">If true, store additional information to allow rolling back choices that lead to a contradiction.</param>
        /// <param name="constraints">Extra constraints to control the generation process.</param>
        public TilePropagator(TileModel tileModel, ITopology topology, bool backtrack = false,
            ITileConstraint[] constraints = null)
            : this(tileModel, topology, new TilePropagatorOptions
            {
                BackTrackDepth = backtrack ? -1 : 0,
                Constraints = constraints,
            })
        {

        }

        /// <summary>
        /// Constructs a TilePropagator.
        /// </summary>
        /// <param name="tileModel">The model to guide the generation.</param>
        /// <param name="topology">The dimensions of the output to generate</param>
        /// <param name="backtrack">If true, store additional information to allow rolling back choices that lead to a contradiction.</param>
        /// <param name="constraints">Extra constraints to control the generation process.</param>
        /// <param name="random">Source of randomness</param>
        [Obsolete("Use TilePropagatorOptions")]
        public TilePropagator(TileModel tileModel, ITopology topology, bool backtrack,
            ITileConstraint[] constraints,
            Random random)
            :this(tileModel, topology, new TilePropagatorOptions
            {
                BackTrackDepth = backtrack ? -1 : 0,
                Constraints = constraints,
                Random = random,
            })
        {

        }

        public TilePropagator(TileModel tileModel, ITopology topology, TilePropagatorOptions options)
        {
            this.tileModel = tileModel;
            this.topology = topology;

            var overlapping = tileModel as OverlappingModel;

            tileModelMapping = tileModel.GetTileModelMapping(topology);
            var patternTopology = tileModelMapping.PatternTopology;
            var patternModel = tileModelMapping.PatternModel;

            var waveConstraints =
                (options.Constraints?.Select(x => new TileConstraintAdaptor(x, this)).ToArray() ?? Enumerable.Empty<IWaveConstraint>())
                .ToArray();

            var waveFrequencySets = options.Weights == null ? null : GetFrequencySets(options.Weights, tileModelMapping);

#pragma warning disable CS0618 // Type or member is obsolete
            this.wavePropagator = new WavePropagator(
                patternModel, 
                patternTopology, 
                options.BackTrackDepth, 
                waveConstraints, 
                options.RandomDouble ?? (options.Random == null ? (Func<double>)null : options.Random.NextDouble),
                waveFrequencySets,
                clear: false);
#pragma warning restore CS0618 // Type or member is obsolete
            wavePropagator.Clear();

        }

        private void TileCoordToPatternCoord(int x, int y, int z, out int px, out int py, out int pz, out int offset)
        {
            tileModelMapping.GetTileCoordToPatternCoord(x, y, z, out px, out py, out pz, out offset);
        }

        private static FrequencySet[] GetFrequencySets(ITopoArray<IDictionary<Tile, PriorityAndWeight>> weights, TileModelMapping tileModelMapping)
        {
            var frequencies = new FrequencySet[tileModelMapping.PatternTopology.IndexCount];
            foreach(var patternIndex in tileModelMapping.PatternTopology.GetIndices())
            {
                // TODO
                if (tileModelMapping.PatternCoordToTileCoordIndexAndOffset != null)
                    throw new NotImplementedException();

                var tileIndex = patternIndex;
                var offset = 0;
                var weightDict = weights.Get(tileIndex);
                var newWeights = new double[tileModelMapping.PatternModel.PatternCount];
                var newPriorities = new int[tileModelMapping.PatternModel.PatternCount];
                foreach(var kv in weightDict)
                {
                    var pattern = tileModelMapping.TilesToPatternsByOffset[offset][kv.Key].Single();
                    newWeights[pattern] = kv.Value.Weight;
                    newPriorities[pattern] = kv.Value.Priority;
                }
                frequencies[patternIndex] = new FrequencySet(newWeights, newPriorities);
            }
            return frequencies;
        }

        /// <summary>
        /// The topology of the output.
        /// </summary>
        public ITopology Topology => topology;

        /// <summary>
        /// The source of randomness
        /// </summary>
        public Func<double> RandomDouble => wavePropagator.RandomDouble;


        /// <summary>
        /// The model to use when generating.
        /// </summary>
        public TileModel TileModel => tileModel;

        /// <summary>
        /// The overall resolution of the generated array.
        /// This will be <see cref="Resolution.Contradiction"/> if at least one location is in contradiction (has no possible tiles)
        /// otherwilse will be <see cref="Resolution.Undecided"/> if at least one location is undecided (has multiple possible tiles)
        /// and will be <see cref="Resolution.Decided"/> otherwise (exactly one tile valid for each location).
        /// </summary>
        public Resolution Status => wavePropagator.Status;

        /// <summary>
        /// This is incremented each time it is necessary to backtrack
        /// a tile while generating results.
        /// It is reset when <see cref="Clear"/> is called.
        /// </summary>
        public int BacktrackCount => wavePropagator.BacktrackCount;

        /// <summary>
        /// Returns a number between 0 and 1 indicating how much of the generation is complete.
        /// This number may decrease at times due to backtracking.
        /// </summary>
        /// <returns></returns>
        public double GetProgress()
        {
            return wavePropagator.Wave.GetProgress();
        }

        /// <summary>
        /// Resets the TilePropagator to the state it was in at construction.
        /// </summary>
        /// <returns>The current <see cref="Status"/> (usually <see cref="Resolution.Undecided"/> unless there are very specific initial conditions)</returns>
        public Resolution Clear()
        {
            return wavePropagator.Clear();
        }

        /// <summary>
        /// Indicates that the generation cannot proceed, forcing the algorithm to backtrack or exit.
        /// </summary>
        public void SetContradiction()
        {
            wavePropagator.SetContradiction();
        }

        /// <summary>
        /// Marks the given tile as not being a valid choice at a given location.
        /// Then it propagates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Ban(int x, int y, int z, Tile tile)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var o);
            var patterns = tileModelMapping.GetPatterns(tile, o);
            foreach(var p in patterns)
            {
                var status = wavePropagator.Ban(px, py, pz, p);
                if (status != Resolution.Undecided)
                    return status;
            }
            return Resolution.Undecided;
        }

        /// <summary>
        /// Marks the given tiles as not being a valid choice at a given location.
        /// Then it propagates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Ban(int x, int y, int z, IEnumerable<Tile> tiles)
        {
            return Ban(x, y, z, CreateTileSet(tiles));
        }

        /// <summary>
        /// Marks the given tiles as not being a valid choice at a given location.
        /// Then it propagates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Ban(int x, int y, int z, TilePropagatorTileSet tiles)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var o);
            var patterns = tileModelMapping.GetPatterns(tiles, o);
            foreach (var p in patterns)
            {
                var status = wavePropagator.Ban(px, py, pz, p);
                if (status != Resolution.Undecided)
                    return status;
            }
            return Resolution.Undecided;
        }

        /// <summary>
        /// Marks the given tile as the only valid choice at a given location.
        /// This is equivalent to banning all other tiles.
        /// Then it propagates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Select(int x, int y, int z, Tile tile)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var o);
            var patterns = tileModelMapping.GetPatterns(tile, o);
            for (var p = 0; p < wavePropagator.PatternCount; p++)
            {
                if (patterns.Contains(p))
                    continue;
                var status = wavePropagator.Ban(px, py, pz, p);
                if (status != Resolution.Undecided)
                    return status;
            }
            return Resolution.Undecided;
        }

        /// <summary>
        /// Marks the given tiles as the only valid choice at a given location.
        /// This is equivalent to banning all other tiles.
        /// Then it propagates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Select(int x, int y, int z, IEnumerable<Tile> tiles)
        {
            return Select(x, y, z, CreateTileSet(tiles));
        }

        /// <summary>
        /// Marks the given tiles as the only valid choice at a given location.
        /// This is equivalent to banning all other tiles.
        /// Then it propagates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Select(int x, int y, int z, TilePropagatorTileSet tiles)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var o);
            var patterns = tileModelMapping.GetPatterns(tiles, o);
            for (var p = 0; p < wavePropagator.PatternCount; p++)
            {
                if (patterns.Contains(p))
                    continue;
                var status = wavePropagator.Ban(px, py, pz, p);
                if (status != Resolution.Undecided)
                    return status;
            }
            return Resolution.Undecided;
        }

        /// <summary>
        /// Makes a single tile selection.
        /// Then it propagates that information to other nearby tiles.
        /// If backtracking is enabled a single step can include several backtracks,.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Step()
        {
            return wavePropagator.Step();
        }

        /// <summary>
        /// Repeatedly Steps until the status is Decided or Contradiction.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Run()
        {
            return wavePropagator.Run();
        }

        internal SelectedTracker CreateSelectedTracker(TilePropagatorTileSet tileSet)
        {
            var tracker = new SelectedTracker(this, wavePropagator, tileModelMapping, tileSet);
            tracker.Reset();
            wavePropagator.AddTracker(tracker);
            return tracker;
        }

        internal SelectedChangeTracker CreateSelectedChangeTracker(TilePropagatorTileSet tileSet, ITristateChanged onChange)
        {
            var tracker = new SelectedChangeTracker(this, wavePropagator, tileModelMapping, tileSet, onChange);
            tracker.Reset();
            wavePropagator.AddTracker(tracker);
            return tracker;
        }

        /// <summary>
        /// Returns a tracker that indicates all recently changed tiles.
        /// This is mostly useful as a performance optimization.
        /// Trackers are valid until <see cref="Clear"/> is called.
        /// </summary>
        internal ChangeTracker CreateChangeTracker()
        {
            var tracker = new ChangeTracker(tileModelMapping);
            tracker.Reset();
            wavePropagator.AddTracker(tracker);
            return tracker;
        }

        /// <summary>
        /// Creates a set of tiles. This set can be used with some operations, and is marginally
        /// faster than passing in a fresh list of tiles ever time.
        /// </summary>
        public TilePropagatorTileSet CreateTileSet(IEnumerable<Tile> tiles)
        {
            return tileModelMapping.CreateTileSet(tiles);
        }

        /// <summary>
        /// Returns true if this tile is the only valid selection for a given location.
        /// </summary>
        public bool IsSelected(int x, int y, int z, Tile tile)
        {
            GetBannedSelected(x, y, z, tile, out var isBanned, out var isSelected);
            return isSelected;
        }

        /// <summary>
        /// Returns true if this tile is the not a valid selection for a given location.
        /// </summary>
        public bool IsBanned(int x, int y, int z, Tile tile)
        {
            GetBannedSelected(x, y, z, tile, out var isBanned, out var isSelected);
            return isBanned;
        }

        /// <summary>
        /// Gets the results of both IsBanned and IsSelected
        /// </summary>
        public void GetBannedSelected(int x, int y, int z, Tile tile, out bool isBanned, out bool isSelected)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var o);
            var patterns = tileModelMapping.TilesToPatternsByOffset[o][tile];
            GetBannedSelectedInternal(px, py, pz, patterns, out isBanned, out isSelected);
        }

        /// <summary>
        /// isBanned is set to true if all the tiles are not valid in the location.
        /// isSelected is set to true if no other the tiles are valid in the location.
        /// </summary>
        public void GetBannedSelected(int x, int y, int z, IEnumerable<Tile> tiles, out bool isBanned, out bool isSelected)
        {
            GetBannedSelected(x, y, z, CreateTileSet(tiles), out isBanned, out isSelected);
        }

        /// <summary>
        /// isBanned is set to true if all the tiles are not valid in the location.
        /// isSelected is set to true if no other the tiles are valid in the location.
        /// </summary>
        public void GetBannedSelected(int x, int y, int z, TilePropagatorTileSet tiles, out bool isBanned, out bool isSelected)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var o);
            var patterns = tileModelMapping.GetPatterns(tiles, o);
            GetBannedSelectedInternal(px, py, pz, patterns, out isBanned, out isSelected);
        }

        internal Tristate GetSelectedTristate(int x, int y, int z, TilePropagatorTileSet tiles)
        {
            GetBannedSelected(x, y, z, tiles, out var isBanned, out var isSelected);
            return isSelected ? Tristate.Yes : isBanned ? Tristate.No : Tristate.Maybe;
        }


        private void GetBannedSelectedInternal(int px, int py, int pz, ISet<int> patterns, out bool isBanned, out bool isSelected)
        {
            var index = wavePropagator.Topology.GetIndex(px, py, pz);
            var wave = wavePropagator.Wave;
            var patternCount = wavePropagator.PatternCount;
            isBanned = true;
            isSelected = true;
            for (var p = 0; p < patternCount; p++)
            {
                if (wave.Get(index, p))
                {
                    if (patterns.Contains(p))
                    {
                        isBanned = false;
                    }
                    else
                    {
                        isSelected = false;
                    }
                }
            }
        }

        /// <summary>
        /// Converts the generated results to an <see cref="ITopoArray{Tile}"/>,
        /// using specific tiles for locations that have not been decided or are in contradiction.
        /// The arguments are not relevant if the <see cref="Status"/> is <see cref="Resolution.Decided"/>.
        /// </summary>
        public ITopoArray<Tile> ToArray(Tile undecided = default(Tile), Tile contradiction = default(Tile))
        {
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            var patternArray = wavePropagator.ToTopoArray();

            return TopoArray.CreateByIndex(index =>
            {
                topology.GetCoord(index, out var x, out var y, out var z);
                TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var o);
                var pattern = patternArray.Get(index);
                Tile tile;
                if (pattern == (int)Resolution.Undecided)
                {
                    tile = undecided;
                }
                else if (pattern == (int)Resolution.Contradiction)
                {
                    tile = contradiction;
                }
                else
                {
                    tile = tileModelMapping.PatternsToTilesByOffset[o][pattern];
                }
                return tile;
            }, topology);
        }

        /// <summary>
        /// Converts the generated results to an <see cref="ITopoArray{T}"/>,
        /// by extracting the value of each decided tile and
        /// using specific values for locations that have not been decided or are in contradiction.
        /// This is simply a convenience over 
        /// The arguments are not relevant if the <see cref="Status"/> is <see cref="Resolution.Decided"/>.
        /// </summary>
        public ITopoArray<T> ToValueArray<T>(T undecided = default(T), T contradiction = default(T))
        {
            // TODO: Just call ToArray() ?
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            var patternArray = wavePropagator.ToTopoArray();

            return TopoArray.CreateByIndex(index =>
            {
                topology.GetCoord(index, out var x, out var y, out var z);

                TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var o);
                var pattern = patternArray.Get(px, py, pz);
                T value;
                if (pattern == (int)Resolution.Undecided)
                {
                    value = undecided;
                }
                else if (pattern == (int)Resolution.Contradiction)
                {
                    value = contradiction;
                }
                else
                {
                    value = (T)tileModelMapping.PatternsToTilesByOffset[o][pattern].Value;
                }
                return value;
            }, topology);
        }

        /// <summary>
        /// Convert the generated result to an array of sets, where each set
        /// indicates the tiles that are still valid at the location.
        /// The size of the set indicates the resolution of that location:
        /// * Greater than 1: <see cref="Resolution.Undecided"/>
        /// * Exactly 1: <see cref="Resolution.Decided"/>
        /// * Exactly 0: <see cref="Resolution.Contradiction"/>
        /// </summary>
        public ITopoArray<ISet<Tile>> ToArraySets()
        {
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            var patternArray = wavePropagator.ToTopoArraySets();

            return TopoArray.CreateByIndex(index =>
            {
                topology.GetCoord(index, out var x, out var y, out var z);

                TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var o);
                var patterns = patternArray.Get(px, py, pz);
                var hs = new HashSet<Tile>();
                var patternToTiles = tileModelMapping.PatternsToTilesByOffset[o];
                foreach (var pattern in patterns)
                {
                    hs.Add(patternToTiles[pattern]);
                }
                return(ISet<Tile>)hs;
            }, topology);
        }

        /// <summary>
        /// Convert the generated result to an array of sets, where each set
        /// indicates the values of tiles that are still valid at the location.
        /// The size of the set indicates the resolution of that location:
        /// * Greater than 1: <see cref="Resolution.Undecided"/>
        /// * Exactly 1: <see cref="Resolution.Decided"/>
        /// * Exactly 0: <see cref="Resolution.Contradiction"/>
        /// </summary>
        public ITopoArray<ISet<T>> ToValueSets<T>()
        {
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            var patternArray = wavePropagator.ToTopoArraySets();

            return TopoArray.CreateByIndex(index =>
            {
                topology.GetCoord(index, out var x, out var y, out var z);

                TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var o);
                var patterns = patternArray.Get(px, py, pz);
                var hs = new HashSet<T>();
                var patternToTiles = tileModelMapping.PatternsToTilesByOffset[o];
                foreach (var pattern in patterns)
                {
                    hs.Add((T)patternToTiles[pattern].Value);
                }
                return (ISet<T>)hs;
            }, topology);
        }
    }
}
