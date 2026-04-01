using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VisionLedTest;

public partial class Form1 : Form
{
  private VideoCapture? _capture;
  private CancellationTokenSource? _cts;
  private Task? _captureTask;

  #region ROI Selection / Mapping

  private readonly object _roiLock = new();
  private Rect _roi = new Rect(X: 100, Y: 100, Width: 500, Height: 350);

  private bool _dragging = false;

  /// <summary>Client coords in PictureBox.</summary>
  private System.Drawing.Point _dragStartClient;

  /// <summary>Client coordinates (for overlay drawing).</summary>
  private Rectangle _dragRectClient;

  #endregion ROI Selection / Mapping

  // Store latest frame size for accurate mapping from PictureBox -> image coords
  private int _lastFrameWidth = 0;

  private int _lastFrameHeight = 0;

  #region Detection Knobs

  /// <summary>Range: 0 - 225.</summary>
  private int _brightThreshold = 220;

  /// <summary>Ignore tiny specks.</summary>
  private int _minBlobArea = 30;

  /// <summary>Ignore huge reflections.</summary>
  private int _maxBlobArea = 8000;

  /// <summary>Cleanup kernel size.</summary>
  private int _morphKernelSize = 3;

  #endregion Detection Knobs

  private int _lastState = -1;

  private enum StateMachine
  {
    None = 0,
    One = 1,
    MoreThanOne = 2,
  }

  public Form1()
  {
    InitializeComponent();

    _preview.SizeMode = PictureBoxSizeMode.Zoom;
    _preview.BackColor = System.Drawing.Color.Black;
    _preview.MouseDown += Preview_MouseDown;
    _preview.MouseMove += PreviewMouseMove;
    _preview.MouseUp += Preview_MouseUp;
    _preview.Paint += Preview_Paint;

    // UX: Cursor indicates selection mode
    _preview.Cursor = Cursors.Cross;

    FormClosing += async (_, __) => await StopAsync();
  }

  private async void BtnStart_Click(object sender, EventArgs e)
  {
    await StartAsync();
  }

  private async void BtnStop_Click(object sender, EventArgs e)
  {
    await StopAsync();
  }

  private void Preview_Paint(object? sender, PaintEventArgs e)
  {
    // Draw the in-progress selection rectangle in client coordinates.
    if (_dragging && _dragRectClient.Width > 0 && _dragRectClient.Height > 0)
    {
      using var pen = new Pen(Color.Lime, 2);
      e.Graphics.DrawRectangle(pen, _dragRectClient);

      using var brush = new SolidBrush(Color.FromArgb(40, Color.Lime));
      e.Graphics.FillRectangle(brush, _dragRectClient);
    }
  }

  private void Preview_MouseUp(object? sender, MouseEventArgs e)
  {
    if (!_dragging || e.Button != MouseButtons.Left) return;
    _dragging = false;

    _dragRectClient = MakeNormalizedRect(_dragStartClient, e.Location);

    // Convert dragged client rectangle -> image/frame rectangle
    var imgRect = ClientRectToImageRect(_dragRectClient);
    if (imgRect.HasValue && imgRect.Value.Width > 5 && imgRect.Value.Height > 5)
    {
      // Set new ROI (thread-safe)
      lock (_roiLock)
      {
        _roi = imgRect.Value;
      }
    }

    _dragRectClient = Rectangle.Empty;
    _preview.Invalidate();
  }

  private void PreviewMouseMove(object? sender, MouseEventArgs e)
  {
    if (!_dragging)
      return;

    _dragRectClient = MakeNormalizedRect(_dragStartClient, e.Location);
    _preview.Invalidate();
  }

  private void Preview_MouseDown(object? sender, MouseEventArgs e)
  {
    if (e.Button != MouseButtons.Left)
      return;

    // No frame yet
    if (_lastFrameWidth <= 0 || _lastFrameHeight <= 0)
      return;

    _dragging = true;
    _dragStartClient = e.Location;
    _dragRectClient = new Rectangle(e.Location, new System.Drawing.Size(0, 0));
    _preview.Invalidate(); // Force redraw of surface
  }

