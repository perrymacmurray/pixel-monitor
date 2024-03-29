﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Management.Automation;
using System.Windows.Forms;

namespace PixelChangeMonitor
{
    static class Program
    {
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        static Color GetColorAtCursor() {
            Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);

            Point location = Point.Empty;
            GetCursorPos(ref location); //location now reflects cursor's position

            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            return screenPixel.GetPixel(0, 0);
        }

        static void Main()
        {
            Thread.Sleep(2000); //Wait 2 seconds before beginning monitoring process

            //Check color every 5 seconds until it changes
            Color initial = GetColorAtCursor();
            do
                Thread.Sleep(1000);
            while (initial == GetColorAtCursor());

            //Color changed, push IFTTT notification
            string key = File.ReadAllText("ifttt_key.txt"); //Gets webhook url from external file

            var results = PowerShell.Create() //Execute request that hooks into IFTTT
                .AddCommand("Invoke-WebRequest")
                .AddParameter("Uri", key)
                .Invoke();

            var msg = results[0].BaseObject.ToString();
            if (!msg.Equals("Congratulations! You've fired the pixel_changed event"))
                MessageBox.Show("Something went wrong notifying IFTTT.", "IFTTT Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
