using System.Collections.Generic;
using OpenCvSharp;

namespace VisionLedTest;

public class RoiManager
{
  private readonly List<RoiDefinition> _rois = [];

  public void AddRoi(Rect rect) =>
    _rois.Add(new RoiDefinition(_rois.Count, rect));

  public void AddRoi(int id, int x, int y, int width, int height) =>
    _rois.Add(new RoiDefinition(id, new OpenCvSharp.Rect(x, y, width, height)));

  public IEnumerable<RoiDefinition> GetRois() => _rois;
}
