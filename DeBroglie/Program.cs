using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace DeBroglie
{
    class Program
    {
        private static void Write(OverlappingModel<int> model, WavePropagator propagator)
        {
            var results = model.ToArray(propagator, -1, -2);

            for (var y = 0; y < results.GetLength(1); y++)
            {
                for (var x = 0; x < results.GetLength(0); x++)
                {
                    var r = results[x, y];
                    string c;
                    switch (r)
                    {
                        case -1: c = "?"; break;
                        case -2: c = "*"; break;
                        case 0: c = " "; break;
                        default: c = r.ToString(); break;
                    }
                    Console.Write(c);
                }
                Console.WriteLine();
            }
        }

        private static void WriteSteps(OverlappingModel<int> model, WavePropagator propagator)
        {
            Write(model, propagator);
            Console.WriteLine();

            while (true)
            {
                var prevBacktrackCount = propagator.BacktrackCount;
                var status = propagator.Step();
                Write(model, propagator);
                if(propagator.BacktrackCount != prevBacktrackCount)
                {
                    Console.WriteLine("Backtracked!");
                }
                Console.WriteLine();

                if (status != CellStatus.Undecided)
                {
                    Console.WriteLine(status);
                    break;
                }
            }
        }

        private static CellStatus Run(WavePropagator propagator, int retries)
        {
            CellStatus status = CellStatus.Undecided;
            for (var retry = 0; retry < retries; retry++)
            {
                status = propagator.Run();
                if (status == CellStatus.Decided)
                {
                    break;
                }
            }
            return status;
        }


        static void Main(string[] args)
        {
            SamplesProcessor.Process();
            return;

            int[,] sample =
            {
                { 0, 0, 1, 0, 0, 0 },
                { 0, 0, 1, 0, 0, 0 },
                { 0, 0, 1, 1, 1, 1 },
                { 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0 },
            };

            var model = new OverlappingModel<int>(sample, 3, false, 8);

            var pathConstraint = PathConstraint.Create(model, new[] { 1 }, new[]{
                new Point(0, 0),
                new Point(9, 9),
            });

            var propagator = new WavePropagator(model, 10, 10, false, true, new[] { pathConstraint });

            var status = Run(propagator, 1);
            Write(model, propagator);

            Console.WriteLine("Backtrack count {0}", propagator.BacktrackCount);
        }
    }
}
