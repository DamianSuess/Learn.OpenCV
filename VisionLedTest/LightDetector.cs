using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace VisionLedTest;

public class LightDetector
{
  #region ROI Selection / Mapping

  private readonly object _roiLock = new();

  /////// <summary>Test ROI (tiny).</summary>
  ////private Rect _roi = new Rect(X: 100, Y: 100, Width: 500, Height: 350);

  /// <summary>Whole "table top".</summary>
  private Rect _roi = new Rect(X: 48, Y: 254, Width: 911, Height: 990);

  #endregion ROI Selection / Mapping

  private int _brightThreshold = 240;

  /// <summary>Store latest frame size for accurate mapping from PictureBox -> image coords.</summary>
  private int _lastFrameWidth = 0;

  /// <summary>Store latest frame size for accurate mapping from PictureBox -> image coords.</summary>
  private int _lastFrameHeight = 0;

  /// <summary>Count of discovered LEDs (default -1 to auto-trigger).</summary>
  private int _lastLedCount = -1;

  /// <summary>Ignore tiny specks.</summary>
  /// <remarks>30.</remarks>
  private int _minBlobArea = 30;

  /// <summary>Ignore huge reflections.</summary>
  private int _maxBlobArea = 8000;

  /// <summary>Cleanup kernel size.</summary>
  private int _morphKernelSize = 3;

  /// <summary>Range: 0 - 225.</summary>
  /// <remarks>230.</remarks>
  public int BrightThreshold
  {
    get => _brightThreshold;
    set => _brightThreshold = Math.Clamp(value, 0, 255);
  }

  /// <summary>Gets or sets the Maximum Blob Area Matrix.</summary>
  public int BlobAreaMax
  {
    get => _maxBlobArea;
    set => _maxBlobArea = Math.Max(1, value);
  }

  /// <summary>Gets or sets the Minimum Blob Area Matrix.</summary>
  public int BlobAreaMin
  {
    get => _minBlobArea;
    set => _minBlobArea = Math.Max(1, value);
  }

  /// <summary>Gets or sets the selected camera index.</summary>
  public int CameraIndex { get; set; } = 0;

  /// <summary>Gets the last captured matrix frame width.</summary>
  public int LastFrameWidth => _lastFrameWidth;

  /// <summary>Gets the last captured matrix frame height.</summary>
  public int LastFrameHeight => _lastFrameHeight;

  public int MorphKernelSize
  {
    get => _morphKernelSize;
    set => _morphKernelSize = Math.Max(1, value);
  }

  /// <summary>Gets or sets indicating whether OpenCV has captured a frame in memory or not yet.</summary>
  public bool HasCachedFrame => _lastFrameWidth > 0 && _lastFrameHeight > 0;

  public RoiManager RoiManager { get; } = new();

