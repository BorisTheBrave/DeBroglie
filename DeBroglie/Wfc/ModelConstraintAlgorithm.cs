namespace DeBroglie.Wfc
{
    public enum ModelConstraintAlgorithm
    {
        /// <summary>
        /// Equivalent to Ac4 currently.
        /// </summary>
        Default,
        /// <summary>
        /// Use the Arc Consistency 4 algorithm.
        /// </summary>
        Ac4,
        /// <summary>
        /// Use the Arc Consistency 3 algorithm.
        /// </summary>
        Ac3,
        /// <summary>
        /// Only update tiles immediately adjacent to updated tiles. 
        /// </summary>
        OneStep,
    }
}
