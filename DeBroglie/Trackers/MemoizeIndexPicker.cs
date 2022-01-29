using DeBroglie.Wfc;
using System;

namespace DeBroglie.Trackers
{
    internal class MemoizeIndexPicker : IIndexPicker, IChoiceObserver
    {
        private Deque<int> prevChoices;
        private Deque<int> futureChoices;
        private readonly IIndexPicker underlying;

        public MemoizeIndexPicker(IIndexPicker underlying)
        {
            this.underlying = underlying;
        }

        public void Init(WavePropagator wavePropagator)
        {
            wavePropagator.AddChoiceObserver(this);
            futureChoices = new Deque<int>();
            prevChoices = new Deque<int>();
        }

        public void Backtrack()
        {
            futureChoices.Shift(prevChoices.Pop());
        }

        public int GetRandomIndex(Func<double> randomDouble)
        {
            if (futureChoices.Count > 0)
            {
                return futureChoices.Unshift();
            }
            else
            {
                return underlying.GetRandomIndex(randomDouble);
            }
        }

        public void MakeChoice(int index, int pattern)
        {
            prevChoices.Push(index);
        }
    }
}
