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
      _btnStart = new Button();
      _btnStop = new Button();
      _preview = new PictureBox();
      btnLoadTemplate = new Button();
      label1 = new Label();
      lblCount = new Label();
      lblStatus = new Label();
      numBrightnessThreshold = new NumericUpDown();
      btnRefresh = new Button();
      ThresholdHScroll = new HScrollBar();
      label2 = new Label();
      label3 = new Label();
      numBlobMin = new NumericUpDown();
      numBlobMax = new NumericUpDown();
      btnImageRefresh = new Button();
      cmboCamera = new ComboBox();
      label4 = new Label();
      label5 = new Label();
      BlobMinScroll = new HScrollBar();
      BlobMaxScroll = new HScrollBar();
      ((System.ComponentModel.ISupportInitialize)_preview).BeginInit();
      ((System.ComponentModel.ISupportInitialize)numBrightnessThreshold).BeginInit();
      ((System.ComponentModel.ISupportInitialize)numBlobMin).BeginInit();
      ((System.ComponentModel.ISupportInitialize)numBlobMax).BeginInit();
      SuspendLayout();
      // 
      // _btnStart
      // 
      _btnStart.Location = new Point(13, 12);
      _btnStart.Name = "_btnStart";
      _btnStart.Size = new Size(75, 23);
      _btnStart.TabIndex = 0;
      _btnStart.Text = "Start";
      _btnStart.UseVisualStyleBackColor = true;
      _btnStart.Click += BtnStart_Click;
      // 
      // _btnStop
      // 
      _btnStop.Location = new Point(94, 12);
      _btnStop.Name = "_btnStop";
      _btnStop.Size = new Size(75, 23);
      _btnStop.TabIndex = 1;
      _btnStop.Text = "Stop";
      _btnStop.UseVisualStyleBackColor = true;
      _btnStop.Click += BtnStop_Click;
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
      // btnLoadTemplate
      // 
      btnLoadTemplate.Location = new Point(175, 12);
      btnLoadTemplate.Name = "btnLoadTemplate";
      btnLoadTemplate.Size = new Size(93, 23);
      btnLoadTemplate.TabIndex = 3;
      btnLoadTemplate.Text = "Load Image";
      btnLoadTemplate.UseVisualStyleBackColor = true;
      btnLoadTemplate.Click += BtnLoadTemplate_Click;
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
      // lblCount
      // 
      lblCount.AutoSize = true;
      lblCount.Location = new Point(341, 16);
      lblCount.Name = "lblCount";
      lblCount.Size = new Size(13, 15);
      lblCount.TabIndex = 5;
      lblCount.Text = "0";
      // 
      // lblStatus
      // 
      lblStatus.AutoSize = true;
      lblStatus.Location = new Point(371, 16);
      lblStatus.Name = "lblStatus";
      lblStatus.Size = new Size(35, 15);
      lblStatus.TabIndex = 6;
      lblStatus.Text = "(null)";
      // 
      // numBrightnessThreshold
      // 
      numBrightnessThreshold.Location = new Point(70, 70);
      numBrightnessThreshold.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
      numBrightnessThreshold.Name = "numBrightnessThreshold";
      numBrightnessThreshold.Size = new Size(53, 23);
      numBrightnessThreshold.TabIndex = 7;
      numBrightnessThreshold.Value = new decimal(new int[] { 250, 0, 0, 0 });
      numBrightnessThreshold.ValueChanged += numBrightnessThreshold_ValueChanged;
      // 
      // btnRefresh
      // 
      btnRefresh.Location = new Point(175, 41);
      btnRefresh.Name = "btnRefresh";
      btnRefresh.Size = new Size(125, 23);
      btnRefresh.TabIndex = 8;
      btnRefresh.Text = "Refresh (Alg2)";
      btnRefresh.UseVisualStyleBackColor = true;
      btnRefresh.Click += btnRefresh_Click;
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
      // numBlobMin
      // 
      numBlobMin.Increment = new decimal(new int[] { 5, 0, 0, 0 });
      numBlobMin.Location = new Point(195, 70);
      numBlobMin.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
      numBlobMin.Name = "numBlobMin";
      numBlobMin.Size = new Size(53, 23);
      numBlobMin.TabIndex = 12;
      numBlobMin.Value = new decimal(new int[] { 30, 0, 0, 0 });
      numBlobMin.ValueChanged += numBlobMin_ValueChanged;
      // 
      // numBlobMax
      // 
      numBlobMax.Increment = new decimal(new int[] { 10, 0, 0, 0 });
      numBlobMax.Location = new Point(323, 70);
      numBlobMax.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
      numBlobMax.Name = "numBlobMax";
      numBlobMax.Size = new Size(53, 23);
      numBlobMax.TabIndex = 13;
      numBlobMax.Value = new decimal(new int[] { 8000, 0, 0, 0 });
      numBlobMax.ValueChanged += numBlobMax_ValueChanged;
      // 
      // btnImageRefresh
      // 
      btnImageRefresh.Location = new Point(274, 12);
      btnImageRefresh.Name = "btnImageRefresh";
      btnImageRefresh.Size = new Size(26, 23);
      btnImageRefresh.TabIndex = 14;
      btnImageRefresh.Text = "X";
      btnImageRefresh.UseVisualStyleBackColor = true;
      btnImageRefresh.Click += btnImageRefresh_Click;
      // 
      // cmboCamera
      // 
      cmboCamera.Enabled = false;
      cmboCamera.FormattingEnabled = true;
      cmboCamera.Location = new Point(70, 41);
      cmboCamera.Name = "cmboCamera";
      cmboCamera.Size = new Size(99, 23);
      cmboCamera.TabIndex = 15;
      // 
      // label4
      // 
      label4.AutoSize = true;
      label4.Enabled = false;
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
      BlobMinScroll.Maximum = 10000;
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
      // Form1
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(596, 388);
      Controls.Add(BlobMaxScroll);
      Controls.Add(BlobMinScroll);
      Controls.Add(label5);
      Controls.Add(label4);
      Controls.Add(cmboCamera);
      Controls.Add(btnImageRefresh);
      Controls.Add(numBlobMax);
      Controls.Add(numBlobMin);
      Controls.Add(label3);
      Controls.Add(label2);
      Controls.Add(ThresholdHScroll);
      Controls.Add(btnRefresh);
      Controls.Add(numBrightnessThreshold);
      Controls.Add(lblStatus);
      Controls.Add(lblCount);
      Controls.Add(label1);
      Controls.Add(btnLoadTemplate);
      Controls.Add(_preview);
      Controls.Add(_btnStop);
      Controls.Add(_btnStart);
      Name = "Form1";
      Text = "Supadupa LED Finder";
      Load += Form1_Load;
      ((System.ComponentModel.ISupportInitialize)_preview).EndInit();
      ((System.ComponentModel.ISupportInitialize)numBrightnessThreshold).EndInit();
      ((System.ComponentModel.ISupportInitialize)numBlobMin).EndInit();
      ((System.ComponentModel.ISupportInitialize)numBlobMax).EndInit();
      ResumeLayout(false);
      PerformLayout();
    }

    #endregion

    private Button _btnStart;
    private Button _btnStop;
    private PictureBox _preview;
    private Button btnLoadTemplate;
    private Label label1;
    private Label lblCount;
    private Label lblStatus;
    private NumericUpDown numBrightnessThreshold;
    private Button btnRefresh;
    private HScrollBar ThresholdHScroll;
    private Label label2;
    private Label label3;
    private NumericUpDown numBlobMin;
    private NumericUpDown numBlobMax;
    private Button btnImageRefresh;
    private ComboBox cmboCamera;
    private Label label4;
    private Label label5;
    private HScrollBar BlobMinScroll;
    private HScrollBar BlobMaxScroll;
  }
}
