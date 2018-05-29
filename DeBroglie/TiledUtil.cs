using System.IO;
using TiledLib;
using TiledLib.Layer;

namespace DeBroglie
{
    public static class TiledUtil
    {
        public static Map Load(string filename)
        {
            using (var s = File.OpenRead(filename))
            {
                return Map.FromStream(s, ts => File.OpenRead(Path.Combine(Path.GetDirectoryName(filename), ts.source)));
            }
        }

        public static void Save(string filename, Map map)
        {
            using (var stream = File.OpenWrite(filename))
            using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 1024, true))
            {
                writer.WriteTmxMap(map);
            }
        }

        public static int[,] AsIntArray(TileLayer layer)
        {
            var layerArray = new int[layer.Width, layer.Height];
            var i = 0;
            for (int y = 0; y < layer.Height; y++)
            {
                for (int x = 0; x < layer.Width; x++)
                {
                    layerArray[x, y] = layer.Data[i++];
                }
            }
            return layerArray;
        }

        public static TileLayer AsLayer(int[,] layerArray)
        {
            var data = new int[layerArray.GetLength(0) * layerArray.GetLength(1)];
            var i = 0;
            for (int y = 0; y < layerArray.GetLength(1); y++)
            {
                for (int x = 0; x < layerArray.GetLength(0); x++)
                {
                    data[i++] = layerArray[x, y];
                }
            }
            var layer = new TileLayer();
            layer.Encoding = "base64";
            layer.Data = data;
            layer.Width = layerArray.GetLength(0);
            layer.Height = layerArray.GetLength(1);
            layer.Visible = true;
            layer.Opacity = 1.0;
            return layer;
        }
    }
}
