using System;
using System.Drawing;
using System.Xml.Serialization;
using TiledLib.Layer;
using DeBroglie;
using System.IO;
using DeBroglie.MagicaVoxel;

namespace DeBroglie.Console
{

    class Program
    {
        static void Main(string[] args)
        {
            foreach (var arg in args)
            {
                ItemsProcessor.Process(arg);
            }
        }
    }
}
