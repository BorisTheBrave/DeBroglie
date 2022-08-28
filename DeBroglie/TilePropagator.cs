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
                BacktrackType = backtrack ? BacktrackType.Backtrack : BacktrackType.Backjump,
                Constraints = constraints,
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


            var randomDouble = options.RandomDouble ?? new Random().NextDouble;

            var (indexPicker, patternPicker) = MakePickers(options);

            var wavePropagatorOptions = new WavePropagatorOptions
            {
                BacktrackPolicy = MakeBacktrackPolicy(options),
                MaxBacktrackDepth = options.MaxBacktrackDepth,
                RandomDouble = randomDouble,
                Constraints = waveConstraints,
                IndexPicker = indexPicker,
                PatternPicker = patternPicker,
                Clear = false,
                ModelConstraintAlgorithm = options.ModelConstraintAlgorithm,
            };

            this.wavePropagator = new WavePropagator(
                patternModel, 
                patternTopology,
                wavePropagatorOptions);
            wavePropagator.Clear();

        }

        private static IBacktrackPolicy MakeBacktrackPolicy(TilePropagatorOptions options)
        {
            switch(options.BacktrackType)
            {
                case BacktrackType.None:
                    return null;
                case BacktrackType.Backtrack:
                    return new ConstantBacktrackPolicy(1);
                case BacktrackType.Backjump:
                    return new PatienceBackjumpPolicy();
                default:
                    throw new Exception($"Unknown BacktrackType {options.BacktrackType}");
            }
        }

        private Tuple<IIndexPicker, IPatternPicker> MakePickers(TilePropagatorOptions options)
        {
            var connectedConstraint = options.Constraints?.OfType<ConnectedConstraint>().FirstOrDefault();
            var connectedPickHeuristic = connectedConstraint != null && connectedConstraint.UsePickHeuristic;

            if(connectedPickHeuristic)
            {
                // Lists pickers that implement IFilteredIndexPicker
                if (options.IndexPickerType != IndexPickerType.Default &&
                    options.IndexPickerType != IndexPickerType.MinEntropy &&
                    options.IndexPickerType != IndexPickerType.Ordered)
                {
                    throw new Exception($"Connected Pick Heuristic is incompatible with the selected IndexPikcerType {options.IndexPickerType}");
                }
                if (options.IndexPickerType == IndexPickerType.Default)
                    options.IndexPickerType = IndexPickerType.MinEntropy;
            }

            // Use the appropriate random picker
            // Generally this is HeapEntropyTracker, but it doesn't support some features
            // so there's a few slower implementations for that
            IIndexPicker indexPicker = null;
            IPatternPicker patternPicker = null;

            switch (options.IndexPickerType)
            {
                case IndexPickerType.Ordered:
                    {
                        if (options.IndexOrder != null)
                        {
                            indexPicker = new OrderedIndexPicker(options.IndexOrder);
                        }
                        else
                        {
                            indexPicker = new SimpleOrderedIndexPicker();
                        }
                        break;
                    }
                case IndexPickerType.ArrayPriorityMinEntropy:
                    {
                        if (options.WeightSetByIndex == null || options.WeightSets == null)
                            throw new ArgumentNullException($"Expected WeightSetByIndex and WeightSets to be set");
                        if (options.TilePickerType != TilePickerType.ArrayPriority && options.TilePickerType != TilePickerType.Default)
                            throw new Exception($"ArrayPriorityMinEntropy only works with Default tile picker");

                        var weightSetCollection = new WeightSetCollection(options.WeightSetByIndex, options.WeightSets, tileModelMapping);
                        var entropyTracker = new ArrayPriorityEntropyTracker(weightSetCollection);

                        indexPicker = entropyTracker;
                        patternPicker = entropyTracker;
                        break;
                    }
                case IndexPickerType.MinEntropy:
                    {
                        indexPicker = new EntropyTracker();
                        break;
                    }
                case IndexPickerType.Default:
                case IndexPickerType.HeapMinEntropy:
                    {
                        indexPicker = new HeapEntropyTracker();
                        break;
                    }
                case IndexPickerType.Dirty:
                    {
                        // Create clean patterns
                        if (tileModelMapping.TileCoordToPatternCoordIndexAndOffset != null)
                            throw new NotSupportedException();
                        if (options.CleanTiles == null)
                            throw new ArgumentNullException($"{nameof(options.CleanTiles)} is null");
                        var cleanPatterns = options.CleanTiles.Map(t => tileModelMapping.TilesToPatternsByOffset[0][t].First());

                        indexPicker = new DirtyIndexPicker(new SimpleOrderedIndexPicker(), cleanPatterns);
                        break;
                    }
                default:
                    throw new Exception($"Unknown IndexPickerType {options.IndexPickerType}");
            }

            if (patternPicker == null)
            {
                switch (options.TilePickerType)
                {
                    case TilePickerType.Default:
                    case TilePickerType.Weighted:
                        patternPicker = new WeightedRandomPatternPicker();
                        break;
                    case TilePickerType.Ordered:
                        patternPicker = new SimpleOrderedPatternPicker();
                        break;
                    case TilePickerType.ArrayPriority:
                        if (options.WeightSetByIndex == null || options.WeightSets == null)
                            throw new ArgumentNullException($"Expected WeightSetByIndex and WeightSets to be set");
                        var weightSetCollection = new WeightSetCollection(options.WeightSetByIndex, options.WeightSets, tileModelMapping);
                        patternPicker = new ArrayPriorityPatternPicker(weightSetCollection);
                        break;
                    default:
                        throw new Exception($"Unknown TilePickerType {options.TilePickerType}");
                }
            }

            if (connectedPickHeuristic)
            {
                indexPicker = connectedConstraint.GetHeuristic(
                    (IFilteredIndexPicker)indexPicker,
                    this);
            }

            if(options.MemoizeIndices)
            {
                indexPicker = new MemoizeIndexPicker(indexPicker);
            }

            return Tuple.Create(indexPicker, patternPicker);

        }

        private void TileCoordToPatternCoord(int x, int y, int z, out int px, out int py, out int pz, out int offset)
        {
            tileModelMapping.GetTileCoordToPatternCoord(x, y, z, out px, out py, out pz, out offset);
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
        /// A string indicating the reason a contradiction occured. This is sometimes set when Status is <see cref="Resolution.Contradiction"/>.
        /// </summary>
        public string ContradictionReason => wavePropagator.ContradictionReason;

        /// <summary>
        /// An object indicating what caused the contradiction. This is somtimes set to a give constraint if the constraint caused the issue.
        /// </summary>
        public object ContradictionSource => wavePropagator.ContradictionSource;

        /// <summary>
        /// This is incremented each time it is necessary to backtrack
        /// a tile while generating results.
        /// It is reset when <see cref="Clear"/> is called.
        /// </summary>
        public int BacktrackCount => wavePropagator.BacktrackCount;

        /// <summary>
        /// This is incremented each time it is necessary to backjump 
        /// while generating results (i.e. when multiple steps are undone simultaneously).
        /// It is reset when <see cref="Clear"/> is called.
        /// </summary>
        public int BackjumpCount => wavePropagator.BackjumpCount;

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
        /// Indicates that the generation cannot proceed, forcing the algorithm to backtrack or exit.
        /// </summary>
        public void SetContradiction(string reason, object source)
        {
            wavePropagator.SetContradiction(reason, source);
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


        public void StepConstraints()
        {
            wavePropagator.StepConstraints();
        }

        /// <summary>
        /// Repeatedly Steps until the status is Decided or Contradiction.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Run()
        {
            return wavePropagator.Run();
        }

        /// <summary>
        /// Returns a tracker that tracks the banned/selected status of each tile with respect to a tileset.
        /// </summary>
        public SelectedTracker CreateSelectedTracker(TilePropagatorTileSet tileSet)
        {
            var tracker = new SelectedTracker(this, wavePropagator, tileModelMapping, tileSet);
            ((ITracker)tracker).Reset();
            wavePropagator.AddTracker(tracker);
            return tracker;
        }

        /// <summary>
        /// Returns a tracker that runs a callback when the banned/selected status of tile changes with respect to a tileset.
        /// </summary>
        public SelectedChangeTracker CreateSelectedChangeTracker(TilePropagatorTileSet tileSet, IQuadstateChanged onChange)
        {
            var tracker = new SelectedChangeTracker(this, wavePropagator, tileModelMapping, tileSet, onChange);
            ((ITracker)tracker).Reset();
            wavePropagator.AddTracker(tracker);
            return tracker;
        }

        /// <summary>
        /// Returns a tracker that indicates all recently changed tiles.
        /// This is mostly useful as a performance optimization.
        /// Trackers are valid until <see cref="Clear"/> is called.
        /// </summary>
        public ChangeTracker CreateChangeTracker()
        {
            var tracker = new ChangeTracker(tileModelMapping);
            ((ITracker)tracker).Reset();
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
            ISet<int> patterns;
            try
            {
                patterns = tileModelMapping.TilesToPatternsByOffset[o][tile];
            }
            catch(KeyNotFoundException)
            {
                throw new KeyNotFoundException($"Couldn't find pattern for tile {tile} at offset {o}");
            }
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

        internal Quadstate GetSelectedQuadstate(int x, int y, int z, TilePropagatorTileSet tiles)
        {
            GetBannedSelected(x, y, z, tiles, out var isBanned, out var isSelected);
            if(isSelected)
            {
                if(isBanned)
                {
                    return Quadstate.Contradiction;
                }
                else
                {
                    return Quadstate.Yes;
                }
            }
            else
            {
                if(isBanned)
                {
                    return Quadstate.No;
                }
                else
                {
                    return Quadstate.Maybe;
                }
            }
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
        /// Gets the tile that has been decided at a given index.
        /// Otherwise returns undecided or contradiction as appropriate.
        /// </summary>
        public Tile GetTile(int index, Tile undecided = default, Tile contradiction = default)
        {
            tileModelMapping.GetTileCoordToPatternCoord(index, out var patternIndex, out var o);
            var pattern = wavePropagator.GetDecidedPattern(patternIndex);
            if (pattern == (int)Resolution.Undecided)
            {
                return undecided;
            }
            else if (pattern == (int)Resolution.Contradiction)
            {
                return contradiction;
            }
            else
            {
                return tileModelMapping.PatternsToTilesByOffset[o][pattern];
            }
        }

        /// <summary>
        /// Gets the value of a Tile that has been decided at a given index.
        /// Otherwise returns undecided or contradiction as appropriate.
        /// </summary>
        public T GetValue<T>(int index, T undecided = default, T contradiction = default)
        {
            tileModelMapping.GetTileCoordToPatternCoord(index, out var patternIndex, out var o);
            var pattern = wavePropagator.GetDecidedPattern(patternIndex);
            if (pattern == (int)Resolution.Undecided)
            {
                return undecided;
            }
            else if (pattern == (int)Resolution.Contradiction)
            {
                return contradiction;
            }
            else
            {
                return (T)tileModelMapping.PatternsToTilesByOffset[o][pattern].Value;
            }
        }

        public ISet<Tile> GetPossibleTiles(int index)
        {
            tileModelMapping.GetTileCoordToPatternCoord(index, out var patternIndex, out var o);
            var patterns = wavePropagator.GetPossiblePatterns(patternIndex);
            var hs = new HashSet<Tile>();
            var patternToTiles = tileModelMapping.PatternsToTilesByOffset[o];
            foreach (var pattern in patterns)
            {
                hs.Add(patternToTiles[pattern]);
            }
            return (ISet<Tile>)hs;
        }

        public IDictionary<Tile, double> GetWeightedTiles(int index)
        {
            tileModelMapping.GetTileCoordToPatternCoord(index, out var patternIndex, out var o);
            var patterns = wavePropagator.GetPossiblePatterns(patternIndex);
            var result = new Dictionary<Tile, double>();
            var patternToTiles = tileModelMapping.PatternsToTilesByOffset[o];
            foreach (var pattern in patterns)
            {
                var f = tileModelMapping.PatternModel.Frequencies[pattern];
                var tile = patternToTiles[pattern];
                if (!result.ContainsKey(tile)) result[tile] = 0;
                result[tile] += f;
            }
            return result;
        }

        public ISet<T> GetPossibleValues<T>(int index)
        {
            tileModelMapping.GetTileCoordToPatternCoord(index, out var patternIndex, out var o);
            var patterns = wavePropagator.GetPossiblePatterns(patternIndex);
            var hs = new HashSet<T>();
            var patternToTiles = tileModelMapping.PatternsToTilesByOffset[o];
            foreach (var pattern in patterns)
            {
                hs.Add((T)patternToTiles[pattern].Value);
            }
            return (ISet<T>)hs;
        }

        /// <summary>
        /// Converts the generated results to an <see cref="ITopoArray{Tile}"/>,
        /// using specific tiles for locations that have not been decided or are in contradiction.
        /// The arguments are not relevant if the <see cref="Status"/> is <see cref="Resolution.Decided"/>.
        /// </summary>
        public ITopoArray<Tile> ToArray(Tile undecided = default, Tile contradiction = default)
        {
            return TopoArray.CreateByIndex(index => GetTile(index, undecided, contradiction), topology);
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
            return TopoArray.CreateByIndex(index => GetValue(index, undecided, contradiction), topology);
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
            return TopoArray.CreateByIndex(GetPossibleTiles, topology);
        }

        public ITopoArray<IDictionary<Tile, double>> ToWeightedArraySets()
        {
            return TopoArray.CreateByIndex(GetWeightedTiles, topology);
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
            return TopoArray.CreateByIndex(GetPossibleValues<T>, topology);
        }
    }
}
