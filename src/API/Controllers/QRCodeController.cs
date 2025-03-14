using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using SkiaSharp.QrCode;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace API.Controllers
{
    [Route("api/qrcode")]
    [ApiController]
    public class QRCodeController : ControllerBase
    {
        /// <summary>
        /// Generates a QR code image in PNG, JPG, or SVG formats.
        /// </summary>
        /// <param name="text" example="Your Text Here">Text to encode in the QR code.</param>
        /// <param name="size" example="350">Size of the QR code in pixels (Optional, default: 300).</param>
        /// <param name="foregroundColor" example="#000000">Foreground color in HEX format (Optional, default: #000000).</param>
        /// <param name="backgroundColor" example="#FFFFFF">Background color in HEX format (Optional, default: #FFFFFF).</param>
        /// <param name="format" example="png">Format of the QR code (png, jpg, svg).</param>
        /// <returns>A QR code image in the requested format.</returns>
        /// <response code="200">Returns the generated QR code</response>
        /// <response code="400">If the input parameters are invalid</response>
        /// <response code="500">If an internal error occurs</response>
        [HttpGet("generate")]
        public IActionResult GenerateQRCode(
            [FromQuery][Required] string text,
            [FromQuery][Required] string format,
            [FromQuery] int? size = 300,
            [FromQuery] string foregroundColor = "#000000",
            [FromQuery] string backgroundColor = "#FFFFFF")
        {
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest("Text cannot be empty.");

            try
            {
                int qrSize = size ?? 300;
                byte[] qrCodeImage = GenerateQRCodeImage(text, qrSize, foregroundColor, backgroundColor, format);
                string mimeType = format.ToLower() == "svg" ? "image/svg+xml" : $"image/{format}";
                return File(qrCodeImage, mimeType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating QR code: {ex.Message}");
            }
        }

        private byte[] GenerateQRCodeImage(string text, int size, string foregroundColor, string backgroundColor, string format)
        {
            var qrCodeGenerator = new QRCodeGenerator();
            using (var qrCodeData = qrCodeGenerator.CreateQrCode(text, ECCLevel.Q))
            {
                SKColor fgColor = SKColor.Parse(foregroundColor);
                SKColor bgColor = SKColor.Parse(backgroundColor);

                if (format.ToLower() == "svg")
                {
                    return GenerateSvgQRCode(qrCodeData, size, fgColor, bgColor);
                }

                return GenerateBitmapQRCode(qrCodeData, size, fgColor, bgColor, format);
            }
        }

        private byte[] GenerateBitmapQRCode(QRCodeData qrCodeData, int size, SKColor fgColor, SKColor bgColor, string format)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(size, size)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(bgColor);

                using (var renderer = new QRCodeRenderer())
                {
                    renderer.Render(canvas, new SKRect(0, 0, size, size), qrCodeData, fgColor, bgColor);
                }

                using (var image = surface.Snapshot())
                using (var data = image.Encode(format.ToLower() == "jpg" ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, 100))
                {
                    return data.ToArray();
                }
            }
        }

        private byte[] GenerateSvgQRCode(QRCodeData qrCodeData, int size, SKColor fgColor, SKColor bgColor)
        {
            var svgBuilder = new StringBuilder();
            svgBuilder.AppendLine($"<svg width=\"{size}\" height=\"{size}\" viewBox=\"0 0 {size} {size}\" xmlns=\"http://www.w3.org/2000/svg\">");
            svgBuilder.AppendLine($"<rect width=\"{size}\" height=\"{size}\" fill=\"{bgColor}\"/>");

            var moduleSize = size / (float)qrCodeData.ModuleMatrix.Count;
            for (int y = 0; y < qrCodeData.ModuleMatrix.Count; y++)
            {
                for (int x = 0; x < qrCodeData.ModuleMatrix.Count; x++)
                {
                    if (qrCodeData.ModuleMatrix[x][y])
                    {
                        svgBuilder.AppendLine($"<rect x=\"{x * moduleSize}\" y=\"{y * moduleSize}\" width=\"{moduleSize}\" height=\"{moduleSize}\" fill=\"{fgColor}\"/>");
                    }
                }
            }

            svgBuilder.AppendLine("</svg>");
            return Encoding.UTF8.GetBytes(svgBuilder.ToString());
        }
    }
}
