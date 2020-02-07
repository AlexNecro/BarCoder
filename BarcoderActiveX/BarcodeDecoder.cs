using System;
using System.Runtime.InteropServices;
using BarCoder;
using ZXing;
using System.Drawing;

namespace BarcoderActiveX
{
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    [Guid("41bf9cfd-d59b-49d6-a39e-db3a1ee58051")]
    public interface IBarcodeDecoder
    {
        string GetBarcodeFromFile(string codeType, string filename);
    }

    public class BarcodeDecoder: IBarcodeDecoder
    {
        public string GetBarcodeFromFile(string codeType, string filename)
        {
            BarcodeFormat format = CodeReader.FormatFromString(codeType);
            CodeReader Reader = new CodeReader(format);

            Bitmap barcodeBitmap = (Bitmap)Image.FromFile(filename, true);
            Result result = Reader.ReadCode(barcodeBitmap);
            barcodeBitmap.Dispose();

            return result.ToString();
        }
    }
}