  private async Task StartAsync()
  {
    if (_captureTask is not null && _captureTask.IsCompleted)
      return;

    _capture = new VideoCapture(0);
    if (!_capture.IsOpened())
    {
      MessageBox.Show("Could not open the webcam", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      _capture.Dispose();
      _capture = null;
      return;
    }

    // Set desired resolution
    _capture.FrameWidth = 1280;
    _capture.FrameHeight = 720;

    _cts = new CancellationTokenSource();
    _btnStart.Enabled = false;
    _btnStop.Enabled = true;

    _lastState = -1; // reset logging state on start

    _captureTask = Task.Run(() => CaptureLoop(_cts.Token));
    await Task.CompletedTask;
  }

  private async Task StopAsync()
  {
    try
    {
      _cts?.Cancel();

      if (_captureTask != null)
      {
        // Wait a short moment for clean shutdown
        await Task.WhenAny(_captureTask, Task.Delay(500));
      }
    }
    catch
    {
      // ignore
    }
    finally
    {
      _cts?.Dispose();
      _cts = null;

      _capture?.Release();
      _capture?.Dispose();
      _capture = null;

      _captureTask = null;

      _btnStart.Enabled = true;
      _btnStop.Enabled = false;

      // Clear any drag overlay
      _dragging = false;
      _dragRectClient = Rectangle.Empty;
      _preview.Invalidate();
    }
  }

  private void CaptureLoop(CancellationToken token)
  {
    if (_capture is null)
      return;

    using var frame = new Mat();

    while (!token.IsCancellationRequested)
    {
      if (!_capture.Read(frame) || frame.Empty())
        continue;

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
      var ledRects = DetectLedRects(frame, safeRoi, out int ledCount);

      // Log state transitions only
      LogStateTransitions(ledCount);

      // Draw overlays: ROI + LED rectangles
      DrawOverlay(frame, safeRoi, ledRects, ledCount);

      // Push to UI (PictureBox)
      UpdatePreview(frame);

      // Small delay to reduce CPU (tune as desired)
      Thread.Sleep(5);
    }
  }

  private List<Rect> DetectLedRects(Mat frameBgr, Rect roi, out int ledCount)
  {
    // Work on ROI only
    using var roiBgr = new Mat(frameBgr, roi);

    using var gray = new Mat();
    Cv2.CvtColor(roiBgr, gray, ColorConversionCodes.BGR2GRAY);

    using var blurred = new Mat();
    Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 0);

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

  private void DrawOverlay(Mat frame, Rect roi, List<Rect> ledRects, int ledCount)
  {
    // ROI in yellow
    Cv2.Rectangle(frame, roi, new Scalar(0, 255, 255), 2);

    // LED boxes in green
    foreach (var r in ledRects)
      Cv2.Rectangle(frame, r, new Scalar(0, 255, 0), 2);

    // Status text
    string status = $"LEDs ON: {ledCount}  Thr={_brightThreshold}  MinArea={_minBlobArea}";
    Cv2.PutText(frame, status, new OpenCvSharp.Point(10, 30),
      HersheyFonts.HersheySimplex, 0.8, new Scalar(255, 255, 255), 2);

    // Helpful hint text
    Cv2.PutText(frame, "Drag on the preview to set ROI", new OpenCvSharp.Point(10, 60),
      HersheyFonts.HersheySimplex, 0.7, new Scalar(200, 200, 200), 2);
  }

  private void LogStateTransitions(int ledCount)
  {
    // TODO: Use Enum
    int state = ledCount switch
    {
      0 => 0, // One LED
      1 => 1, // Multiple LEDs
      _ => 2, // None detected
    };

    if (state == _lastState)
      return;

    string ts = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");

    if (state == 1)
      Console.WriteLine($"[{ts}] Exactly ONE LED is ON");
    else if (state == 2)
      Console.WriteLine($"[{ts}] MULTIPLE LEDs are ON (count={ledCount})");
    else
      Console.WriteLine($"[{ts}] No LEDs are ON");

    _lastState = state;
  }

  private void UpdatePreview(Mat frameBgr)
  {
    // Convert to Bitmap for WinForms
    using Bitmap bmp = BitmapConverter.ToBitmap(frameBgr);
    var x = (Bitmap)bmp.Clone();

    // Marshal to UI thread
    if (_preview.InvokeRequired)
      _preview.BeginInvoke(new Action(() =>
      {
        //// SetPreviewImage((Bitmap)bmp.Clone());
        SetPreviewImage(x);
      }));
    else
      SetPreviewImage((Bitmap)bmp.Clone());
  }

  private void SetPreviewImage(Bitmap newImage)
  {
    // Avoid memory leak: dispose the previous image
    var old = _preview.Image;
    _preview.Image = newImage;
    old?.Dispose();
  }

  private static Rectangle MakeNormalizedRect(System.Drawing.Point a, System.Drawing.Point b)
  {
    int x1 = Math.Min(a.X, b.X);
    int y1 = Math.Min(a.Y, b.Y);
    int x2 = Math.Max(a.X, b.X);
    int y2 = Math.Max(a.Y, b.Y);
    return new Rectangle(x1, y1, x2 - x1, y2 - y1);
  }

  private static Rect ClampRectToFrame(Rect r, int frameWidth, int frameHeight)
  {
    int x = Math.Max(0, r.X);
    int y = Math.Max(0, r.Y);
    int w = Math.Min(r.Width, frameWidth - x);
    int h = Math.Min(r.Height, frameHeight - y);
    ////return new Rect(x, y, w, h);
    return new Rect(x, y, Math.Max(0, w), Math.Max(0, h));
  }

  /// <summary>
  ///   Converts a PictureBox client rectangle (mouse drag) into an OpenCvSharp Rect in image pixels.
  ///   Returns null if selection is outside the displayed image area.
  /// </summary>
  ////  private Rect? ClientRectToImageRect(Rect clientRect)
  private Rect? ClientRectToImageRect(Rectangle clientRect)
  {
    var imgDisp = GetImageDisplayRect();
    if (imgDisp == Rectangle.Empty)
      return null;

    // Convert OpenCV Rect to System.Drawing.Rectangle
    ////Rectangle sysRect = new()
    ////{
    ////  X = clientRect.X,
    ////  Y = clientRect.Y,
    ////  Width = imgDisp.Width,
    ////  Height = imgDisp.Height,
    ////};

    // Intersect with displayed image area so dragging into black bars doesn't break mapping
    var sel = Rectangle.Intersect(clientRect, imgDisp);
    if (sel.Width <= 0 || sel.Height <= 0)
      return null;

    // Map client -> image pixels
    float scaleX = _lastFrameWidth / (float)imgDisp.Width;
    float scaleY = _lastFrameHeight / (float)imgDisp.Height;

    int x = (int)((sel.X - imgDisp.X) * scaleX);
    int y = (int)((sel.Y - imgDisp.Y) * scaleY);
    int w = (int)(sel.Width * scaleX);
    int h = (int)(sel.Height * scaleY);

    // Clamp to image bounds
    var r = new Rect(x, y, w, h);
    return ClampRectToFrame(r, _lastFrameWidth, _lastFrameHeight);
  }

  /// <summary>
  /// For PictureBoxSizeMode.Zoom: returns the rectangle inside the PictureBox where the image actually appears.
  /// This accounts for letterboxing bars.
  /// </summary>
  private Rectangle GetImageDisplayRect()
  {
    int pbW = _preview.ClientSize.Width;
    int pbH = _preview.ClientSize.Height;

    int imgW = _lastFrameWidth;
    int imgH = _lastFrameHeight;

    if (pbW <= 0 || pbH <= 0 || imgW <= 0 || imgH <= 0)
      return Rectangle.Empty;

    float pbAspect = pbW / (float)pbH;
    float imgAspect = imgW / (float)imgH;

    int drawW, drawH;
    if (imgAspect > pbAspect)
    {
      // Image limited by width
      drawW = pbW;
      drawH = (int)(pbW / imgAspect);
    }
    else
    {
      // Image limited by height
      drawH = pbH;
      drawW = (int)(pbH * imgAspect);
    }

    int offsetX = (pbW - drawW) / 2;
    int offsetY = (pbH - drawH) / 2;

    return new Rectangle(offsetX, offsetY, drawW, drawH);
  }
}
