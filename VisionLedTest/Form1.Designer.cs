using System.Drawing;
using System.Windows.Forms;

namespace VisionLedTest
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      BtnStart = new Button();
      BtnStop = new Button();
      _preview = new PictureBox();
      BtnLoadTemplate = new Button();
      label1 = new Label();
      LblCount = new Label();
      LblStatus = new Label();
      NumBrightnessThreshold = new NumericUpDown();
      BtnRefresh = new Button();
      ThresholdHScroll = new HScrollBar();
      label2 = new Label();
      label3 = new Label();
      NumBlobMin = new NumericUpDown();
      NumBlobMax = new NumericUpDown();
      BtnImageRefresh = new Button();
      CmbCamera = new ComboBox();
      label4 = new Label();
      label5 = new Label();
      BlobMinScroll = new HScrollBar();
      BlobMaxScroll = new HScrollBar();
      BtnGenerateGrid = new Button();
      ((System.ComponentModel.ISupportInitialize)_preview).BeginInit();
      ((System.ComponentModel.ISupportInitialize)NumBrightnessThreshold).BeginInit();
      ((System.ComponentModel.ISupportInitialize)NumBlobMin).BeginInit();
      ((System.ComponentModel.ISupportInitialize)NumBlobMax).BeginInit();
      SuspendLayout();
      // 
      // BtnStart
      // 
      BtnStart.Location = new Point(13, 12);
      BtnStart.Name = "BtnStart";
      BtnStart.Size = new Size(75, 23);
      BtnStart.TabIndex = 0;
      BtnStart.Text = "Start";
      BtnStart.UseVisualStyleBackColor = true;
      BtnStart.Click += BtnStart_ClickAsync;
      // 
      // BtnStop
      // 
      BtnStop.Location = new Point(94, 12);
      BtnStop.Name = "BtnStop";
      BtnStop.Size = new Size(75, 23);
      BtnStop.TabIndex = 1;
      BtnStop.Text = "Stop";
      BtnStop.UseVisualStyleBackColor = true;
      BtnStop.Click += BtnStop_ClickAsync;
      // 
      // _preview
      // 
      _preview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      _preview.Location = new Point(12, 120);
      _preview.Name = "_preview";
      _preview.Size = new Size(572, 256);
      _preview.TabIndex = 2;
      _preview.TabStop = false;
      // 
      // BtnLoadTemplate
      // 
      BtnLoadTemplate.Location = new Point(175, 12);
      BtnLoadTemplate.Name = "BtnLoadTemplate";
      BtnLoadTemplate.Size = new Size(93, 23);
      BtnLoadTemplate.TabIndex = 3;
      BtnLoadTemplate.Text = "Load Image";
      BtnLoadTemplate.UseVisualStyleBackColor = true;
      BtnLoadTemplate.Click += BtnLoadTemplate_Click;
      // 
      // label1
      // 
      label1.AutoSize = true;
      label1.Location = new Point(306, 16);
      label1.Name = "label1";
      label1.Size = new Size(29, 15);
      label1.TabIndex = 4;
      label1.Text = "Cnt:";
      // 
      // LblCount
      // 
      LblCount.AutoSize = true;
      LblCount.Location = new Point(341, 16);
      LblCount.Name = "LblCount";
      LblCount.Size = new Size(13, 15);
      LblCount.TabIndex = 5;
      LblCount.Text = "0";
      // 
      // LblStatus
      // 
      LblStatus.AutoSize = true;
      LblStatus.Location = new Point(371, 16);
      LblStatus.Name = "LblStatus";
      LblStatus.Size = new Size(35, 15);
      LblStatus.TabIndex = 6;
      LblStatus.Text = "(null)";
      // 
      // NumBrightnessThreshold
      // 
      NumBrightnessThreshold.Location = new Point(70, 70);
      NumBrightnessThreshold.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
      NumBrightnessThreshold.Name = "NumBrightnessThreshold";
      NumBrightnessThreshold.Size = new Size(53, 23);
      NumBrightnessThreshold.TabIndex = 7;
      NumBrightnessThreshold.Value = new decimal(new int[] { 250, 0, 0, 0 });
      NumBrightnessThreshold.ValueChanged += NumBrightnessThreshold_ValueChanged;
      // 
      // BtnRefresh
      // 
      BtnRefresh.Location = new Point(175, 41);
      BtnRefresh.Name = "BtnRefresh";
      BtnRefresh.Size = new Size(125, 23);
      BtnRefresh.TabIndex = 8;
      BtnRefresh.Text = "Refresh (Alg2)";
      BtnRefresh.UseVisualStyleBackColor = true;
      BtnRefresh.Click += btnRefresh_Click;
      // 
      // ThresholdHScroll
      // 
      ThresholdHScroll.Location = new Point(13, 91);
      ThresholdHScroll.Maximum = 255;
      ThresholdHScroll.Name = "ThresholdHScroll";
      ThresholdHScroll.Size = new Size(110, 23);
      ThresholdHScroll.TabIndex = 9;
      ThresholdHScroll.Scroll += ThresholdHScroll_Scroll;
      // 
      // label2
      // 
      label2.AutoSize = true;
      label2.Location = new Point(13, 73);
      label2.Name = "label2";
      label2.Size = new Size(46, 15);
      label2.TabIndex = 10;
      label2.Text = "Thresh:";
      // 
      // label3
      // 
      label3.AutoSize = true;
      label3.Location = new Point(129, 73);
      label3.Name = "label3";
      label3.Size = new Size(60, 15);
      label3.TabIndex = 11;
      label3.Text = "Blob MIN:";
      // 
      // NumBlobMin
      // 
      NumBlobMin.Increment = new decimal(new int[] { 5, 0, 0, 0 });
      NumBlobMin.Location = new Point(195, 70);
      NumBlobMin.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
      NumBlobMin.Name = "NumBlobMin";
      NumBlobMin.Size = new Size(53, 23);
      NumBlobMin.TabIndex = 12;
      NumBlobMin.Value = new decimal(new int[] { 30, 0, 0, 0 });
      NumBlobMin.ValueChanged += NumBlobMin_ValueChanged;
      // 
      // NumBlobMax
      // 
      NumBlobMax.Increment = new decimal(new int[] { 10, 0, 0, 0 });
      NumBlobMax.Location = new Point(323, 70);
      NumBlobMax.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
      NumBlobMax.Name = "NumBlobMax";
      NumBlobMax.Size = new Size(53, 23);
      NumBlobMax.TabIndex = 13;
      NumBlobMax.Value = new decimal(new int[] { 8000, 0, 0, 0 });
      NumBlobMax.ValueChanged += NumBlobMax_ValueChanged;
      // 
      // BtnImageRefresh
      // 
      BtnImageRefresh.Location = new Point(274, 12);
      BtnImageRefresh.Name = "BtnImageRefresh";
      BtnImageRefresh.Size = new Size(26, 23);
      BtnImageRefresh.TabIndex = 14;
      BtnImageRefresh.Text = "X";
      BtnImageRefresh.UseVisualStyleBackColor = true;
      BtnImageRefresh.Click += BtnImageRefresh_Click;
      // 
      // CmbCamera
      // 
      CmbCamera.DropDownStyle = ComboBoxStyle.DropDownList;
      CmbCamera.FormattingEnabled = true;
      CmbCamera.Items.AddRange(new object[] { "0", "1", "2", "3" });
      CmbCamera.Location = new Point(70, 41);
      CmbCamera.Name = "CmbCamera";
      CmbCamera.Size = new Size(99, 23);
      CmbCamera.TabIndex = 15;
      CmbCamera.SelectedIndexChanged += CmbCamera_SelectedIndexChanged;
      // 
      // label4
      // 
      label4.AutoSize = true;
      label4.Location = new Point(13, 44);
      label4.Name = "label4";
      label4.Size = new Size(51, 15);
      label4.TabIndex = 16;
      label4.Text = "Camera:";
      // 
      // label5
      // 
      label5.AutoSize = true;
      label5.Location = new Point(254, 73);
      label5.Name = "label5";
      label5.Size = new Size(63, 15);
      label5.TabIndex = 17;
      label5.Text = "Blob MAX:";
      // 
      // BlobMinScroll
      // 
      BlobMinScroll.Location = new Point(129, 91);
      BlobMinScroll.Maximum = 1000;
      BlobMinScroll.Name = "BlobMinScroll";
      BlobMinScroll.Size = new Size(119, 23);
      BlobMinScroll.TabIndex = 18;
      BlobMinScroll.ValueChanged += BlobMinScroll_ValueChanged;
      // 
      // BlobMaxScroll
      // 
      BlobMaxScroll.Location = new Point(254, 91);
      BlobMaxScroll.Maximum = 10000;
      BlobMaxScroll.Name = "BlobMaxScroll";
      BlobMaxScroll.Size = new Size(122, 23);
      BlobMaxScroll.TabIndex = 19;
      BlobMaxScroll.ValueChanged += BlobMaxScroll_ValueChanged;
      // 
      // BtnGenerateGrid
      // 
      BtnGenerateGrid.Location = new Point(459, 40);
      BtnGenerateGrid.Name = "BtnGenerateGrid";
      BtnGenerateGrid.Size = new Size(75, 23);
      BtnGenerateGrid.TabIndex = 20;
      BtnGenerateGrid.Text = "Gen. Grid";
      BtnGenerateGrid.UseVisualStyleBackColor = true;
      BtnGenerateGrid.Click += BtnGenerateGrid_Click;
      // 
      // Form1
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(596, 388);
      Controls.Add(BtnGenerateGrid);
      Controls.Add(BlobMaxScroll);
      Controls.Add(BlobMinScroll);
      Controls.Add(label5);
      Controls.Add(label4);
      Controls.Add(CmbCamera);
      Controls.Add(BtnImageRefresh);
      Controls.Add(NumBlobMax);
      Controls.Add(NumBlobMin);
      Controls.Add(label3);
      Controls.Add(label2);
      Controls.Add(ThresholdHScroll);
      Controls.Add(BtnRefresh);
      Controls.Add(NumBrightnessThreshold);
      Controls.Add(LblStatus);
      Controls.Add(LblCount);
      Controls.Add(label1);
      Controls.Add(BtnLoadTemplate);
      Controls.Add(_preview);
      Controls.Add(BtnStop);
      Controls.Add(BtnStart);
      Name = "Form1";
      Text = "Supadupa LED Finder";
      Load += Form1_Load;
      ((System.ComponentModel.ISupportInitialize)_preview).EndInit();
      ((System.ComponentModel.ISupportInitialize)NumBrightnessThreshold).EndInit();
      ((System.ComponentModel.ISupportInitialize)NumBlobMin).EndInit();
      ((System.ComponentModel.ISupportInitialize)NumBlobMax).EndInit();
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private Button BtnStart;
    private Button BtnStop;
    private PictureBox _preview;
    private Button BtnLoadTemplate;
    private Label label1;
    private Label LblCount;
    private Label LblStatus;
    private NumericUpDown NumBrightnessThreshold;
    private Button BtnRefresh;
    private HScrollBar ThresholdHScroll;
    private Label label2;
    private Label label3;
    private NumericUpDown NumBlobMin;
    private NumericUpDown NumBlobMax;
    private Button BtnImageRefresh;
    private ComboBox CmbCamera;
    private Label label4;
    private Label label5;
    private HScrollBar BlobMinScroll;
    private HScrollBar BlobMaxScroll;
    private Button BtnGenerateGrid;
  }
}
