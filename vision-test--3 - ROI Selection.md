# OpenCV LED Detection

1. Using OpenCV for C#, create an example that takes a live video feed from my webcam and detects when 1 or more LEDs light up within the designated area. Log the time to Console when 1 LED is on, and when more than one LED is on at the same time.
2. And make it WinForms application with 2 buttons, "Start" and "Stop". Provide a preview control of what the camera is seeing and outline detected LEDs outlined with squares.
3. Add click-and-drag ROI selection

## Overview
Sure thing — click‑and‑drag ROI selection is a great usability upgrade. Below is a **drop‑in update** to your WinForms example that lets you:

✅ **Click + drag on the PictureBox preview** to draw a selection rectangle  
✅ On mouse release, the selection becomes the new **ROI in camera/image coordinates**  
✅ ROI is shown in the live feed (yellow), and detected LEDs are boxed (green)

Because your `PictureBox.SizeMode = Zoom`, the preview often has **letterboxing** (black bars). The code below correctly maps mouse coordinates from the PictureBox **back to the real frame coordinates**, accounting for scaling + offsets.

***

# What changes you’ll make

1.  Add a few fields for drag state and frame size tracking
2.  Hook up PictureBox mouse events: `MouseDown`, `MouseMove`, `MouseUp`
3.  Add coordinate mapping helpers: PictureBox client → image/frame pixels
4.  Draw the drag rectangle overlay during selection (`PictureBox.Paint`)
5.  Use the newly selected ROI in your detection loop

***

# ✅ Updated `MainForm.cs` (complete file)

> If you already have `MainForm.cs` from the earlier response, you can replace it entirely with this version (it includes everything plus ROI drag support).

