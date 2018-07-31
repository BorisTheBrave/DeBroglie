namespace DeBroglie
{
    public class PatternModel
    {
        /**
          * propagator[pattern1][direction] contains all the patterns that can be placed in
          * next to pattern1 in the direction direction.
          */
        public int[][][] Propagator { get; set; }

        /**
         * Stores the desired relative frequencies of each pattern
         */
        public double[] Frequencies { get; set; }

        public int PatternCount => Frequencies.Length;
    }
}
