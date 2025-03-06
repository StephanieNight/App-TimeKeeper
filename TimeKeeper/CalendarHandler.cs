using System.Globalization;
using System.Text.Json;
using TimeKeeper.Models;

namespace TimeKeeper
{
  class CalendarHandler
  {
    string PathsDays = "Days";

    int ActiveDayId = -1;
    int ActiveMonthIndex = -1;

    FileHandler filesystem;
    MonthModel ActiveMonth = null;


    Dictionary<int, DayModel> Days = new Dictionary<int, DayModel>();

    public CalendarHandler(FileHandler filehandler)
    {
      filesystem = filehandler;
    }

    public List<DayModel> GetDays()
    {
      return Days.Values.ToList();
    }
    public List<DayModel> GetIncomplteDays()
    {
      var DaysNotCompleted = new List<DayModel>();
      foreach (var day in GetDays())
      {
        if (day.IsComplete == false)
        {
          DaysNotCompleted.Add(day);
        }
      }
      return DaysNotCompleted;
    }

    public bool IsDayActive()
    {
      return ActiveDayId != -1;
    }
    public void LoadToday()
    {
      DateTime today = DateTime.Today;
      if (Days.Keys.Contains(today.Day))
      {
        ActiveDayId = today.Day;
      }
    }
    public void ActivateDay(int id)
    {
      if (Days.Keys.Contains(id))
      {
        ActiveDayId = id;
      }

    }
    public void DeActivateDay(int index)
    {
      ActiveDayId = -1;
    }
    public int AddDay()
    {
      DayModel day = new DayModel();
      day.StartTime = DateTime.Now;
      Days.Add(day.Id, day);
      ActivateDay(day.Id);
      return ActiveMonthIndex;
    }
    public void SetDayStart(DateTime startDatetime)
    {
      Days[ActiveDayId].StartTime = startDatetime;
    }
    public void SetDayEnd(DateTime endDatetime)
    {
      Days[ActiveDayId].EndTime = endDatetime;
    }
    public void SetDayLunch(TimeSpan lunchTime)
    {
      Days[ActiveDayId].Lunch = lunchTime;
    }
    public DayModel GetActiveDay()
    {
      return Days[ActiveDayId];
    }
    public void EndDay()
    {
      Days[ActiveDayId].EndTime = DateTime.Now;
    }
    public void Load()
    {
      var days = filesystem.GetFilesInFolder(PathsDays);
      foreach (var dayfile in days)
      {
        DayModel day = filesystem.Deserialize<DayModel>(dayfile);
        Days.Add(day.Id, day);
      }
    }
    public void SaveDays()
    {
      foreach (DayModel day in GetDays())
      {
        filesystem.Serialize<DayModel>($"{PathsDays}/{day.StartTime.Value.ToString("yyyy-MM-dd")}.json", day);
      }
    }
  }
}
