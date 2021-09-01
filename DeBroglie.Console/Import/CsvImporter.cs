using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DeBroglie.Console.Import
{
    public class CsvImporter : ISampleSetImporter
    {
        public SampleSet Load(string filename)
        {
            var data = new List<List<List<Tile>>>();
            data.Add(new List<List<Tile>>());
            foreach (var line in File.ReadLines(filename))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    data.Add(new List<List<Tile>>());
                    continue;
                }
                var row = line.Split(",").Select(Parse).ToList();
                data[data.Count - 1].Add(row);
            }

            var width = data[0][0].Count;
            var height = data[0].Count;
            var depth = data.Count;
            var dataArray = new Tile[width, height, depth];
            for(var x=0;x<width;x++)
            {
                for(var y=0;y<height;y++)
                {
                    for(var z=0;z<depth;z++)
                    {
                        dataArray[x, y, z] = data[z][y][x];
                    }
                }
            }

            var sample = TopoArray.Create(dataArray, false);

            return new SampleSet
            {
                Directions = DirectionSet.Cartesian3d,
                Samples = new[] { sample },
            };
        }

        public Tile Parse(string tile)
        {
            return new Tile(tile.Trim());
        }
    }
}
