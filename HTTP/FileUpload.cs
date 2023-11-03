using OpenTap;
namespace HTTP;

public class FileUpload
{
    [Display("Key", Order: 1)]
    public string Name { get; set; } = "file";

    [Display("Value", Order: 2)]
    [FilePath(FilePathAttribute.BehaviorChoice.Open)]
    public string Value { get; set; }

    [Display("Content Type", Order: 3)]
    public string ContentType { get; set; } = "application/octet-stream";
}
