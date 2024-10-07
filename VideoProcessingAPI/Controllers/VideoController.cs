using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace VideoProcessingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] VideoUploadModel model)
        {
            if (model.Video == null || model.Video.Length == 0)
            {
                return BadRequest("No video uploaded.");
            }

            var tempInputFile = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.mp4");
            try
            {
                using (var stream = new FileStream(tempInputFile, FileMode.Create))
                {
                    await model.Video.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error saving uploaded video: {ex.Message}");
                return StatusCode(500, "Failed to save uploaded video.");
            }

            var tempOutputFile = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.mp4");

            var arguments = BuildFFmpegArguments(tempInputFile, tempOutputFile, model);
            Console.WriteLine($"FFmpeg Arguments: {arguments}");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            string ffmpegError = string.Empty;

            try
            {
                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();
                    ffmpegError = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        Console.Error.WriteLine($"FFmpeg Error: {ffmpegError}");
                        return StatusCode(500, $"Video processing failed: {ffmpegError}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FFmpeg execution failed: {ex.Message}");
                return StatusCode(500, "Video processing encountered an error.");
            }

            var memory = new MemoryStream();
            try
            {
                using (var stream = new FileStream(tempOutputFile, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error reading processed video: {ex.Message}");
                return StatusCode(500, "Failed to read processed video.");
            }

            try
            {
                System.IO.File.Delete(tempInputFile);
                System.IO.File.Delete(tempOutputFile);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deleting temporary files: {ex.Message}");
            }

            return File(memory, "video/mp4", "processed_video.mp4");
        }

private string BuildFFmpegArguments(string inputFile, string outputFile, VideoUploadModel model)
{
    string textColor = model.TextColor.TrimStart('#');
    int textSize = model.TextSize > 0 ? model.TextSize : 24;

    var filters = new List<string>();

    // NomVideo in top-left corner
    if (!string.IsNullOrEmpty(model.NomVideo))
    {
        filters.Add($"drawtext=text='{EscapeText(model.NomVideo)}':x=10:y=10:fontsize={textSize}:fontcolor=0x{textColor}:box=1:boxcolor=black@0.5");
    }

    // NombreReps in bottom-left corner
    if (!string.IsNullOrEmpty(model.NombreReps))
    {
        filters.Add($"drawtext=text='{EscapeText(model.NombreReps)}':x=10:y=h-th-10:fontsize={textSize}:fontcolor=0x{textColor}:box=1:boxcolor=black@0.5");
    }

    // NbCalories in bottom-right corner
    if (!string.IsNullOrEmpty(model.NbCalories))
    {
        filters.Add($"drawtext=text='{EscapeText(model.NbCalories)}':x=w-tw-10:y=h-th-10:fontsize={textSize}:fontcolor=0x{textColor}:box=1:boxcolor=black@0.5");
    }

    // Simple dynamic countdown timer at the top right
    string timerText = "text='%{pts\\:hms}'";
    filters.Add($"drawtext={timerText}:x=w-tw-10:y=10:fontsize={textSize * 2}:fontcolor=red:box=1:boxcolor=white@0.8:boxborderw=5:rate=30");

    string filterComplex = string.Join(",", filters);

    // Keep the 60-second duration
    return $"-stream_loop -1 -i \"{inputFile}\" -filter_complex \"{filterComplex}\" -t 60 -r 30 -c:a copy \"{outputFile}\"";
}


        private string EscapeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text.Replace("'", "'\\''").Replace(":", "\\:").Replace("\\", "\\\\");
        }
    }

    public class VideoUploadModel
    {
        public IFormFile Video { get; set; }
        public string NomVideo { get; set; }
        public string NombreReps { get; set; }
        public string NbCalories { get; set; }
        public string TextColor { get; set; } = "#FFFFFF";
        public int TextSize { get; set; } = 24;
    }
}
