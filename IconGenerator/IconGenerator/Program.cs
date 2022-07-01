using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace IconGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<char> list = new List<char>();
            for (char c = 'A'; c <= 'Z'; ++c)
            {
                list.Add(c);
            }
            for (var i = 0; i < 10; i++)
            {
                list.Add(i.ToString().ToCharArray()[0]);
            }

            for (var i1 = 0; i1 < list.Count; i1++)
            {
                var i = list[i1];
                using (Bitmap b = new Bitmap(256, 256))
                {
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        //RectangleF rectf = new RectangleF(10, -15, 256, 256);
                        RectangleF rectf = new RectangleF(5, 00, 256, 316);

                           var color = Color.FromArgb(182, 137, 255); // фиолетовый
                        //  var color = Color.FromArgb(246, 255, 132); //жёлтый
                        // var color = Color.FromArgb(145, 255, 242); // голубой
                        g.Clear(color);

                        g.DrawRectangle(new Pen(Brushes.Black, 6), new Rectangle(0, 0, 256, 256));
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                        StringFormat format = new StringFormat()
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };

                        var color2 = Brushes.Black;
                        //var color2 = Brushes.Red;
                        g.DrawString("" + i, new Font("Arial", 212), Brushes.Black, rectf, format);
                    }
                    var name = @"C:\Users\Max\Pictures\solution icon switcher\out";
                    var dir = name+"\\png\\";
                    var dir2 = name+"\\ico\\";
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    if (!Directory.Exists(dir2))
                    {
                        Directory.CreateDirectory(dir2);
                    }
                    var path1 = dir + "\\" + i + ".png";
                    var path2 = dir2 + "\\" + i + ".ico";
                    b.Save(path1, ImageFormat.Png);
                    PngIconConverter.Convert(path1, path2, 256);
                }
            }
        }

        class PngIconConverter
        {

            //https://gist.github.com/darkfall/1656050
            /* input image with width = height is suggested to get the best result */
            /* png support in icon was introduced in Windows Vista */
            public static bool Convert(System.IO.Stream input_stream, System.IO.Stream output_stream, int size, bool keep_aspect_ratio = false)
            {
                System.Drawing.Bitmap input_bit = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromStream(input_stream);
                if (input_bit != null)
                {
                    int width, height;
                    if (keep_aspect_ratio)
                    {
                        width = size;
                        height = input_bit.Height / input_bit.Width * size;
                    }
                    else
                    {
                        width = height = size;
                    }
                    System.Drawing.Bitmap new_bit = new System.Drawing.Bitmap(input_bit, new System.Drawing.Size(width, height));
                    if (new_bit != null)
                    {
                        // save the resized png into a memory stream for future use
                        System.IO.MemoryStream mem_data = new System.IO.MemoryStream();
                        new_bit.Save(mem_data, System.Drawing.Imaging.ImageFormat.Png);

                        System.IO.BinaryWriter icon_writer = new System.IO.BinaryWriter(output_stream);
                        if (output_stream != null && icon_writer != null)
                        {
                            // 0-1 reserved, 0
                            icon_writer.Write((byte)0);
                            icon_writer.Write((byte)0);

                            // 2-3 image type, 1 = icon, 2 = cursor
                            icon_writer.Write((short)1);

                            // 4-5 number of images
                            icon_writer.Write((short)1);

                            // image entry 1
                            // 0 image width
                            icon_writer.Write((byte)width);
                            // 1 image height
                            icon_writer.Write((byte)height);

                            // 2 number of colors
                            icon_writer.Write((byte)0);

                            // 3 reserved
                            icon_writer.Write((byte)0);

                            // 4-5 color planes
                            icon_writer.Write((short)0);

                            // 6-7 bits per pixel
                            icon_writer.Write((short)32);

                            // 8-11 size of image data
                            icon_writer.Write((int)mem_data.Length);

                            // 12-15 offset of image data
                            icon_writer.Write((int)(6 + 16));

                            // write image data
                            // png data must contain the whole png data file
                            icon_writer.Write(mem_data.ToArray());

                            icon_writer.Flush();

                            return true;
                        }
                    }
                    return false;
                }
                return false;
            }

            public static bool Convert(string input_image, string output_icon, int size, bool keep_aspect_ratio = false)
            {
                System.IO.FileStream input_stream = new System.IO.FileStream(input_image, System.IO.FileMode.Open);
                System.IO.FileStream output_stream = new System.IO.FileStream(output_icon, System.IO.FileMode.OpenOrCreate);

                bool result = Convert(input_stream, output_stream, size, keep_aspect_ratio);

                input_stream.Close();
                output_stream.Close();

                return result;
            }
        }
    }
}
