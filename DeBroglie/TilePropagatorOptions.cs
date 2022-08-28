using DeBroglie.Constraints;
using DeBroglie.Topo;
using DeBroglie.Wfc;
using System;
using System.Collections.Generic;

namespace DeBroglie
{
    public class PriorityAndWeight
    {
        public int Priority { get; set; }
        public double Weight { get; set; }
    }

    public enum IndexPickerType
    {
        /// <summary>
        /// Use the most appropriate picker, usually MinEntropy 
        /// </summary>
        Default,
        /// <summary>
        /// Pick the first available index.
        /// Uses IndexOrder if available, otherwise an arbitrary order.
        /// </summary>
        Ordered,
        /// <summary>
        /// Pick the index with the least entropy in the remaining tiles
        /// </summary>
        MinEntropy,
        /// <summary>
        /// As MinEntropy, but better optimized for large outputs
        /// </summary>
        HeapMinEntropy,
        /// <summary>
        /// Override frequencies on a per-index
        /// </summary>
        ArrayPriorityMinEntropy,
        /// <summary>
        /// Only pick indices that must deviate from a known clean value.
        /// This lets you regenerate a unknown subset of a much larger map,
        /// providing tiles are sufficiently stable.
        /// Experimental.
        /// </summary>
        Dirty,
    }

    public enum TilePickerType
    {
        /// <summary>
        /// Use the most appropriate picker, usuaully Weighted
        /// </summary>
        Default,
        /// <summary>
        /// Pick the first available tile.
        /// </summary>
        Ordered,
        /// <summary>
        /// Pick at random, based on frequencies supplied by the model
        /// </summary>
        Weighted,
        /// <summary>
        /// Use the provided weights.
        /// </summary>
        ArrayPriority,
    }

    public enum BacktrackType
    {
        None,
        Backtrack,
        Backjump,
    }

    public class TilePropagatorOptions
    {
        public BacktrackType BacktrackType { get; set; }

        /// <summary>
        /// Maximum number of steps to backtrack.
        /// 0 means disabled.
        /// </summary>
        public int MaxBacktrackDepth { get; set; }

        /// <summary>
        /// Extra constraints to control the generation process.
        /// </summary>
        public ITileConstraint[] Constraints { get; set; }

        /// <summary>
        /// Source of randomness used by generation
        /// </summary>
        public Func<double> RandomDouble { get; set; }

        /// <summary>
        /// Controls which cells are selected during generation.
        /// </summary>
        public IndexPickerType IndexPickerType { get; set; }

        /// <summary>
        /// Controls which tiles are selected during generation.
        /// </summary>
        public TilePickerType TilePickerType { get; set; }

        /// <summary>
        /// Controls the algorithm used for enforcing the constraints of the model.
        /// </summary>
        public ModelConstraintAlgorithm ModelConstraintAlgorithm { get; set; }

        /// <summary>
        /// Overrides the weights set from the model, on a per-position basis.
        /// The integers correspond to entries in WeightSets
        /// Only used by <see cref="IndexPickerType.ArrayPriorityMinEntropy"/> and <see cref="TilePickerType.ArrayPriority"/>
        /// </summary>
        public ITopoArray<int> WeightSetByIndex { get; set; }

        /// <summary>
        /// The weights sets reference by WeightSetByIndex
        /// </summary>
        public IDictionary<int, IDictionary<Tile, PriorityAndWeight>> WeightSets { get; set; }

        /// <summary>
        /// Only used by <see cref="IndexPickerType.Dirty"/>
        /// </summary>
        public ITopoArray<Tile> CleanTiles { get; set; }

        /// <summary>
        /// Only used by <see cref="IndexPickerType.Ordered"/>
        /// </summary>
        public int[] IndexOrder { get; set; }

        /// <summary>
        /// If true, the same indices will be retried after backtracking,
        /// otherwise a new choice of index will be made.
        /// </summary>
        public bool MemoizeIndices { get; set; }
    }
}
