using DeBroglie.Models;
using DeBroglie.Topo;
using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Trackers
{
    public interface IQuadstateChanged
    {
        void Reset(SelectedChangeTracker tracker);

        void Notify(int index, Quadstate before, Quadstate after);
    }

    /// <summary>
    /// Runs a callback when the banned/selected status of tile changes with respect to a tileset.
    /// </summary>
    public class SelectedChangeTracker : ITracker
    {
        private readonly TilePropagator tilePropagator;

        private readonly WavePropagator wavePropagator;

        private readonly TileModelMapping tileModelMapping;

        // Indexed by tile topology
        private readonly int[] patternCounts;

        private readonly Quadstate[] values;

        private readonly TilePropagatorTileSet tileSet;

        private readonly IQuadstateChanged onChange;

        internal SelectedChangeTracker(TilePropagator tilePropagator, WavePropagator wavePropagator, TileModelMapping tileModelMapping, TilePropagatorTileSet tileSet, IQuadstateChanged onChange)
        {
            this.tilePropagator = tilePropagator;
            this.wavePropagator = wavePropagator;
            this.tileModelMapping = tileModelMapping;
            this.tileSet = tileSet;
            this.onChange = onChange;
            patternCounts = new int[tilePropagator.Topology.IndexCount];
            values = new Quadstate[tilePropagator.Topology.IndexCount];
        }

        private Quadstate GetQuadstateInner(int index)
        {
            var selectedPatternCount = patternCounts[index];

            tileModelMapping.GetTileCoordToPatternCoord(index, out var patternIndex, out var offset);

            var totalPatternCount = wavePropagator.Wave.GetPatternCount(patternIndex);

            if (totalPatternCount == 0)
            {
                return Quadstate.Contradiction;
            }
            else if (selectedPatternCount == 0)
            {
                return Quadstate.No;
            }
            else if (totalPatternCount == selectedPatternCount)
            {
                return Quadstate.Yes;
            }
            else
            {
                return Quadstate.Maybe;
            }
        }

        public Quadstate GetQuadstate(int index)
        {
            return values[index];
        }

        public bool IsSelected(int index)
        {
            return GetQuadstate(index).IsYes();
        }

        public void DoBan(int patternIndex, int pattern)
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
            DoNotify(index);
        }

        public void Reset()
        {
            var wave = wavePropagator.Wave;
            foreach(var index in tilePropagator.Topology.GetIndices())
            {
                tileModelMapping.GetTileCoordToPatternCoord(index, out var patternIndex, out var offset);
                var patterns = tileModelMapping.GetPatterns(tileSet, offset);
                var count = 0;
                foreach (var p in patterns)
                {
                    if(patterns.Contains(p) && wave.Get(patternIndex, p))
                    {
                        count++;
                    }
                }
                patternCounts[index] = count;
                values[index] = GetQuadstateInner(index);
            }
            onChange.Reset(this);
        }


        public void UndoBan(int patternIndex, int pattern)
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
            DoNotify(index);
        }

        private void DoNotify(int index)
        {
            var newValue = GetQuadstateInner(index);
            var oldValue = values[index];
            if (newValue != oldValue)
            {
                values[index] = newValue;
                onChange.Notify(index, oldValue, newValue);
            }
        }
    }
}
