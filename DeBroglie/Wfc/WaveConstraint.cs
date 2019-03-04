namespace DeBroglie.Wfc
{
    /// <summary>
    /// A variant of ITileConstraint that works on patterns instead of tiles.
    /// </summary>
    internal interface IWaveConstraint
    {
        void Init(WavePropagator wavePropagator);

        void Check(WavePropagator wavePropagator);
    }
}
