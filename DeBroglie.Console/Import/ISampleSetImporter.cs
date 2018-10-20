namespace DeBroglie.Console.Import
{
    public interface ISampleSetImporter
    {
        SampleSet Load(string filename);

        Tile Parse(string tile);
    }
}
