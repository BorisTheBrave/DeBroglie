using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie
{
    /// <summary>
    /// Represents an uncertain boolean.
    /// </summary>
    public enum Quadstate
    {
        Contradiction = -2,
        No = -1,
        Maybe = 0,
        Yes = 1,
    }

    public static class QuadstateExtensions
    {
        public static bool IsYes(this Quadstate v) => v == Quadstate.Yes;
        public static bool IsMaybe(this Quadstate v) => v == Quadstate.Maybe;
        public static bool IsNo(this Quadstate v) => v == Quadstate.No;
        public static bool IsContradiction(this Quadstate v) => v == Quadstate.Contradiction;

        public static bool Possible(this Quadstate v) => (int)v >= 0;
    }
}
