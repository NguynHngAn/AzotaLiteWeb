using Microsoft.Extensions.Options;

namespace AzotaLite.Web.Services
{
    public class StorageOptions
    {
        public string ExamRoot { get; set; } = "App_Data/Exams";
        public long MaxExamSizeBytes { get; set; } = 10 * 1024 * 1024;
        public string[] AllowedContentTypes { get; set; } = Array.Empty<string>();
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    }

    public class LocalFileStorage : IFileStorage
    {
        private readonly StorageOptions _opt;

        public LocalFileStorage(IOptions<StorageOptions> opt)
        {
            _opt = opt.Value;
            Directory.CreateDirectory(_opt.ExamRoot);
        }

        public async Task<(string storedName, long size)> SaveExamAsync(
            Stream stream, string originalFileName, string contentType, CancellationToken ct = default)
        {
            var ext = Path.GetExtension(originalFileName).ToLowerInvariant();

            if (_opt.AllowedContentTypes?.Length > 0 && !_opt.AllowedContentTypes.Contains(contentType))
                throw new InvalidOperationException($"Content-Type '{contentType}' không được phép.");

            if (_opt.AllowedExtensions?.Length > 0 && !_opt.AllowedExtensions.Contains(ext))
                throw new InvalidOperationException($"Đuôi file '{ext}' không được phép.");

            // Đo kích thước
            long size;
            if (stream.CanSeek)
            {
                size = stream.Length;
            }
            else
            {
                // copy qua MemoryStream để biết size (trường hợp không Seekable)
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                size = ms.Length;
                ms.Position = 0;
                stream = new MemoryStream(ms.ToArray());
            }

            if (size <= 0 || size > _opt.MaxExamSizeBytes)
                throw new InvalidOperationException($"Kích thước file không hợp lệ (<=0 hoặc > {_opt.MaxExamSizeBytes} bytes).");

            // Tạo tên file lưu trữ ngẫu nhiên
            var storedName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(_opt.ExamRoot, storedName);

            // Ghi file
            stream.Position = 0;
            using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fs, ct);
            }

            return (storedName, size);
        }

        public Task<Stream?> OpenExamAsync(string storedName, CancellationToken ct = default)
        {
            var fullPath = Path.Combine(_opt.ExamRoot, storedName);
            if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);
            Stream s = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult<Stream?>(s);
        }

        public Task DeleteExamAsync(string storedName, CancellationToken ct = default)
        {
            var fullPath = Path.Combine(_opt.ExamRoot, storedName);
            if (File.Exists(fullPath)) File.Delete(fullPath);
            return Task.CompletedTask;
        }
    }
}
