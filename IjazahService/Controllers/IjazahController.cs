using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IjazahService.Data;
using IjazahService.Models;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

namespace IjazahService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Menambahkan Authorize agar hanya pengguna yang terautentikasi yang bisa mengakses
    public class IjazahController : ControllerBase
    {
        private readonly IjazahDbContext _db;
        private readonly IWebHostEnvironment _env;

        public IjazahController(IjazahDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File kosong");

            if (!file.FileName.EndsWith(".pdf"))
                return BadRequest("Hanya file PDF yang diperbolehkan");

            var uploads = Path.Combine(_env.ContentRootPath, "Uploads");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            //var filePath = Path.Combine(uploads, file.FileName);
            var uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploads, uniqueFileName);


            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var username = User.Identity?.Name;
            var ijazah = new Ijazah
            {
                FileName = uniqueFileName,
                FilePath = filePath,
                UploadedBy = username,
                UploadedAt = DateTime.UtcNow
            };


            _db.Ijazahs.Add(ijazah);
            await _db.SaveChangesAsync();

            return Ok(ijazah);
        }

        [HttpGet("GetAllIjazah")]
        public IActionResult MyIjazahs()
        {
            var username = User.Identity?.Name;
            var files = _db.Ijazahs.Where(x => x.UploadedBy == username).ToList();
            return Ok(files);
        }

        [HttpGet("GetIjazahbyId/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var username = User.Identity?.Name;
            var ijazah = await _db.Ijazahs.FirstOrDefaultAsync(x => x.Id == id && x.UploadedBy == username);

            if (ijazah == null)
                return NotFound("Ijazah tidak ditemukan");

            return Ok(ijazah);
        }

        [HttpDelete("DeleteIjzahbyId/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var username = User.Identity?.Name;
            var ijazah = await _db.Ijazahs.FirstOrDefaultAsync(x => x.Id == id && x.UploadedBy == username);

            if (ijazah == null)
                return NotFound("Ijazah tidak ditemukan");

            // Hapus file fisik jika ada
            if (System.IO.File.Exists(ijazah.FilePath))
            {
                System.IO.File.Delete(ijazah.FilePath);
            }

            _db.Ijazahs.Remove(ijazah);
            await _db.SaveChangesAsync();

            return Ok("Ijazah berhasil dihapus");
        }

        [HttpPut("UpdateIjazahbyId/{id}/rename")]
        public async Task<IActionResult> UpdateFileName(int id, [FromBody] string newFileName)
        {
            if (string.IsNullOrWhiteSpace(newFileName) || !newFileName.EndsWith(".pdf"))
                return BadRequest("Nama file tidak valid atau bukan PDF");

            var username = User.Identity?.Name;
            var ijazah = await _db.Ijazahs.FirstOrDefaultAsync(x => x.Id == id && x.UploadedBy == username);

            if (ijazah == null)
                return NotFound("Ijazah tidak ditemukan");

            var currentPath = ijazah.FilePath;
            var uploadsDir = Path.GetDirectoryName(currentPath);
            var newPath = Path.Combine(uploadsDir!, newFileName);

            if (System.IO.File.Exists(currentPath))
            {
                System.IO.File.Move(currentPath, newPath);
            }

            ijazah.FileName = newFileName;
            ijazah.FilePath = newPath;

            _db.Ijazahs.Update(ijazah);
            await _db.SaveChangesAsync();

            return Ok("Nama file berhasil diperbarui");
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadById(int id)
        {
            var username = User.Identity?.Name;
            var ijazah = await _db.Ijazahs.FirstOrDefaultAsync(x => x.Id == id && x.UploadedBy == username);

            if (ijazah == null || !System.IO.File.Exists(ijazah.FilePath))
                return NotFound("File tidak ditemukan");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(ijazah.FilePath);
            var contentType = "application/pdf";
            var fileName = Path.GetFileName(ijazah.FilePath);

            return File(fileBytes, contentType, fileName);
        }

        [HttpGet("download/all")]
        public async Task<IActionResult> DownloadAll()
        {
            var username = User.Identity?.Name;
            var files = _db.Ijazahs.Where(x => x.UploadedBy == username).ToList();

            if (!files.Any())
                return NotFound("Tidak ada file untuk diunduh");

            using (var memoryStream = new MemoryStream())
            {
                using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in files)
                    {
                        if (System.IO.File.Exists(file.FilePath))
                        {
                            var fileBytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);
                            var zipEntry = zip.CreateEntry(file.FileName, CompressionLevel.Fastest);

                            using (var zipStream = zipEntry.Open())
                            {
                                await zipStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                            }
                        }
                    }
                }

                memoryStream.Position = 0;
                var zipFileName = $"Ijazah_{username}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
                return File(memoryStream.ToArray(), "application/zip", zipFileName);
            }
        }
    }
}
