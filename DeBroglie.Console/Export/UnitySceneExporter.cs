using DeBroglie.Console.Config;
using DeBroglie.Console.Export;
using DeBroglie.Models;
using DeBroglie.Tiled;
using DeBroglie.Topo;
using System.IO;
using TiledLib;
using TiledLib.Layer;

namespace DeBroglie.Console.Export
{

    public class UnitySceneExporter : IExporter
    {
        public void Export(TileModel model, TilePropagator tilePropagator, string filename, DeBroglieConfig config, ExportOptions exportOptions)
        {
            var topoArray = tilePropagator.ToArray(new Tile("?"), new Tile("!"));
            var topology = topoArray.Topology;

            using (var s = File.Open(filename, FileMode.Create))
            using (var tw = new StreamWriter(s))
            {
                tw.WriteLine("%YAML 1.1");
                tw.WriteLine("%TAG !u! tag:unity3d.com,2011:");
                int id = 1;
                foreach(var index in topology.GetIndices())
                {
                    var objectId = id++;
                    var transformId = id++;
                    topology.GetCoord(index, out var x, out var y, out var z);
                    var str = @"
--- !u!1 &objectId
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: transformId}
  m_Layer: 0
  m_Name: objectName
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &transformId
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: objectId}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: transformX, y: transformY, z: transformZ}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_Children: []
  m_Father: {fileID: 0}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
";
                    str = str
                        .Trim()
                        .Replace("objectId", objectId.ToString())
                        .Replace("transformId", transformId.ToString())
                        .Replace("transformX", x.ToString())
                        .Replace("transformY", y.ToString())
                        .Replace("transformZ", z.ToString())
                        .Replace("objectName", topoArray.Get(index).Value?.ToString())
                        ;
                    tw.WriteLine(str);
                }
            }
        }
    }
}
