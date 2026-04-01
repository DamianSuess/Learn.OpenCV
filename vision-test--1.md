# OpenCV LED Detection

1. Using OpenCV for C#, create an example that takes a live video feed from my webcam and detects when 1 or more LEDs light up within the designated area. Log the time to Console when 1 LED is on, and when more than one LED is on at the same time.

## Overview

Below is a **complete, runnable C# example** using **OpenCvSharp (OpenCV for .NET)** that:

*   Opens a **live webcam feed**
*   Looks only inside a **designated ROI (area)**
*   Detects **bright “LED-like” blobs**
*   **Counts** how many LEDs are ON
*   Logs **timestamped transitions** to the Console when:
    *   **exactly 1 LED** is ON
    *   **2+ LEDs** are ON simultaneously

It also shows a preview window with the ROI and detected LEDs drawn on top.

***

## 1) Install / Setup (OpenCvSharp)

From a terminal in a new folder:

```bash
dotnet new console -n LedDetectDemo
cd LedDetectDemo
dotnet add package OpenCvSharp4
dotnet add package OpenCvSharp4.runtime.win
```

> If you’re on Linux/macOS, use the appropriate runtime package (e.g., `OpenCvSharp4.runtime.ubuntu.20.04-x64`) or ensure OpenCV native binaries are available.

***

## 2) How the detection works (simple + robust)

Inside the ROI each frame:

1.  Convert to **grayscale**
2.  **Blur** to reduce noise
3.  Threshold for **very bright pixels** (LEDs tend to saturate)
4.  Apply **morphology** (remove specks, fill small gaps)
5.  Find **contours** and filter by area/circularity-like criteria
6.  The number of remaining blobs ≈ number of lit LEDs

Then we maintain a small state machine to log **only when the count “state” changes**, not every frame.

***

## 3) Full Example Code (Program.cs)

> This is a Console app. It uses OpenCV’s `imshow` window for live preview.

```csharp
using System;
using OpenCvSharp;

namespace LedDetectDemo
{
    class Program
    {
        // --- ROI (designated area) in pixels. Adjust these for your setup. ---
        // Tip: Run once, look at the preview, then tweak.
        static Rect RoiRect = new Rect(x: 100, y: 100, width: 400, height: 300);

        // --- Detection tuning knobs ---
        static int BrightThreshold = 220;     // 0..255. Increase if false positives.
        static int MinBlobArea = 30;          // Minimum contour area to be considered an LED
        static int MaxBlobArea = 5000;        // Maximum contour area to ignore large reflections
        static int MorphKernelSize = 3;       // Morphological cleanup kernel

        // Log state: 0 = none, 1 = exactly one, 2 = more than one
        static int _lastState = -1;

        static void Main()
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

        static int CountLitLeds(Mat roiBgr, out Point[] ledCenters)
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

        static void LogStateTransitions(int ledCount)
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

        static void DrawOverlay(Mat frame, Rect roi, Point[] ledCenters, int ledCount)
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

        static Rect ClampRectToFrame(Rect r, int frameWidth, int frameHeight)
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

## 4) Tuning tips (important for LED detection)

### If you get false positives (glare, reflections, shiny plastic)

*   Increase `BrightThreshold` (e.g., 230–245)
*   Increase `MinBlobArea`
*   Reduce ROI to exclude bright background
*   Add aspect/circularity filtering (already includes a basic aspect check)

### If you miss LEDs

*   Decrease `BrightThreshold` (e.g., 180–210)
*   Reduce `MinBlobArea`
*   Increase camera exposure / disable auto-exposure (if possible in camera settings)

### If LEDs are colored (red/green/blue) and brightness threshold isn’t enough

Use HSV color filtering instead of grayscale thresholding. I can provide a variant that detects *red-only* or *green-only* LEDs.

***

## 5) Upgrade ideas (if you want it more “production”)

*   **Debounce / persistence**: require LED state to be stable for N frames before logging (prevents flicker noise)
*   **Per-LED fixed locations**: if you know exactly where each LED is, sample small sub-ROIs and do a per-LED on/off decision (often the most reliable)
*   **Adaptive threshold**: compute ROI mean/stddev and pick threshold dynamically

***

## Quick question (so I can tailor the best variant)

Are your LEDs:

1.  **Same color** (just “bright spots”) or **specific colors** (e.g., red/green)?
2.  **Fixed positions** (known locations), or can they appear anywhere inside the ROI?

If you tell me that, I can provide a more robust version (either HSV color masks or per-LED sampling with hysteresis + debounce).