  /// <summary>Analysis Algorithm 'C' - Multi-ROI Detector.</summary>
  /// <param name="frame">Frame to analyze.</param>
  /// <param name="rois">Collection of <see cref="RoiDefinition"/>.</param>
  /// <returns></returns>
  public Dictionary<int, bool> DetectLights(Mat frame, IEnumerable<RoiDefinition> rois)
  {
    var results = new Dictionary<int, bool>();

    foreach (var roi in rois)
    {
      Mat sub = new(frame, roi.Region);

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

  /// <summary>Algorithm A.</summary>
  /// <param name="frame">Frame to analyze.</param>
  /// <param name="isGrayscale">Is input image color or grayscale.</param>
  /// <returns>Returns index of ROI and bool indicating if On:True or Off:False.</returns>
  public Dictionary<int, bool> AnalyzeFrame(Mat frame, IEnumerable<RoiDefinition>? rois = null, bool isSrcGrayscale = false)
  {
    var results = new Dictionary<int, bool>();

    // Update frame size for mapping ROI selections
    _lastFrameWidth = frame.Width;
    _lastFrameHeight = frame.Height;

    // Read ROI thread-safely and clamp to frame
    Rect roi;
    lock (_roiLock)
      roi = _roi;

    // Clamp ROI to the frame's Bounds
    var safeRoi = ClampRectToFrame(_roi, frame.Width, frame.Height);

    // Fallback region of interest
    if (safeRoi.Width <= 0 || safeRoi.Height <= 0)
      safeRoi = new Rect(0, 0, frame.Width, frame.Height);

    // Process ROI and detect LED rectangles
    int ledCount = 0;
    List<Rect> ledRects;
    ledRects = DetectLedRects(frame, safeRoi, out ledCount, isSrcGrayscale);

    // Log state transitions only
    LogLedCountTransitions(ledCount);

    // Draw overlays: ROI + LED rectangles
    DrawOverlay(frame, safeRoi, ledRects, ledCount, isBgColor: true);

    ////// Push to UI (PictureBox)
    ////UpdatePreview(frame);

    return results;
  }

  /// <summary>Gets a list of OpenCV Camera Indexes.</summary>
  /// <returns>Collection of Camera Indexes (default=0).</returns>
  /// <remarks>Can cause a 5 second delay.</remarks>
  public List<int> GetCameraList()
  {
    // NOTE for OpenCVSharp's VideoCaptureAPIs
    //  * 700 (70x) points to DirectShow (DSHOW) :: camera_id + domain_offset (1+700)
    //  * 1400 (140x) to MSMF

    List<int> cameraIndexes = [];
    int cameraCount = 0;

    while (true)
    {
      using (var cap = new VideoCapture(cameraCount))
      {
        if (!cap.IsOpened())
          break;

        cameraIndexes.Add(cameraCount);
        cameraCount++;
      }
    }

    Console.WriteLine($"Detected {cameraCount} camera(s)");
    return cameraIndexes;
  }

  public Rect ClampRectToFrame(Rect r, int frameWidth, int frameHeight)
  {
    int x = Math.Max(0, r.X);
    int y = Math.Max(0, r.Y);
    int w = Math.Min(r.Width, frameWidth - x);
    int h = Math.Min(r.Height, frameHeight - y);
    ////return new Rect(x, y, w, h);
    return new Rect(x, y, Math.Max(0, w), Math.Max(0, h));
  }

  internal void SetRoi(Rect value)
  {
    _roi = value;
  }

  private List<Rect> DetectLedRects(
    Mat frameBgr,
    Rect roi,
    out int ledCount,
    bool isSrcGrayscale = false)
  {
    // Work on ROI only
    using var roiBgr = new Mat(frameBgr, roi);

    // Convert to grayscale if needed
    using var blurred = new Mat();
    if (!isSrcGrayscale)
    {
      using var gray = new Mat();
      Cv2.CvtColor(roiBgr, gray, ColorConversionCodes.BGR2GRAY);

      // https://docs.opencv.org/4.x/d4/d13/tutorial_py_filtering.html
      Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 0);
    }
    else
    {
      Cv2.GaussianBlur(roiBgr, blurred, new OpenCvSharp.Size(5, 5), 0);
    }

    // Threshold for bright pixels (LEDs)
    using var binary = new Mat();
    Cv2.Threshold(blurred, binary, _brightThreshold, 255, ThresholdTypes.Binary);

    // Morphological cleanup
    using var kernel = Cv2.GetStructuringElement(
      MorphShapes.Ellipse,
      new OpenCvSharp.Size(_morphKernelSize, _morphKernelSize));

    using var opened = new Mat();
    Cv2.MorphologyEx(binary, opened, MorphTypes.Open, kernel);

    using var closed = new Mat();
    Cv2.MorphologyEx(opened, closed, MorphTypes.Close, kernel);

    // Find blobs
    Cv2.FindContours(closed, out OpenCvSharp.Point[][] contours, out _,
      RetrievalModes.External, ContourApproximationModes.ApproxSimple);

    var rects = new List<Rect>();

    foreach (var c in contours)
    {
      double area = Cv2.ContourArea(c);
      if (area < _minBlobArea || area > _maxBlobArea)
        continue;

      var r = Cv2.BoundingRect(c);

      // Optional:
      // Reject overly skinny shapes (glare streaks)
      double aspect = r.Width / (double)Math.Max(1, r.Height);
      if (aspect < 0.2 || aspect > 5.0)
        continue;

      // Convert ROI-local rect to full-frame coordinates
      var rf = new Rect(r.X + roi.X, r.Y + roi.Y, r.Width, r.Height);
      rects.Add(rf);
    }

    ledCount = rects.Count;
    return rects;
  }

  /// <summary>
  /// Draws an overlay on the specified image frame, including the region of interest (ROI), detected LED rectangles,
  /// and status information.
  /// </summary>
  /// <remarks>The overlay includes a highlighted ROI, bounding boxes for detected LEDs, and status text with
  /// detection parameters. The drawing style of the LED rectangles depends on the value of <paramref
  /// name="isBgColor"/>. This method modifies the input frame in place.</remarks>
  /// <param name="frame">The image frame on which to draw the overlay. Must not be null.</param>
  /// <param name="roi">The region of interest within the frame to highlight. Specifies the area being analyzed for LEDs.</param>
  /// <param name="ledRects">A list of rectangles representing the detected LED positions within the frame. Each rectangle is drawn as part of
  /// the overlay.</param>
  /// <param name="ledCount">The number of detected LEDs to display in the status text.</param>
  /// <param name="isBgColor">If <see langword="true"/>, draws LED rectangles in a color suitable for background images; otherwise, uses black
  /// and white for binary images.</param>
  private void DrawOverlay(Mat frame, Rect roi, List<Rect> ledRects, int ledCount, bool isBgColor = false)
  {
    // ROI in yellow
    Cv2.Rectangle(frame, roi, new Scalar(0, 255, 255), 2);

    if (isBgColor)
    {
      // LED boxes in Magic Pink
      foreach (var r in ledRects)
        Cv2.Rectangle(frame, r, new Scalar(255, 0, 255), 2);
    }
    else
    {
      // NOTE: When displaying as 'Binary' (black/white), you MUST draw in black/white
      foreach (var r in ledRects)
        Cv2.Rectangle(frame, r, new Scalar(0), 3);
    }

    // Status text
    string roiCoords = $"[{roi.X},{roi.Y},{roi.Width},{roi.Height}]";
    string status = $"LEDs ON: {ledCount}  Thr={_brightThreshold}  MinArea={_minBlobArea}  ROI={roiCoords}";
    Cv2.PutText(frame, status, new OpenCvSharp.Point(10, 30),
      HersheyFonts.HersheySimplex, 0.8, new Scalar(255, 255, 255), 2);

    // Helpful hint text
    Cv2.PutText(frame, "Drag on the preview to set ROI", new OpenCvSharp.Point(10, 60),
      HersheyFonts.HersheySimplex, 0.7, new Scalar(200, 200, 200), 2);
  }

  /// <summary>Log when there is =1, <1, or 0 light sources detected.</summary>
  /// <param name="ledCount">LEDs discovered.</param>
  private void LogLedCountTransitions(int ledCount)
  {
    // TODO: Use Enum
    int state = ledCount switch
    {
      0 => 0, // One LED
      1 => 1, // Multiple LEDs
      _ => 2, // None detected
    };

    if (state == _lastLedCount)
      return;

    string ts = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");

    if (state == 1)
      Console.WriteLine($"[{ts}] Exactly ONE LED is ON");
    else if (state == 2)
      Console.WriteLine($"[{ts}] MULTIPLE LEDs are ON (count={ledCount})");
    else
      Console.WriteLine($"[{ts}] No LEDs are ON");

    _lastLedCount = state;
  }
}
