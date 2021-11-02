﻿using DeBroglie.Constraints;
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
        /// Pick the first available index
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
        /// Source of randomness used by generation
        /// </summary>
        public Func<double> RandomDouble { get; set; }

        [Obsolete("Use RandomDouble")]
        public Random Random { get; set; }

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
        /// Only used by <see cref="IndexPickerType.ArrayPriorityMinEntropy"/>
        /// </summary>
        public ITopoArray<IDictionary<Tile, PriorityAndWeight>> Weights { get; set; }
    }
}
