using System;
using System.Windows.Forms;

namespace _2kurs_5laba {
  internal static class Program {
    [STAThread]
    private static void Main()
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new Form1());
    }
  }
}