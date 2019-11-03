using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.IO;
using System.Diagnostics;

namespace ManiacEDv2
{
    public static class Extensions
    {

        public static string GetMemoryUsage() // KLUDGE but works
        {
            try
            {
                string fname = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

                ProcessStartInfo ps = new ProcessStartInfo("tasklist");
                ps.Arguments = "/fi \"IMAGENAME eq " + fname + ".*\" /FO CSV /NH";
                ps.RedirectStandardOutput = true;
                ps.CreateNoWindow = true;
                ps.UseShellExecute = false;
                var p = Process.Start(ps);
                if (p.WaitForExit(1000))
                {
                    var s = p.StandardOutput.ReadToEnd().Split('\"');
                    return s[9].Replace("\"", "");
                }
            }
            catch { }
            return "Unable to get memory usage";
        }

        public static System.Drawing.Color Blend(this System.Drawing.Color color, System.Drawing.Color backcolor, double amount)
        {
            byte r = (byte)((color.R * amount) + backcolor.R * (1 - amount));
            byte g = (byte)((color.G * amount) + backcolor.G * (1 - amount));
            byte b = (byte)((color.B * amount) + backcolor.B * (1 - amount));
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        public static System.Drawing.Bitmap ChangeImageColor(Bitmap source, System.Drawing.Color OldColor, System.Drawing.Color NewColor)
        {
            Bitmap Result = new Bitmap(source.Width, source.Height);
            Graphics g = Graphics.FromImage(Result);
            using (Bitmap bmp = new Bitmap(source))
            {

                // Set the image attribute's color mappings
                System.Drawing.Imaging.ColorMap[] colorMap = new System.Drawing.Imaging.ColorMap[1];
                colorMap[0] = new System.Drawing.Imaging.ColorMap();
                colorMap[0].OldColor = OldColor;
                colorMap[0].NewColor = NewColor;
                System.Drawing.Imaging.ImageAttributes attr = new System.Drawing.Imaging.ImageAttributes();
                attr.SetRemapTable(colorMap);
                // Draw using the color map
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
                g.DrawImage(bmp, rect, 0, 0, rect.Width, rect.Height, GraphicsUnit.Pixel, attr);
            }
            return Result;
        }
    }
}
