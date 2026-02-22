namespace cronch.Services;

public class FileAccessWrapper
{
    public virtual void WriteAllText(string path, string content) => File.WriteAllText(path, content);

    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    [System.Runtime.Versioning.SupportedOSPlatform("freebsd")]
    public virtual void SetUnixFileMode(string path, UnixFileMode mode) => File.SetUnixFileMode(path, mode);

    public virtual void Delete(string path) => File.Delete(path);
}
