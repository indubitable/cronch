using System.Diagnostics;

namespace cronch.Services;

public class ProcessFactory
{
    public virtual ProcessWrapper Create(ProcessStartInfo startInfo) => new(startInfo);
}
