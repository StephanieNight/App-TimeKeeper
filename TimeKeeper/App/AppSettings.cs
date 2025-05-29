using TimeKeeper.App.Managers.Calendar;

namespace TimeKeeper.App
{
  class AppSettings
  {
    public AppSettings(){ }
    public AppSettings(bool addDefault = false)
    {
      if (addDefault)
      {
        // Load a default. 
        Projects.Add(new CalendarSettings());
      }
    }
    public string KeeperName { get; set; } = "Keeper";
    public bool ShowDeficit { get; set; }
    public bool ShowTotalWork { get; set; }
    public List<CalendarSettings> Projects { get; set; } = new List<CalendarSettings>();
  }
}
