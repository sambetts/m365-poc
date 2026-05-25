using System.Text;

namespace SPO.ColdStorage.Models;

public class VersionStorageInfo
{
    public long TotalSize { get; set; } = 0;
    public int VersionCount { get; set; } = 0;
}
