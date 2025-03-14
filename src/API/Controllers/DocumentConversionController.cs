using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using IronPdf;
using IronXL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Newtonsoft.Json;

namespace DocumentConverterAPI.Controllers
{
    [ApiController]
    [Route("api/convert")]
    public class DocumentConversionController : ControllerBase
    {
        // Endpoint: Convert DOCX to PDF
        [HttpPost("docx-to-pdf")]
        public async Task<IActionResult> ConvertDocxToPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                var tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var pdf = new ChromePdfRenderer();
                var pdfDocument = pdf.RenderHtmlAsPdf($"<iframe src='{tempFilePath}' style='width:100%; height:100%;'></iframe>");
                var result = pdfDocument.BinaryData;

                System.IO.File.Delete(tempFilePath);

                return File(result, "application/pdf", "converted.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Endpoint: Convert PDF to DOCX
        [HttpPost("pdf-to-docx")]
        public async Task<IActionResult> ConvertPdfToDocx(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                var tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var pdf = new PdfDocument(tempFilePath);
                var text = pdf.ExtractAllText();

                var docxFilePath = Path.ChangeExtension(tempFilePath, ".docx");
                System.IO.File.WriteAllText(docxFilePath, text);

                var result = await System.IO.File.ReadAllBytesAsync(docxFilePath);

                System.IO.File.Delete(tempFilePath);
                System.IO.File.Delete(docxFilePath);

                return File(result, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "converted.docx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Endpoint: Convert Image to PDF
        [HttpPost("image-to-pdf")]
        public async Task<IActionResult> ConvertImageToPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                var tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                using (var image = Image.Load(tempFilePath))
                {
                    var pdf = new ChromePdfRenderer();
                    var pdfDocument = pdf.RenderHtmlAsPdf($"<img src='{tempFilePath}' />");
                    var result = pdfDocument.BinaryData;

                    System.IO.File.Delete(tempFilePath);

                    return File(result, "application/pdf", "converted.pdf");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Endpoint: Convert PDF to Image
        [HttpPost("pdf-to-image")]
        public async Task<IActionResult> ConvertPdfToImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                var tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var pdf = new PdfDocument(tempFilePath);

                // Convert the first page of the PDF to an image
                var imageFilePaths = pdf.RasterizeToImageFiles(tempFilePath + "_*.png");

                if (imageFilePaths.Length == 0)
                    return StatusCode(500, "Failed to convert PDF to image.");

                var imageFilePath = imageFilePaths[0]; // Use the first page's image
                var result = await System.IO.File.ReadAllBytesAsync(imageFilePath);

                // Clean up temporary files
                System.IO.File.Delete(tempFilePath);
                foreach (var path in imageFilePaths)
                {
                    System.IO.File.Delete(path);
                }

                return File(result, "image/png", "converted.png");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Endpoint: Convert Excel to CSV
        [HttpPost("excel-to-csv")]
        public async Task<IActionResult> ConvertExcelToCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                var tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var workbook = WorkBook.Load(tempFilePath);
                var worksheet = workbook.DefaultWorkSheet;
                var csv = new System.Text.StringBuilder();

                // Iterate through rows and columns to build CSV
                for (int row = 0; row < worksheet.RowCount; row++)
                {
                    var rowData = new List<string>();
                    for (int col = 0; col < worksheet.ColumnCount; col++)
                    {
                        var cell = worksheet.Rows[row].Columns[col];
                        var cellValue = cell?.Value?.ToString() ?? "";
                        rowData.Add(cellValue);
                    }
                    csv.AppendLine(string.Join(",", rowData));
                }

                var result = System.Text.Encoding.UTF8.GetBytes(csv.ToString());

                System.IO.File.Delete(tempFilePath);

                return File(result, "text/csv", "converted.csv");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Endpoint: Convert CSV to Excel
        [HttpPost("csv-to-excel")]
        public async Task<IActionResult> ConvertCsvToExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                var tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var workbook = WorkBook.LoadCSV(tempFilePath, fileFormat: ExcelFileFormat.XLSX);
                var excelFilePath = Path.ChangeExtension(tempFilePath, ".xlsx");
                workbook.SaveAs(excelFilePath);

                var result = await System.IO.File.ReadAllBytesAsync(excelFilePath);

                System.IO.File.Delete(tempFilePath);
                System.IO.File.Delete(excelFilePath);

                return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "converted.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Endpoint: Convert CSV to JSON
        [HttpPost("csv-to-json")]
        public async Task<IActionResult> ConvertCsvToJson(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                var tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var csv = await System.IO.File.ReadAllTextAsync(tempFilePath);
                var rows = csv.Split('\n');
                var headers = rows[0].Split(',');
                var json = new List<Dictionary<string, string>>();

                for (int i = 1; i < rows.Length; i++)
                {
                    var values = rows[i].Split(',');
                    var rowDict = new Dictionary<string, string>();
                    for (int j = 0; j < headers.Length; j++)
                    {
                        rowDict[headers[j]] = values[j];
                    }
                    json.Add(rowDict);
                }

                var result = JsonConvert.SerializeObject(json);

                System.IO.File.Delete(tempFilePath);

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
