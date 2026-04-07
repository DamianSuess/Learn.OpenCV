using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace VisionLedTest;

// TODO: Use method calls!!! Not cheap hax0r skr1p7 k!ddy 1997 workarounds
public partial class Form1 : Form
{
  /// <summary>Light/LED detector business logic.</summary>
  private readonly LightDetector _detector = new();

  private VideoCapture? _capture;
  private CancellationTokenSource? _cts;
  private Task? _captureTask;

  #region ROI Selection / Mapping

  private readonly object _roiLock = new();

  private bool _isDragging = false;

  /// <summary>Client coords in PictureBox.</summary>
  private System.Drawing.Point _dragStartClient;

  /// <summary>Client coordinates (for overlay drawing).</summary>
  private Rectangle _dragRectClient;

  #endregion ROI Selection / Mapping

  private string? _imageFileName = null;
  private List<System.Drawing.Point> _ledPositions = [];

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

    NumBrightnessThreshold.Value = _detector.BrightThreshold;
    NumBlobMax.Value = _detector.BlobAreaMax;
    NumBlobMin.Value = _detector.BlobAreaMin;

    ThresholdHScroll.Value = _detector.BrightThreshold;
    BlobMaxScroll.Value = _detector.BlobAreaMax;
    BlobMinScroll.Value = _detector.BlobAreaMin;

