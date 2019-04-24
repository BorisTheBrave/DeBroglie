using DeBroglie.Console.Config;
using DeBroglie.Models;

namespace DeBroglie.Console.Export
{
    public interface IExporter
    {
        void Export(TileModel model, TilePropagator tilePropagator, string filename, DeBroglieConfig config, ExportOptions exportOptions);
    }
}
