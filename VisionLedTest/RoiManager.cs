using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OpenCvSharp;

namespace VisionLedTest;

public class RoiManager
{
  private readonly List<RoiDefinition> _rois = [];

  public void AddRoi(Rect rect) =>
    _rois.Add(new RoiDefinition(_rois.Count, rect));

  public void AddRoi(int id, int x, int y, int width, int height) =>
    _rois.Add(new RoiDefinition(id, new OpenCvSharp.Rect(x, y, width, height)));

  public void Clear() =>
    _rois.Clear();

  public IReadOnlyList<RoiDefinition> GetRois() => _rois;

  public void SetRois(IEnumerable<RoiDefinition> rois)
  {
    _rois.Clear();
    _rois.AddRange(rois);
  }

  public static List<RoiDefinition> GenerateGrid(int rows, int cols, int width, int height)
  {
    var list = new List<RoiDefinition>();
    int id = 0;

    int cellW = width / cols;
    int cellH = height / rows;

    for (int r = 0; r < rows; r++)
    {
      for (int c = 0; c < cols; c++)
      {
        var rect = new Rect(c * cellW, r * cellH, cellW, cellH);
        list.Add(new RoiDefinition(id++, rect));
      }
    }

    return list;
  }

  public static void Save(string filePath, IEnumerable<RoiDefinition> rois)
  {
    var json = JsonSerializer.Serialize(rois, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(filePath, json);
  }

  public static List<RoiDefinition> Load(string filePath)
  {
    var json = File.ReadAllText(filePath);
    try
    {
      return JsonSerializer.Deserialize<List<RoiDefinition>>(json)!;
    }
    catch
    {
      return [];
    }
  }
}
