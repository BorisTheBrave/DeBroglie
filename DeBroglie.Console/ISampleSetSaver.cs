using DeBroglie.Console.Export;
using DeBroglie.Models;

namespace DeBroglie.Console
{
    public interface ISampleSetSaver
    {
        void Save(TileModel model, TilePropagator tilePropagator, string filename, DeBroglieConfig config, ExportOptions exportOptions);

    }
}
