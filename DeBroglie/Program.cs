using System;

namespace DeBroglie
{

    class Program
    {
        static void Main(string[] args)
        {
            int[,] sample =
            {
                { 0, 0, 1, 0, 0, 0 },
                { 0, 0, 1, 0, 0, 0 },
                { 0, 0, 1, 1, 1, 1 },
                { 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0 },
            };

            var model = new OverlappingModel(sample, 3, false, 8);

            var propagator = new WavePropagator(model, 10, 10, true);

            var status = propagator.Run();

            var results = model.ToArray(propagator);


            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    var r = results[x, y];
                    string c;
                    switch(r)
                    {
                        case (int)CellStatus.Undecided: c = "?"; break;
                        case (int)CellStatus.Contradiction: c = "*"; break;
                        case 0: c = " "; break;
                        default: c = r.ToString(); break;
                    }
                    Console.Write(c);
                }
                Console.WriteLine();
            }
        }
    }
}
