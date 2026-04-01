using System;
using System.Windows.Forms;

namespace VisionLedTest;

public partial class Form1 : Form
{
  public Form1()
  {
    InitializeComponent();
  }

  private void button1_Click(object sender, EventArgs e)
  {
    // var cv = new LedDetectDemo();
    LedDetectDemo.MainTest();
  }
}
