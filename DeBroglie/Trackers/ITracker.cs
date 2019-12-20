using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Trackers
{
    interface ITracker
    {
        void Reset();

        void DoBan(int index, int pattern);

        void UndoBan(int index, int pattern);
    }
}
