using System;
using OpenCvSharp;

namespace VisionLedTest;

public static class LedDetectDemo
{
  // --- ROI (designated area) in pixels. Adjust these for your setup. ---
  // Tip: Run once, look at the preview, then tweak.
  private static Rect RoiRect = new Rect(X: 100, Y: 100, Width: 400, Height: 300);

  // --- Detection tuning knobs ---
  private static int BrightThreshold = 220;     // 0..255. Increase if false positives.

  private static int MinBlobArea = 30;          // Minimum contour area to be considered an LED
  private static int MaxBlobArea = 5000;        // Maximum contour area to ignore large reflections
  private static int MorphKernelSize = 3;       // Morphological cleanup kernel

  // Log state: 0 = none, 1 = exactly one, 2 = more than one
  private static int _lastState = -1;

  public static void MainTest()
  {
    using var capture = new VideoCapture(0);
    if (!capture.IsOpened())
    {
      Console.WriteLine("ERROR: Could not open webcam.");
      return;
    }

    // Optional: set camera resolution
    capture.FrameWidth = 1280;
    capture.FrameHeight = 720;

    using var frame = new Mat();

    Console.WriteLine("Press ESC to quit.");
    Console.WriteLine($"ROI: {RoiRect} | BrightThreshold={BrightThreshold}, MinBlobArea={MinBlobArea}");

    while (true)
    {
      capture.Read(frame);
      if (frame.Empty())
        continue;

      // Ensure ROI stays inside the frame bounds
      var safeRoi = ClampRectToFrame(RoiRect, frame.Width, frame.Height);
      if (safeRoi.Width <= 0 || safeRoi.Height <= 0)
      {
        Console.WriteLine("ERROR: ROI is out of bounds. Adjust RoiRect.");
        break;
      }

      // --- Process ROI only ---
      using var roiBgr = new Mat(frame, safeRoi);
      int ledCount = CountLitLeds(roiBgr, out var ledCenters);

      // --- Log transitions ---
      LogStateTransitions(ledCount);

      // --- Visualization overlay ---
      DrawOverlay(frame, safeRoi, ledCenters, ledCount);

      Cv2.ImShow("LED Detector", frame);

      // ESC to quit
      int key = Cv2.WaitKey(1);
      if (key == 27) break;

      // Optional quick tuning with keys:
      // Up/Down adjust threshold, +/- adjust min area
      if (key == 2490368) BrightThreshold = Math.Min(255, BrightThreshold + 1); // Up arrow
      if (key == 2621440) BrightThreshold = Math.Max(0, BrightThreshold - 1);   // Down arrow
      if (key == '+') MinBlobArea += 5;
      if (key == '-') MinBlobArea = Math.Max(1, MinBlobArea - 5);
    }

    Cv2.DestroyAllWindows();
  }

  private static int CountLitLeds(Mat roiBgr, out Point[] ledCenters)
  {
    // Convert to grayscale
    using var gray = new Mat();
    Cv2.CvtColor(roiBgr, gray, ColorConversionCodes.BGR2GRAY);

    // Blur to reduce sensor noise
    using var blurred = new Mat();
    Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);

    // Threshold for bright spots
    using var binary = new Mat();
    Cv2.Threshold(blurred, binary, BrightThreshold, 255, ThresholdTypes.Binary);

    // Morphological cleanup (open then close)
    using var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse,
        new Size(MorphKernelSize, MorphKernelSize));

    using var opened = new Mat();
    Cv2.MorphologyEx(binary, opened, MorphTypes.Open, kernel);

    using var closed = new Mat();
    Cv2.MorphologyEx(opened, closed, MorphTypes.Close, kernel);

    // Find contours
    Cv2.FindContours(closed, out Point[][] contours, out HierarchyIndex[] _,
        RetrievalModes.External, ContourApproximationModes.ApproxSimple);

    // Filter contours to get LED candidates
    var centers = new System.Collections.Generic.List<Point>();

    foreach (var c in contours)
    {
      double area = Cv2.ContourArea(c);
      if (area < MinBlobArea || area > MaxBlobArea)
        continue;

      // Optional: Reject long skinny shapes (reflections)
      var rect = Cv2.BoundingRect(c);
      double aspect = rect.Width / (double)rect.Height;
      if (aspect < 0.2 || aspect > 5.0)
        continue;

      // Compute center via moments
      var m = Cv2.Moments(c);
      if (Math.Abs(m.M00) < 1e-5)
        continue;

      int cx = (int)(m.M10 / m.M00);
      int cy = (int)(m.M01 / m.M00);
      centers.Add(new Point(cx, cy));
    }

    ledCenters = centers.ToArray();
    return ledCenters.Length;
  }

  private static void LogStateTransitions(int ledCount)
  {
    int state = ledCount switch
    {
      0 => 0,
      1 => 1,
      _ => 2
    };

    if (state == _lastState) return; // No transition -> no log spam

    // Timestamp: local time (change to UtcNow if you prefer)
    string ts = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");

    if (state == 1)
    {
      Console.WriteLine($"[{ts}] Exactly ONE LED is ON");
    }
    else if (state == 2)
    {
      Console.WriteLine($"[{ts}] MULTIPLE LEDs are ON (count >= 2)");
    }
    else
    {
      Console.WriteLine($"[{ts}] No LEDs are ON");
    }

    _lastState = state;
  }

  private static void DrawOverlay(Mat frame, Rect roi, Point[] ledCenters, int ledCount)
  {
    // Draw ROI rectangle
    Cv2.Rectangle(frame, roi, new Scalar(0, 255, 255), 2);

    // Draw detected LED centers (convert ROI-relative to frame coords)
    foreach (var p in ledCenters)
    {
      var pf = new Point(p.X + roi.X, p.Y + roi.Y);
      Cv2.Circle(frame, pf, 6, new Scalar(0, 255, 0), 2);
    }

    // Text
    string text = $"LEDs ON: {ledCount} | Thr={BrightThreshold} | MinArea={MinBlobArea}";
    Cv2.PutText(frame, text, new Point(10, 30),
        HersheyFonts.HersheySimplex, 0.8, new Scalar(255, 255, 255), 2);
  }

  private static Rect ClampRectToFrame(Rect r, int frameWidth, int frameHeight)
  {
    int x = Math.Max(0, r.X);
    int y = Math.Max(0, r.Y);
    int w = Math.Min(r.Width, frameWidth - x);
    int h = Math.Min(r.Height, frameHeight - y);
    return new Rect(x, y, w, h);
  }
}
