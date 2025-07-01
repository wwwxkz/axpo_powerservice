namespace PowerPositionService.Interfaces;

public interface IFileManager
{
    Task WriteToFileAsync(string filePath, Dictionary<string, double> data);
    string GetFileName(string folderPath);
}
