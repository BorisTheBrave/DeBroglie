using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie
{
    internal struct Tristate
    {
        private int v;

        private Tristate(int v)
        {
            this.v = v;
        }

        public bool IsYes => v == 1;
        public bool IsMaybe=> v == 0;
        public bool IsNo => v == -1;
        public bool Impossible => v == -1;
        public bool Possible => v != -1;

        public static Tristate operator &(Tristate a, Tristate b)
        {
            return new Tristate(Math.Min(a.v, b.v));
        }

        public static Tristate operator |(Tristate a, Tristate b)
        {
            return new Tristate(Math.Max(a.v, b.v));
        }

        public static Tristate operator !(Tristate a)
        {
            return new Tristate(-a.v);
        }

        public static readonly Tristate No = new Tristate(-1);
        public static readonly Tristate Maybe = new Tristate(0);
        public static readonly Tristate Yes = new Tristate(1);
    }
}
