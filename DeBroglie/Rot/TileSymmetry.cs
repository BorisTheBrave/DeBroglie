namespace DeBroglie.Rot
{
    /// <summary>
    /// Specifies the way in which a tile can be symmetric.
    /// The letters are chosen that they have the letter itself
    /// has the symmetry group it represents.
    /// </summary>
    public enum TileSymmetry
    {
        /// <summary>
        /// No symmetry
        /// </summary>
        F = 0,
        None = 0,
        /// <summary>
        /// Fully symmetric
        /// </summary>
        X,
        /// <summary>
        /// Reflectable on y-axis
        /// </summary>
        T,
        /// <summary>
        /// Reflectable on x-axis and y-axis
        /// </summary>
        I,
        /// <summary>
        /// Reflectable on one diagonal
        /// </summary>
        L,
        /// <summary>
        /// Reflectable on both diagonals.
        /// </summary>
        Slash,
        /// <summary>
        /// Reflectable on other diagonal
        /// </summary>
        Q,
        /// <summary>
        /// Can rotate 180 degrees
        /// </summary>
        N,
        /// <summary>
        /// Reflectable on x-axis
        /// </summary>
        E,
        /// <summary>
        /// Any rotation, but no reflection.
        /// There's no keyboard symbol that corresponds to this!
        /// </summary>
        Cyclic
    }

    public static class TileSymmetryUtils
    {
        public static TileSymmetry Parse(string ts)
        {
            switch(ts)
            {
                case "F":
                    return TileSymmetry.F;
                case "X":
                    return TileSymmetry.X;
                case "T":
                    return TileSymmetry.T;
                case "I":
                    return TileSymmetry.I;
                case "L":
                    return TileSymmetry.L;
                case "/":
                case "\\":
                    return TileSymmetry.Slash;
                case "N":
                    return TileSymmetry.N;
                case "E":
                    return TileSymmetry.E;
                case "Q":
                    return TileSymmetry.Q;
            }
            switch(ts.ToLower())
            {
                case "none":
                    return TileSymmetry.F;
                case "full":
                    return TileSymmetry.X;
                case "cyclic":
                    return TileSymmetry.Cyclic;
            }
            throw new System.Exception($"Cannot parse {ts} as a TileSymmetry.");
        }
    }
}