    // Causes app to delay by 5sec on startup & not wired to Start/Stop
    ////_detector.GetCameraList();
    CmbCamera.Items.Clear();
    CmbCamera.Items.AddRange([0, 1, 2, 3]);
    CmbCamera.SelectedIndex = 0;
  }

  private void Form1_Load(object sender, EventArgs e)
  {
  }

  #region User Controls - Start/Stop/Load

  private async void BtnStart_ClickAsync(object sender, EventArgs e)
  {
    _imageFileName = null;
    await StartAsync();

    BtnLoadTemplate.Enabled = false;
    CmbCamera.Enabled = false;
  }

  private async void BtnStop_ClickAsync(object sender, EventArgs e)
  {
    _imageFileName = null;

    await StopAsync();
    BtnLoadTemplate.Enabled = true;
    CmbCamera.Enabled = true;
  }

  private void BtnLoadTemplate_Click(object sender, EventArgs e)
  {
    string templatePath;

    // Don't do this, use a method with a parameter
    if (sender as Button == BtnImageRefresh && _imageFileName is not null)
    {
      templatePath = _imageFileName;
    }
    else
    {
      using var ofd = new OpenFileDialog
      {
        Filter = "Images (*.png;*.jpg)|*.png;*.jpg|PNG Images (*.png)|*.png|JPEG Images (*.jpg)|*.jpg|All Files|*",
      };

      if (ofd.ShowDialog() != DialogResult.OK)
        return;

      templatePath = ofd.FileName;
    }

    try
    {
      // Analyze in color and let "LightDetector" class binarize it
      // So that we can display the output in color.
      //
      ////using var frame = Cv2.ImRead(templatePath, ImreadModes.Grayscale);
      using var frame = Cv2.ImRead(templatePath, ImreadModes.Color);
      if (frame.Empty())
      {
        MessageBox.Show("Failed to load image");
        return;
      }

      _imageFileName = templatePath;

      int algorithm = 2;

      if (algorithm == 1)
        AnalyzeStaticImage(frame); // Alg1: Binary OG Static Images
      else
      {
        _detector.AnalyzeFrame(frame, rois: null, isSrcGrayscale: false);
      }

      // Push to UI (PictureBox)
      UpdatePreview(frame);
    }
    catch (Exception ex)
    {
      var nl = Environment.NewLine;
      MessageBox.Show($"Failure processing!{nl}Message: {ex.Message}{nl}Source: {ex.Source}");
    }
  }

  private void btnRefresh_Click(object sender, EventArgs e)
  {
    if (_imageFileName is null)
    {
      MessageBox.Show("No template loaded");
      return;
    }

    using var grayMat = Cv2.ImRead(_imageFileName, ImreadModes.Grayscale);
    if (grayMat.Empty())
      return;

    AnalyzeStaticImage(grayMat);
  }

  #endregion User Controls - Start/Stop/Load

  private void AnalyzeBinary(Mat frame)
  {
    var src = Cv2.ImRead(_imageFileName, ImreadModes.Grayscale);
    Rect roi = new()
    {
      X = 0,
      Y = 0,
      Width = src.Width,
      Height = src.Height,
    };

    // Threshold to find bright mark
    Cv2.Threshold(src, src, 200, 255, ThresholdTypes.Binary);
    UpdatePreview(src);
  }

  private void AnalyzeStaticImage(Mat? gray)
  {
    try
    {
      // Show quick grayscale preview with threshold applied (for user feedback/tuning) - this is optional and can be removed
      ////double thresh = _brightThreshold;
      double thresh = _detector.BrightThreshold;

      // Create a binary mask (don't mutate gray in-place)
      using var binary = new Mat();
      Cv2.Threshold(gray, binary, thresh, 255, ThresholdTypes.Binary);

      // Morphology to clean up small noise
      using var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new OpenCvSharp.Size(3, 3));
      Cv2.MorphologyEx(binary, binary, MorphTypes.Open, kernel);

      // Try contours first (fast + common)
      Cv2.FindContours(binary, out OpenCvSharp.Point[][] contours, out HierarchyIndex[] _,
          RetrievalModes.External, ContourApproximationModes.ApproxSimple);

      var centers = new List<System.Drawing.Point>();

      foreach (var cnt in contours)
      {
        // Min/Max Blob size
        double area = Cv2.ContourArea(cnt);
        if (area < _detector.BlobAreaMin || area > _detector.BlobAreaMax)
          continue;

        var m = Cv2.Moments(cnt);
        if (Math.Abs(m.M00) < double.Epsilon)
          continue;

        int cx = (int)(m.M10 / m.M00);
        int cy = (int)(m.M01 / m.M00);
        centers.Add(new System.Drawing.Point(cx, cy));
      }

      // If contours found nothing, fall back to ConnectedComponentsWithStats
      if (centers.Count == 0)
      {
        // ConnectedComponentsWithStats requires CV_8UC1; binary is already that from Threshold
        using var labels = new Mat();
        using var stats = new Mat();
        using var centroids = new Mat();

        int nLabels = Cv2.ConnectedComponentsWithStats(
            binary,
            labels,
            stats,
            centroids,
            PixelConnectivity.Connectivity8,
            MatType.CV_32S);

        // label 0 is background, start at 1
        for (int i = 1; i < nLabels; i++)
        {
          int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
          if (area < 3)
            continue; // filter tiny specks; tune this

          // Centroids is CV_64F with 2 columns (x,y)
          double cx = centroids.At<double>(i, 0);
          double cy = centroids.At<double>(i, 1);

          centers.Add(new System.Drawing.Point((int)Math.Round(cx), (int)Math.Round(cy)));
        }
      }

      // Optional: sort left-to-right then top-to-bottom for stable ordering
      centers = centers
        .OrderBy(p => p.Y)
        .ThenBy(p => p.X)
        .ToList();

      // Show me what you go baby!
      UpdatePreview(binary);

      // Draw overlays: ROI + LED rectangles
      ////DrawOverlay(binary, safeRoi, ledRects, ledCount);

      _ledPositions = centers;

      LblCount.Text = _ledPositions.Count.ToString();
      LblStatus.Text = $"Template loaded: {System.IO.Path.GetFileName(_imageFileName)}; LEDs found: {_ledPositions.Count}";
    }
    catch (Exception ex)
    {
      var nl = Environment.NewLine;
      MessageBox.Show($"Failure processing!{nl}Message: {ex.Message}{nl}Source: {ex.Source}");
    }
  }

  private void Preview_Paint(object? sender, PaintEventArgs e)
  {
    // Draw the in-progress selection rectangle in client coordinates.
    if (_isDragging && _dragRectClient.Width > 0 && _dragRectClient.Height > 0)
    {
      using var pen = new Pen(Color.Lime, 2);
      e.Graphics.DrawRectangle(pen, _dragRectClient);

      using var brush = new SolidBrush(Color.FromArgb(40, Color.Lime));
      e.Graphics.FillRectangle(brush, _dragRectClient);
    }
  }

  private void Preview_MouseUp(object? sender, MouseEventArgs e)
  {
    if (!_isDragging || e.Button != MouseButtons.Left) return;
    _isDragging = false;

    _dragRectClient = MakeNormalizedRect(_dragStartClient, e.Location);

    // Convert dragged client rectangle -> image/frame rectangle
    var imgRect = ClientRectangleToImageRect(_dragRectClient);
    if (imgRect.HasValue && imgRect.Value.Width > 5 && imgRect.Value.Height > 5)
    {
      // Set new ROI (thread-safe)
      lock (_roiLock)
      {
        _detector.SetRoi(imgRect.Value);
      }
    }

    _dragRectClient = Rectangle.Empty;
    _preview.Invalidate();

    // Search again for blobs (LEDs)
    if (_imageFileName is not null)
    {
      // TODO: Use method calls!!! Not cheap hax0r skr1p7 k!ddy 1997 workarounds
      // Pass, 'btnImageRefresh' object so the image analyzer thinks Refresh called it
      BtnImageRefresh_Click(BtnImageRefresh, e);
    }
  }

  private void PreviewMouseMove(object? sender, MouseEventArgs e)
  {
    if (!_isDragging)
      return;

    _dragRectClient = MakeNormalizedRect(_dragStartClient, e.Location);
    _preview.Invalidate();
  }

  private void Preview_MouseDown(object? sender, MouseEventArgs e)
  {
    if (e.Button != MouseButtons.Left)
      return;

    // No frame yet
    ////if (_lastFrameWidth <= 0 || _lastFrameHeight <= 0)
    ////  return;
    if (!_detector.HasCachedFrame)
      return;

    _isDragging = true;
    _dragStartClient = e.Location;
    _dragRectClient = new Rectangle(e.Location, new System.Drawing.Size(0, 0));
    _preview.Invalidate(); // Force redraw of surface
  }

  private async Task StartAsync()
  {
    if (_captureTask is not null && _captureTask.IsCompleted)
      return;

    _capture = new VideoCapture(1);
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
    BtnStart.Enabled = false;
    BtnStop.Enabled = true;

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

      BtnStart.Enabled = true;
      BtnStop.Enabled = false;

      // Clear any drag overlay
      _isDragging = false;
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

      // NOTE: AnalyzeFrame ROIs and draw overlays for debugging, but this is not free.
      //  Consider separating pure detection logic from visualization
      //  for better performance in production.
      var results = _detector.AnalyzeFrame(frame, rois: null);
      ////AnalyzeFrame(frame);

      // Push to UI (PictureBox)
      UpdatePreview(frame);

      void UpdateStatus()
      {
        LblCount.Text = results.Count.ToString();
        LblStatus.Text = $"LEDs found: {results.Count}";
      }

      if (LblCount.InvokeRequired)
        LblCount.BeginInvoke(new Action(() => UpdateStatus()));
      else
        UpdateStatus();

      // Small delay to reduce CPU (tune as desired)
      Thread.Sleep(5);
    }
  }

  private void UpdatePreview(Mat frameBgr)
  {
    // Convert to Bitmap for WinForms
    using Bitmap bmp = BitmapConverter.ToBitmap(frameBgr);
    var imgClone = (Bitmap)bmp.Clone();

    // Marshal to UI thread
    if (_preview.InvokeRequired)
      _preview.BeginInvoke(new Action(
        () => SetPreviewImage(imgClone)));
    else
      SetPreviewImage(imgClone);
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

  /// <summary>
  ///   Converts a PictureBox client rectangle (mouse drag) into an OpenCvSharp Rect in image pixels.
  ///   Returns null if selection is outside the displayed image area.
  /// </summary>
  ////  private Rect? ClientRectToImageRect(Rect clientRect)
  private Rect? ClientRectangleToImageRect(Rectangle clientRect)
  {
    var imgDisp = GetImageDisplayRect();
    if (imgDisp == Rectangle.Empty)
      return null;

    // Intersect with displayed image area so dragging into black bars doesn't break mapping
    var sel = Rectangle.Intersect(clientRect, imgDisp);
    if (sel.Width <= 0 || sel.Height <= 0)
      return null;

    // Map client -> image pixels
    float scaleX = _detector.LastFrameWidth / (float)imgDisp.Width;
    float scaleY = _detector.LastFrameHeight / (float)imgDisp.Height;

    int x = (int)((sel.X - imgDisp.X) * scaleX);
    int y = (int)((sel.Y - imgDisp.Y) * scaleY);
    int w = (int)(sel.Width * scaleX);
    int h = (int)(sel.Height * scaleY);

    // Clamp to image bounds
    var r = new Rect(x, y, w, h);
    return _detector.ClampRectToFrame(r, _detector.LastFrameWidth, _detector.LastFrameHeight);
  }

  /// <summary>
  /// For PictureBoxSizeMode.Zoom: returns the rectangle inside the PictureBox where the image actually appears.
  /// This accounts for letterboxing bars.
  /// </summary>
  private Rectangle GetImageDisplayRect()
  {
    int pbW = _preview.ClientSize.Width;
    int pbH = _preview.ClientSize.Height;

    int imgW = _detector.LastFrameWidth;
    int imgH = _detector.LastFrameHeight;

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

  #region User Controls - Adjusters

  private void ThresholdHScroll_Scroll(object sender, ScrollEventArgs e)
  {
    ////_brightThreshold = ThresholdHScroll.Value;
    _detector.BrightThreshold = ThresholdHScroll.Value;
    NumBrightnessThreshold.Value = ThresholdHScroll.Value;
  }

  private void NumBrightnessThreshold_ValueChanged(object sender, EventArgs e)
  {
    ////_brightThreshold = (int)numBrightnessThreshold.Value;
    _detector.BrightThreshold = (int)NumBrightnessThreshold.Value;

    // Auto refresh loaded template
    if (_imageFileName is not null)
      btnRefresh_Click(sender, e);
  }

  private void NumBlobMax_ValueChanged(object sender, EventArgs e)
  {
    ////_maxBlobArea = (int)numBlobMax.Value;
    _detector.BlobAreaMax = (int)NumBlobMax.Value;
  }

  private void NumBlobMin_ValueChanged(object sender, EventArgs e)
  {
    ////_minBlobArea = (int)numBlobMin.Value;
    _detector.BlobAreaMin = (int)NumBlobMin.Value;
  }

  private void BtnImageRefresh_Click(object sender, EventArgs e)
  {
    BtnLoadTemplate_Click(sender, e);
  }

  private void BlobMaxScroll_ValueChanged(object sender, EventArgs e)
  {
    ////_maxBlobArea = BlobMaxScroll.Value;
    _detector.BlobAreaMax = BlobMaxScroll.Value;
    NumBlobMax.Value = BlobMaxScroll.Value;
  }

  private void BlobMinScroll_ValueChanged(object sender, EventArgs e)
  {
    ////_minBlobArea = BlobMinScroll.Value;
    _detector.BlobAreaMin = BlobMinScroll.Value;
    NumBlobMin.Value = BlobMinScroll.Value;
  }

  private void BtnGenerateGrid_Click(object sender, EventArgs e)
  {
    int cols = 5;
    int rows = 3;

    var grid = RoiManager.GenerateGrid(
      rows,
      cols,
      _detector.LastFrameWidth,
      _detector.LastFrameHeight);

    _detector.RoiManager.SetRois(grid);
  }

  private void CmbCamera_SelectedIndexChanged(object sender, EventArgs e)
  {
    _detector.CameraIndex = CmbCamera.SelectedIndex;
  }

  #endregion User Controls - Adjusters
}
