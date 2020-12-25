namespace DeBroglie.Wfc
{
    internal class PatternModel
    {
        /**
          * propagator[pattern1][edgeLabel] contains all the patterns that can be placed in
          * next to pattern1 according to the given edge label.
          * NB: For grid topologies edge label corresponds to the direction.
          */
        public int[][][] Propagator { get; set; }

        /**
         * Stores the desired relative frequencies of each pattern
         */
        public double[] Frequencies { get; set; }

        public int PatternCount => Frequencies.Length;
    }
}
