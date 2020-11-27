using DeBroglie.Models;
using DeBroglie.Topo;
using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DeBroglie.Trackers
{
    /// <summary>
    /// Tracks the banned/selected status of each tile with respect to a tileset.
    /// </summary>
    public class SelectedTracker : ITracker
    {
        private readonly TilePropagator tilePropagator;

        private readonly WavePropagator wavePropagator;

        private readonly TileModelMapping tileModelMapping;

        // Indexed by tile topology
        private readonly int[] patternCounts;

        private readonly TilePropagatorTileSet tileSet;

        internal SelectedTracker(TilePropagator tilePropagator, WavePropagator wavePropagator, TileModelMapping tileModelMapping, TilePropagatorTileSet tileSet)
        {
            this.tilePropagator = tilePropagator;
            this.wavePropagator = wavePropagator;
            this.tileModelMapping = tileModelMapping;
            this.tileSet = tileSet;
            patternCounts = new int[tilePropagator.Topology.IndexCount];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quadstate GetQuadstate(int index)
        {
            var selectedPatternCount = patternCounts[index];
            if (selectedPatternCount == 0)
                return Quadstate.No;

            tileModelMapping.GetTileCoordToPatternCoord(index, out var patternIndex, out var offset);

            var totalPatternCount = wavePropagator.Wave.GetPatternCount(patternIndex);
            if (totalPatternCount == selectedPatternCount)
            {
                return Quadstate.Yes;
            }
            return Quadstate.Maybe;
        }

        public bool IsSelected(int index)
        {
            return GetQuadstate(index).IsYes();
        }

        void ITracker.DoBan(int patternIndex, int pattern)
        {
            if(tileModelMapping.PatternCoordToTileCoordIndexAndOffset == null)
            {
                DoBan(patternIndex, pattern, patternIndex, 0);
            }
            else
            {
                foreach (var (p, index, offset) in tileModelMapping.PatternCoordToTileCoordIndexAndOffset.Get(patternIndex))
                {
                    DoBan(patternIndex, pattern, index, offset);
                }
            }
        }

        private void DoBan(int patternIndex, int pattern, int index, int offset)
        {
            var patterns = tileModelMapping.GetPatterns(tileSet, offset);
            if (patterns.Contains(pattern))
            {
                patternCounts[index] -= 1;
            }
        }

        void ITracker.Reset()
        {
            var wave = wavePropagator.Wave;
            foreach(var index in tilePropagator.Topology.GetIndices())
            {
                tileModelMapping.GetTileCoordToPatternCoord(index, out var patternIndex, out var offset);
                var patterns = tileModelMapping.GetPatterns(tileSet, offset);
                var count = 0;
                foreach (var p in patterns)
                {
                    if(wave.Get(patternIndex, p))
                    {
                        count++;
                    }
                }
                patternCounts[index] = count;
            }
        }


        void ITracker.UndoBan(int patternIndex, int pattern)
        {
            if (tileModelMapping.PatternCoordToTileCoordIndexAndOffset == null)
            {
                UndoBan(patternIndex, pattern, patternIndex, 0);
            }
            else
            {
                foreach (var (p, index, offset) in tileModelMapping.PatternCoordToTileCoordIndexAndOffset.Get(patternIndex))
                {
                    UndoBan(patternIndex, pattern, index, offset);
                }
            }
        }

        private void UndoBan(int patternIndex, int pattern, int index, int offset)
        {
            var patterns = tileModelMapping.GetPatterns(tileSet, offset);
            if (patterns.Contains(pattern))
            {
                patternCounts[index] += 1;
            }
        }
    }
}
