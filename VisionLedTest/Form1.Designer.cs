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
      ((System.ComponentModel.ISupportInitialize)_preview).BeginInit();
      ((System.ComponentModel.ISupportInitialize)numBrightnessThreshold).BeginInit();
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
      _preview.Location = new Point(12, 67);
      _preview.Name = "_preview";
      _preview.Size = new Size(553, 292);
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
      label1.Location = new Point(283, 16);
      label1.Name = "label1";
      label1.Size = new Size(29, 15);
      label1.TabIndex = 4;
      label1.Text = "Cnt:";
      // 
      // lblCount
      // 
      lblCount.AutoSize = true;
      lblCount.Location = new Point(318, 16);
      lblCount.Name = "lblCount";
      lblCount.Size = new Size(13, 15);
      lblCount.TabIndex = 5;
      lblCount.Text = "0";
      // 
      // lblStatus
      // 
      lblStatus.AutoSize = true;
      lblStatus.Location = new Point(367, 16);
      lblStatus.Name = "lblStatus";
      lblStatus.Size = new Size(35, 15);
      lblStatus.TabIndex = 6;
      lblStatus.Text = "(null)";
      // 
      // numBrightnessThreshold
      // 
      numBrightnessThreshold.Location = new Point(13, 38);
      numBrightnessThreshold.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
      numBrightnessThreshold.Name = "numBrightnessThreshold";
      numBrightnessThreshold.Size = new Size(120, 23);
      numBrightnessThreshold.TabIndex = 7;
      numBrightnessThreshold.Value = new decimal(new int[] { 250, 0, 0, 0 });
      numBrightnessThreshold.ValueChanged += numBrightnessThreshold_ValueChanged;
      // 
      // btnRefresh
      // 
      btnRefresh.Location = new Point(139, 38);
      btnRefresh.Name = "btnRefresh";
      btnRefresh.Size = new Size(75, 23);
      btnRefresh.TabIndex = 8;
      btnRefresh.Text = "Refresh";
      btnRefresh.UseVisualStyleBackColor = true;
      btnRefresh.Click += btnRefresh_Click;
      // 
      // Form1
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(577, 371);
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
      Text = "Form1";
      Load += Form1_Load;
      ((System.ComponentModel.ISupportInitialize)_preview).EndInit();
      ((System.ComponentModel.ISupportInitialize)numBrightnessThreshold).EndInit();
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
  }
}
