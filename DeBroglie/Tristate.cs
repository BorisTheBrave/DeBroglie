using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie
{
    internal enum Tristate
    {
        No = -1,
        Maybe = 0,
        Yes = 1,
    }

    internal static class TristateExtensions
    {
        public static bool IsYes(this Tristate v) => v == Tristate.Yes;
        public static bool IsMaybe(this Tristate v) => v == Tristate.Maybe;
        public static bool IsNo(this Tristate v) => v == Tristate.No;
        public static bool Impossible(this Tristate v) => v == Tristate.No;
        public static bool Possible(this Tristate v) => v != Tristate.No;
    }
}
