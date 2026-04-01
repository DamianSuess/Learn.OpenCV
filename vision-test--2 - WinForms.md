# OpenCV LED Detection

1. Using OpenCV for C#, create an example that takes a live video feed from my webcam and detects when 1 or more LEDs light up within the designated area. Log the time to Console when 1 LED is on, and when more than one LED is on at the same time.
2. And make it WinForms application with 2 buttons, "Start" and "Stop". Provide a preview control of what the camera is seeing and outline detected LEDs outlined with squares.

## Overview
Absolutely — here’s a **WinForms** version using **OpenCvSharp** with:

*   **Start** and **Stop** buttons
*   A **PictureBox** preview of the live webcam feed
*   **Detected LEDs outlined with squares (rectangles)**
*   Console logging with timestamps when:
    *   **exactly 1 LED** is ON
    *   **2+ LEDs** are ON simultaneously
    *   (also logs when **none** are ON, on state change)

This is a clean “single-form, no designer required” sample you can paste into a new project.

***

## 1) Create the WinForms project + install packages

### Create project

```bash
dotnet new winforms -n LedDetectWinForms
cd LedDetectWinForms
```

### Add OpenCvSharp packages

```bash
dotnet add package OpenCvSharp4
dotnet add package OpenCvSharp4.runtime.win
dotnet add package OpenCvSharp4.Extensions
```

> `OpenCvSharp4.Extensions` gives you `BitmapConverter.ToBitmap(Mat)` for displaying frames in WinForms.

***

## 2) Project files

You’ll have **Program.cs** and **MainForm.cs** (plus the default .csproj).

### Program.cs

```csharp
using System;
using System.Windows.Forms;

namespace LedDetectWinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
```

***

## 3) MainForm.cs (Start/Stop + Preview + LED rectangles)

> This version builds UI controls in code (no designer) and runs capture in a background task with a CancellationToken.

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
        private readonly Button _btnStop  = new() { Text = "Stop", Width = 100, Enabled = false };
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

        // --- ROI (designated area) in pixels; tweak for your setup. ---
        // Tip: start with a big ROI, then narrow it once you see your LED panel.
        private Rect _roi = new Rect(x: 100, y: 100, width: 500, height: 350);

        // --- Detection knobs ---
        private int _brightThreshold = 220;   // 0..255 (raise to reduce false positives)
        private int _minBlobArea = 30;        // ignore tiny specks
        private int _maxBlobArea = 8000;      // ignore huge reflections
        private int _morphKernelSize = 3;     // cleanup kernel size

        // State machine: 0=none, 1=exactly one, 2=more than one
        private int _lastState = -1;

        public MainForm()
        {
            Text = "LED Detector (OpenCvSharp) - Start/Stop";
            Width = 1100;
            Height = 750;

            // Top panel for buttons
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

            // Optional: set desired resolution (may be ignored by some webcams)
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
            catch { /* ignore */ }
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

                // Clamp ROI to frame bounds
                var safeRoi = ClampRectToFrame(_roi, frame.Width, frame.Height);
                if (safeRoi.Width <= 0 || safeRoi.Height <= 0)
                    safeRoi = new Rect(0, 0, frame.Width, frame.Height); // fallback

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

                // Optional: reject overly skinny shapes (glare streaks)
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
            {
                Cv2.Rectangle(frame, r, new Scalar(0, 255, 0), 2);
            }

            // Status text
            string status = $"LEDs ON: {ledCount}  Thr={_brightThreshold}  MinArea={_minBlobArea}";
            Cv2.PutText(frame, status, new OpenCvSharp.Point(10, 30),
                HersheyFonts.HersheySimplex, 0.8, new Scalar(255, 255, 255), 2);
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
            // Convert to Bitmap for WinForms
            using Bitmap bmp = BitmapConverter.ToBitmap(frameBgr);

            // Marshal to UI thread
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
            // Avoid memory leak: dispose the previous image
            var old = _preview.Image;
            _preview.Image = newImage;
            old?.Dispose();
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
}
```

Run it:

```bash
dotnet run
```

***

## 4) How to adjust the “designated area” (ROI)

The ROI is currently fixed:

```csharp
private Rect _roi = new Rect(100, 100, 500, 350);
```

To match your LED board area, tweak these values:

*   `x,y` moves the box
*   `width,height` resizes the box

If you want, I can add **click-and-drag ROI selection** in the PictureBox (very handy).

***

## 5) Practical tuning guidance (for real LEDs)

*   **False positives (glare/reflections):**
    *   Raise `_brightThreshold` (e.g., 230–245)
    *   Increase `_minBlobArea`
    *   Narrow the ROI
*   **Missed LEDs:**
    *   Lower `_brightThreshold` (e.g., 180–210)
    *   Decrease `_minBlobArea`
    *   Improve lighting / exposure (avoid auto exposure pumping)

***

## Optional upgrades (tell me if you want these)

1.  **Debounce / hysteresis** (avoid flicker logging): require LED state to persist N frames before changing state.
2.  **Per-LED fixed locations** (most robust): define small boxes over each LED and measure brightness per box, then count “ON” LEDs.
3.  **Color-based detection** (HSV) for red/green LEDs.

***

### Quick question (so I can tailor the best detection strategy)

Are your LEDs in **fixed, known positions** (like a panel), or can they appear **anywhere in the ROI**?  
Fixed-position LEDs are *much* more reliable with per-LED sampling + hysteresis.
