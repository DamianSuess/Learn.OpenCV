using OpenCvSharp;

namespace VisionLedTest;

public class RoiDefinition(int id, Rect region)
{
  public int Id { get; set; } = id;

  public Rect Region { get; set; } = region;
}
