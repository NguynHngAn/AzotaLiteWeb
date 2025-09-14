namespace AzotaLite.Web.Services
{
    public interface IFileStorage
    {
        Task<(string storedName, long size)> SaveExamAsync(
            Stream stream, string originalFileName, string contentType, CancellationToken ct = default);

        Task<Stream?> OpenExamAsync(string storedName, CancellationToken ct = default);

        Task DeleteExamAsync(string storedName, CancellationToken ct = default);
    }
}
