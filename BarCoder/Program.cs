using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;
using System.Xml;
using System;

namespace BarCoder
{
    class Program
    {
        static void Encode(ref string[] args)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.PDF_417,
                Options = new EncodingOptions { Margin = 0 }
            };

            writer.Options.Hints.Add(EncodeHintType.ERROR_CORRECTION, 3); //От 0 до 8
            writer.Options.Hints.Add(EncodeHintType.PDF417_COMPACTION, ZXing.PDF417.Internal.Compaction.AUTO);
            var imgBitmap = writer.Write(args[1].PadRight(2, ' '));
            using (var stream = new MemoryStream())
            {
                imgBitmap.Save(stream, ImageFormat.Png);
                System.IO.File.WriteAllBytes(args[2], stream.ToArray());
            }
        }

        static void Decode(ref string[] args)
        {
            int firstFileIndex = 1;
            BarcodeFormat format = CodeReader.FormatFromString(args[1].ToLower());

            if (format != BarcodeFormat.PDF_417)
            {
                firstFileIndex = 2;
            }

            CodeReader Reader = new CodeReader(format);

            XmlDocument doc = new XmlDocument();
            XmlElement root = (XmlElement)doc.CreateElement("images");
            doc.AppendChild(root);
            root.SetAttribute("count", (args.Length - 2).ToString());
            for (int i = firstFileIndex; i < args.Length - 1; ++i)
            {
                XmlElement el = (XmlElement)root.AppendChild(doc.CreateElement("image" + (i - 1).ToString()));
                el.SetAttribute("path", args[i]);

                Console.WriteLine("Читаем " + args[i]);
                Bitmap barcodeBitmap = (Bitmap)Image.FromFile(args[i], true);
                Result result = Reader.ReadCode(barcodeBitmap);
                barcodeBitmap.Dispose();

                if (result == null)
                {
                    el.SetAttribute("code", "");
                } else { 
                    el.SetAttribute("code", result.Text.TrimEnd());
                }
            }
            doc.Save(args[args.Length - 1]);
        }

        static void Main(string[] args)
        {
            if (args.Length >= 3)
            {
                if (args[0].Equals("decode"))
                {
                    Decode(ref args);
                }
                else if (args[0].Equals("encode"))
                {
                    Encode(ref args);
                }
            } else {
                Console.WriteLine("Это версия распознавания штрихкода для 1С");
                Console.WriteLine("запускать так: barcoder.exe decode codetype infile outfile");
                Console.WriteLine("codetype = ean13, qr, code128, code39, codabar");
                Console.WriteLine("если во втором параметре любое другое значение, то считается, что это имя файла, а параметр пропущен. тип кода при этом будет PDF417");
                Console.WriteLine("для получения картинки со штрихкодом так: barcoder.exe encode infile outfile");
                Console.WriteLine("где codetype - это pdf417,qr,ean13");

            }
        }
    }
}
