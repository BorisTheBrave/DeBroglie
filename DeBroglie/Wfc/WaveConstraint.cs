namespace DeBroglie.Wfc
{
    /// <summary>
    /// A variant of ITileConstraint that works on patterns instead of tiles.
    /// </summary>
    internal interface IWaveConstraint
    {
        Resolution Init(WavePropagator wavePropagator);

        Resolution Check(WavePropagator wavePropagator);
    }
}
