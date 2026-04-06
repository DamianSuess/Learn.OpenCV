using System.Collections.Generic;
using OpenCvSharp;

namespace VisionLedTest;

public class LightDetector
{
  public Dictionary<int, bool> DetectLights(Mat frame, IEnumerable<RoiDefinition> rois)
  {
    var results = new Dictionary<int, bool>();
    foreach (var roi in rois)
    {
      Mat sub = new Mat(frame, roi.Region);

      // Convert to grayscale
      Mat gray = new();
      Cv2.CvtColor(sub, gray, ColorConversionCodes.BGR2GRAY);

      // Threshold to find bright spots
      Mat thresh = new();
      Cv2.Threshold(gray, thresh, 200, 255, ThresholdTypes.Binary);

      // Count white pixels
      int whitePixels = Cv2.CountNonZero(thresh);

      // Light detected if enough bright pixels
      bool lightOn = whitePixels > 50;

      results[roi.Id] = lightOn;

      gray.Dispose();
      thresh.Dispose();
      sub.Dispose();
    }

    return results;
  }
}
