using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using ZXing;

namespace BarCoder
{
    public class CodeReader
    {
        IBarcodeReader reader;

        public CodeReader(BarcodeFormat format)
        {
            reader = new BarcodeReader();
            reader.Options.PossibleFormats = new List<BarcodeFormat>();
            reader.Options.PossibleFormats.Add(format /*BarcodeFormat.PDF_417*/);
            reader.Options.TryHarder = true;
        }

        public static BarcodeFormat FormatFromString(string strFormat)
        {
            BarcodeFormat format = BarcodeFormat.PDF_417;

            if (strFormat == "ean13")
            {
                format = BarcodeFormat.EAN_13;
            }
            else if (strFormat == "qr")
            {
                format = BarcodeFormat.QR_CODE;
            }
            else if (strFormat == "code128")
            {
                format = BarcodeFormat.CODE_128;
            }
            else if (strFormat == "code39")
            {
                format = BarcodeFormat.CODE_39;
            }
            else if (strFormat == "codabar")
            {
                format = BarcodeFormat.CODABAR;
            }

            return format;
        }

        Bitmap RotateBitmap(Bitmap bitmap, float angle)
        {
            Bitmap rotatedBitmap = new Bitmap(bitmap.Width, bitmap.Height);
            using (Graphics g = Graphics.FromImage(rotatedBitmap))
            {
                g.TranslateTransform(bitmap.Width / 2, bitmap.Height / 2);
                g.RotateTransform(angle);
                g.TranslateTransform(-bitmap.Width / 2, -bitmap.Height / 2);
                g.DrawImage(bitmap, new Point(0, 0));
            }
            return rotatedBitmap;
        }

        Result CheckRegion(ref Bitmap barcodeBitmap, ref int debugNum)
        {
            //Сюда попадает уже вырезанный кусок картинки
            Result result = null;

            if (debugNum != 0)
            {
                if (debugNum != 0) barcodeBitmap.Save("debug_" + debugNum + ".jpg", ImageFormat.Jpeg);
                debugNum++;
            }

            try
            {
                result = reader.Decode(barcodeBitmap);
            }
            catch
            {
                //Do nothing...
            }

            float angle = 0.5f;

            //немного поворачиваем, вдруг криво стоит
            while ((result == null) && (angle < 3.0f))
            {
                try
                {
                    result = reader.Decode(RotateBitmap(barcodeBitmap, angle));
                }
                catch
                {
                    //Do nothing...
                }
                if (result == null)
                {
                    try
                    {
                        result = reader.Decode(RotateBitmap(barcodeBitmap, -angle));
                    }
                    catch
                    {
                        //Do nothing...
                    }
                }
                angle += 0.5f;
            }
            return result;
        }

        static Bitmap Sharpen(Bitmap bitmap)
        {
            Bitmap sharnepBitmap = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), bitmap.PixelFormat);
            for (int i = 0; i < sharnepBitmap.Width; i++)
            {
                for (int j = 0; j < sharnepBitmap.Height; j++)
                {
                    var currentPixel = sharnepBitmap.GetPixel(i, j);
                    if ((currentPixel.R <= 155) && (currentPixel.G <= 155) && (currentPixel.B <= 155))
                    {
                        sharnepBitmap.SetPixel(i, j, Color.FromArgb(0, 0, 0));
                    }
                }
            }
            return sharnepBitmap;
        }

        public Result ReadCode(Bitmap barcodeBitmap)
        {
            Rectangle[] regions0 = new Rectangle[2];
            regions0[0] = new Rectangle(0, 0, 1000 / 2, 1000 / 10); //topleft
            regions0[1] = new Rectangle(1000 / 2, 0, 1000, 1000 / 10); //topright

            Rectangle[] regions90 = new Rectangle[1];
            regions90[0] = new Rectangle(2000 / 3, 0, 1000, 1000 / 7); //topright

            Bitmap rotatedBitmap;
            Result result = null;
            int debugNum = 0;

            Debug.WriteLine("0 deg");
            result = CheckRegions(ref barcodeBitmap, regions0, ref debugNum);

            if (result == null)
            {
                rotatedBitmap = barcodeBitmap.Clone(new Rectangle(0, 0, barcodeBitmap.Width, barcodeBitmap.Height), barcodeBitmap.PixelFormat);

                rotatedBitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);

                if (debugNum != 0) rotatedBitmap.Save("debug_d180.jpg", ImageFormat.Jpeg);
                Debug.WriteLine("180 deg");
                result = CheckRegions(ref rotatedBitmap, regions0, ref debugNum);

            }


            if (result == null)
            {
                rotatedBitmap = barcodeBitmap.Clone(new Rectangle(0, 0, barcodeBitmap.Width, barcodeBitmap.Height), barcodeBitmap.PixelFormat);

                rotatedBitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);

                if (debugNum != 0) rotatedBitmap.Save("debug_d90.jpg", ImageFormat.Jpeg);
                Debug.WriteLine("90 deg");
                result = CheckRegions(ref rotatedBitmap, regions90, ref debugNum);
            }

            if (result == null)
            {
                rotatedBitmap = barcodeBitmap.Clone(new Rectangle(0, 0, barcodeBitmap.Width, barcodeBitmap.Height), barcodeBitmap.PixelFormat);

                rotatedBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);

                if (debugNum != 0) rotatedBitmap.Save("debug_d270.jpg", ImageFormat.Jpeg);
                Debug.WriteLine("270 deg");
                result = CheckRegions(ref rotatedBitmap, regions90, ref debugNum);
            }

            return result;
        }

        private Result CheckRegions(ref Bitmap barcodeBitmap, Rectangle[] regions, ref int debugNum)
        {
            Bitmap partBitmap = null;
            Result result = null;

            foreach (Rectangle rectangle in regions)
            {
                Rectangle CroppedRect = new Rectangle();
                CroppedRect.X = barcodeBitmap.Width * rectangle.X / 1000;
                CroppedRect.Y = barcodeBitmap.Height * rectangle.Y / 1000;
                CroppedRect.Width = barcodeBitmap.Width * rectangle.Width / 1000 - CroppedRect.X;
                CroppedRect.Height = barcodeBitmap.Height * rectangle.Height / 1000 - CroppedRect.Y;

                partBitmap = barcodeBitmap.Clone(CroppedRect, barcodeBitmap.PixelFormat);

                result = CheckRegion(ref partBitmap, ref debugNum);

                Debug.WriteLine("" + CroppedRect + " - " + result);

                if (result != null)
                {
                    partBitmap.Dispose();
                    return result;
                }

            };

            return result;
        }
    }
}