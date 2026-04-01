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
      ((System.ComponentModel.ISupportInitialize)_preview).BeginInit();
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
      _btnStop.Location = new Point(104, 12);
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
      _preview.Location = new Point(12, 41);
      _preview.Name = "_preview";
      _preview.Size = new Size(553, 318);
      _preview.TabIndex = 2;
      _preview.TabStop = false;
      // 
      // Form1
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(577, 371);
      Controls.Add(_preview);
      Controls.Add(_btnStop);
      Controls.Add(_btnStart);
      Name = "Form1";
      Text = "Form1";
      ((System.ComponentModel.ISupportInitialize)_preview).EndInit();
      ResumeLayout(false);
    }

    #endregion

    private Button _btnStart;
    private Button _btnStop;
    private PictureBox _preview;
  }
}
