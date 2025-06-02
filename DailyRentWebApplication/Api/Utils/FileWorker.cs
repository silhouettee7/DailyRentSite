using Microsoft.AspNetCore.StaticFiles;

namespace Api.Utils;

public class FileWorker
{
    private readonly FileExtensionContentTypeProvider _fileExtensionContentTypeProvider = new ();
    
    public string DefineMimeType(string fileName)
    {
        if (!_fileExtensionContentTypeProvider.TryGetContentType(fileName, out var mimeType))
        {
            mimeType = "application/octet-stream";
        }
        return mimeType;
    }
}