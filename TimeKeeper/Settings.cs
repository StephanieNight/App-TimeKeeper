using System.Runtime.InteropServices;

namespace TimeKeeper
{
  class Settings
  {
    public string KeeperName { get; set; } = "Keeper";
    public Rounding Rounding { get; set; } = Rounding.None;
    public bool ShowDeficit { get; set; }
    public bool ShowTotalWork { get; set; }
    public string DataLocation
    {
      get
      {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
          return $"{Environment.GetEnvironmentVariable("HOME")}/.timeKeeper";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
          return $"C://TimeKeeper";
        }
        return "";
      }
    }
  }
}
