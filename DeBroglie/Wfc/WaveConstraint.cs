namespace DeBroglie.Wfc
{
    internal interface IWaveConstraint
    {
        CellStatus Init(WavePropagator wavePropagator);

        CellStatus Check(WavePropagator wavePropagator);
    }
}