```csharp
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LedDetectWinForms
{
    public class MainForm : Form
    {
        // UI
        private readonly Button _btnStart = new() { Text = "Start", Width = 100 };
        private readonly Button _btnStop = new() { Text = "Stop", Width = 100, Enabled = false };
        private readonly PictureBox _preview = new()
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };

        // Capture loop
        private VideoCapture? _capture;
        private CancellationTokenSource? _cts;
        private Task? _captureTask;

        // ---- ROI selection / mapping ----
        private readonly object _roiLock = new();
        private Rect _roi = new Rect(x: 100, y: 100, width: 500, height: 350);

        private bool _dragging = false;
        private Point _dragStartClient;          // client coords in PictureBox
        private Rectangle _dragRectClient;       // client coords (for overlay drawing)

        // Store latest frame size for accurate mapping from PictureBox -> image coords
        private int _lastFrameWidth = 0;
        private int _lastFrameHeight = 0;

        // --- Detection knobs ---
        private int _brightThreshold = 220;
        private int _minBlobArea = 30;
        private int _maxBlobArea = 8000;
        private int _morphKernelSize = 3;

        // State machine: 0=none, 1=exactly one, 2=more than one
        private int _lastState = -1;

        public MainForm()
        {
            Text = "LED Detector (OpenCvSharp) - Start/Stop + Drag ROI";
            Width = 1100;
            Height = 750;

            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(10),
                FlowDirection = FlowDirection.LeftToRight
            };
            top.Controls.Add(_btnStart);
            top.Controls.Add(_btnStop);

            Controls.Add(_preview);
            Controls.Add(top);

            _btnStart.Click += async (_, __) => await StartAsync();
            _btnStop.Click += async (_, __) => await StopAsync();
            FormClosing += async (_, __) => await StopAsync();

            // ---- ROI drag event handlers ----
            _preview.MouseDown += Preview_MouseDown;
            _preview.MouseMove += Preview_MouseMove;
            _preview.MouseUp += Preview_MouseUp;
            _preview.Paint += Preview_Paint;

            // Optional UX: cursor indicates selection mode
            _preview.Cursor = Cursors.Cross;
        }

        private async Task StartAsync()
        {
            if (_captureTask != null && !_captureTask.IsCompleted)
                return;

            _capture = new VideoCapture(0);
            if (!_capture.IsOpened())
            {
                MessageBox.Show("Could not open the webcam.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _capture.Dispose();
                _capture = null;
                return;
            }

            _capture.FrameWidth = 1280;
            _capture.FrameHeight = 720;

            _cts = new CancellationTokenSource();
            _btnStart.Enabled = false;
            _btnStop.Enabled = true;

            _lastState = -1;

            _captureTask = Task.Run(() => CaptureLoop(_cts.Token));
            await Task.CompletedTask;
        }

        private async Task StopAsync()
        {
            try
            {
                _cts?.Cancel();
                if (_captureTask != null)
                    await Task.WhenAny(_captureTask, Task.Delay(500));
            }
            catch { }
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
            if (_capture == null) return;

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

                var safeRoi = ClampRectToFrame(roi, frame.Width, frame.Height);
                if (safeRoi.Width <= 0 || safeRoi.Height <= 0)
                    safeRoi = new Rect(0, 0, frame.Width, frame.Height);

                // Detect LED rectangles (in full-frame coordinates)
                var ledRects = DetectLedRects(frame, safeRoi, out int ledCount);

                // Log state transitions only
                LogStateTransitions(ledCount);

                // Draw overlays: ROI + LED rectangles
                DrawOverlay(frame, safeRoi, ledRects, ledCount);

                // Push to UI
                UpdatePreview(frame);

                Thread.Sleep(5);
            }
        }

        private List<Rect> DetectLedRects(Mat frameBgr, Rect roi, out int ledCount)
        {
            using var roiBgr = new Mat(frameBgr, roi);

            using var gray = new Mat();
            Cv2.CvtColor(roiBgr, gray, ColorConversionCodes.BGR2GRAY);

            using var blurred = new Mat();
            Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 0);

            using var binary = new Mat();
            Cv2.Threshold(blurred, binary, _brightThreshold, 255, ThresholdTypes.Binary);

            using var kernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new OpenCvSharp.Size(_morphKernelSize, _morphKernelSize));

            using var opened = new Mat();
            Cv2.MorphologyEx(binary, opened, MorphTypes.Open, kernel);

            using var closed = new Mat();
            Cv2.MorphologyEx(opened, closed, MorphTypes.Close, kernel);

            Cv2.FindContours(closed, out OpenCvSharp.Point[][] contours, out _,
                RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            var rects = new List<Rect>();

            foreach (var c in contours)
            {
                double area = Cv2.ContourArea(c);
                if (area < _minBlobArea || area > _maxBlobArea)
                    continue;

                var r = Cv2.BoundingRect(c);

                // Reject overly skinny shapes (optional)
                double aspect = r.Width / (double)Math.Max(1, r.Height);
                if (aspect < 0.2 || aspect > 5.0)
                    continue;

                // Convert ROI-local rect -> full-frame rect
                rects.Add(new Rect(r.X + roi.X, r.Y + roi.Y, r.Width, r.Height));
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

            string status = $"LEDs ON: {ledCount}  Thr={_brightThreshold}  MinArea={_minBlobArea}";
            Cv2.PutText(frame, status, new OpenCvSharp.Point(10, 30),
                HersheyFonts.HersheySimplex, 0.8, new Scalar(255, 255, 255), 2);

            // Helpful hint text
            Cv2.PutText(frame, "Drag on the preview to set ROI", new OpenCvSharp.Point(10, 60),
                HersheyFonts.HersheySimplex, 0.7, new Scalar(200, 200, 200), 2);
        }

        private void LogStateTransitions(int ledCount)
        {
            int state = ledCount switch
            {
                0 => 0,
                1 => 1,
                _ => 2
            };

            if (state == _lastState) return;

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
            using Bitmap bmp = BitmapConverter.ToBitmap(frameBgr);

            if (_preview.InvokeRequired)
            {
                _preview.BeginInvoke(new Action(() => SetPreviewImage((Bitmap)bmp.Clone())));
            }
            else
            {
                SetPreviewImage((Bitmap)bmp.Clone());
            }
        }

        private void SetPreviewImage(Bitmap newImage)
        {
            var old = _preview.Image;
            _preview.Image = newImage;
            old?.Dispose();
        }

        // ---------------------------
        // ROI Drag selection handlers
        // ---------------------------

        private void Preview_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (_lastFrameWidth <= 0 || _lastFrameHeight <= 0) return; // no frame yet

            _dragging = true;
            _dragStartClient = e.Location;
            _dragRectClient = new Rectangle(e.Location, new Size(0, 0));
            _preview.Invalidate();
        }

        private void Preview_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            _dragRectClient = MakeNormalizedRect(_dragStartClient, e.Location);
            _preview.Invalidate();
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

        // ---------------------------
        // Coordinate mapping helpers
        // ---------------------------

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

        /// <summary>
        /// Converts a PictureBox client rectangle (mouse drag) into an OpenCvSharp Rect in image pixels.
        /// Returns null if selection is outside the displayed image area.
        /// </summary>
        private Rect? ClientRectToImageRect(Rect clientRect)
        {
            var imgDisp = GetImageDisplayRect();
            if (imgDisp == Rectangle.Empty) return null;

            // Intersect with displayed image area so dragging into black bars doesn't break mapping
            var sel = Rectangle.Intersect(clientRect, imgDisp);
            if (sel.Width <= 0 || sel.Height <= 0) return null;

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

        private static Rectangle MakeNormalizedRect(Point a, Point b)
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
            return new Rect(x, y, Math.Max(0, w), Math.Max(0, h));
        }
    }
}
```

***

# Notes / Tips

### ✅ How it behaves

*   Dragging draws a **green translucent rectangle** on the preview.
*   On mouse release, ROI updates immediately.
*   The **yellow ROI rectangle** in the camera feed will jump to your selection (on the next frames).

### ROI size sanity

This ignores “tiny” selections:

```csharp
imgRect.Value.Width > 5 && imgRect.Value.Height > 5
```

Change that if you want.

***

# Want a nice extra UX feature?

If you want, I can add either of these:

1.  **Right‑click resets ROI** to full frame
2.  **Click+drag to move existing ROI** (drag inside ROI moves it; drag outside creates new)
3.  An on‑screen **slider** for threshold / min blob area so you can tune without recompiling

Which would be most useful for your setup?
