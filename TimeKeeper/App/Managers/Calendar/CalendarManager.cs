using TimeKeeper.App.Common.Extensions;
using TimeKeeper.App.Common.Filesystem;
using TimeKeeper.App.Managers.Calendar.Enums;
using TimeKeeper.App.Managers.Calendar.Models;

namespace TimeKeeper.App.Managers.Calendar
{
  class CalendarManager
  {
    string PathsData = "Data";

    int ActiveDayId = -1;
    int ActiveMonthId = -1;
    int ActiveYearId = -1;

    bool IsOnBreak = false;

    CalendarSettings Settings;
    FileSystemManager Filesystem;

    Dictionary<int, YearModel> Years = new Dictionary<int, YearModel>();

    public string Status
    {
      get
      {
        if (IsOnBreak)
        {
          return "On break!".ToUpper();
        }
        DayModel day = GetActiveDay();
        if (day != null)
        {
          if (day.IsComplete && day.EndTime < DateTime.Now)
          {
            return "Day Completed!".ToUpper();
          }
          return "Working!".ToUpper();
        }
        return "";
      }
    }

    public CalendarManager(FileSystemManager filesystem, CalendarSettings calendarSettings)
    {
      PathsData += $"/{calendarSettings.Name}";
      Settings = calendarSettings;
      Filesystem = filesystem;
      Filesystem.InitializeFolder($"{Filesystem.BasePath}/{PathsData}");
    }

    public List<DayModel> GetDays()
    {
      if (IsYearActive())
      {
        if (IsMonthActive())
        {
          return Years[ActiveYearId].GetMonth(ActiveMonthId).GetDays();
        }
      }
      return new List<DayModel>();
    }
    public List<MonthModel> GetMonths()
    {
      if (IsYearActive())
      {
        return Years[ActiveYearId].GetMonths();
      }
      return new List<MonthModel>();
    }
    public List<YearModel> GetYears()
    {

      return Years.Values.ToList();
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

    public void AddExpectedWorkWeek(Dictionary<DayOfWeek, TimeSpan> expectedWorkWeek)
    {
      // Load saved expected work week into dictionary.
      foreach (var work in expectedWorkWeek)
      {
        Settings.ExpectedWorkWeek.Add(work.Key, work.Value);
      }
    }

    public TimeSpan GetExpectedWorkDay(DayOfWeek dayOfWeek)
    {
      if (Settings.ExpectedWorkWeek.ContainsKey(dayOfWeek))
      {
        return Settings.ExpectedWorkWeek[dayOfWeek];
      }
      return GetDefaultExpectedWorkDay(dayOfWeek);
    }
    public TimedSegment[] GetPlannedBreaks(DateOnly date)
    {
      List<TimedSegment> breaks = new List<TimedSegment>();
      
      foreach (var planned in Settings.PlannedBreaks)
      {
        if (planned.ActiveOnDays.Contains(date.DayOfWeek))
        {
          TimedSegment b = new TimedSegment();
          b.StartTime = new DateTime(date, planned.Start);
          b.EndTime = new DateTime(date, planned.End);
          b.Name = planned.Name;
          breaks.Add(b);
        }
      }
      return breaks.ToArray();
    }
    public TimeSpan GetDefaultExpectedWorkDay(DayOfWeek dayOfWeek)
    {
      switch (dayOfWeek)
      {
        case DayOfWeek.Monday:
        case DayOfWeek.Tuesday:
        case DayOfWeek.Wednesday:
        case DayOfWeek.Thursday:
          return new TimeSpan(7, 30, 0);
        case DayOfWeek.Friday:
          return new TimeSpan(7, 0, 0);
        case DayOfWeek.Saturday:
        case DayOfWeek.Sunday:
          return new TimeSpan(0, 0, 0);
        default:
          return new TimeSpan(0, 0, 0);
      }
    }

    public bool IsYearActive()
    {
      return Years.ContainsKey(ActiveYearId);
    }
    public bool IsMonthActive()
    {
      if (IsYearActive())
      {
        return Years[ActiveYearId].ContainMonthId(ActiveMonthId);
      }
      return false;
    }
    public bool IsDayActive()
    {
      if (IsYearActive())
      {
        if (IsMonthActive())
        {
          return GetActiveYear().GetMonth(ActiveMonthId).ContainDayId(ActiveDayId);
        }
      }
      return false;
    }

    public void ActivateToday(DateTime today = new DateTime())
    {
      if (today == new DateTime())
      {
        today = DateTime.Today;
      }
      ActivateYear(today.Year);
      ActivateMonth(today.Month);
      ActivateDay(today.Day);
    }
    public bool ActivateYear(int yearId)
    {
      if (Years.ContainsKey(yearId))
      {
        ActiveYearId = yearId;
        LoadMonths();
        return true;
      }
      return false;
    }
    public bool ActivateMonth(int monthId)
    {

      if (IsYearActive())
      {
        if (GetActiveYear().ContainMonthId(monthId))
        {
          ActiveMonthId = monthId;
          LoadDays();
          return true;
        }
      }
      return false;
    }
    public bool ActivateDay(int dayId)
    {

      if (IsYearActive())
      {
        if (IsMonthActive())
        {
          if (GetActiveMonth().ContainDayId(dayId))
          {
            ActiveDayId = dayId;
            return true;
          }
        }
      }
      return false;
    }

    public void DeActivateDay()
    {
      ActiveDayId = -1;
    }

    public YearModel GetActiveYear()
    {
      if (Years.ContainsKey(ActiveYearId))
      {
        return Years[ActiveYearId];
      }
      return null;
    }
    public MonthModel GetActiveMonth()
    {
      var year = GetActiveYear();
      if (year != null) { return year.GetMonth(ActiveMonthId); }
      return null;
    }
    public DayModel GetActiveDay()
    {
      var month = GetActiveMonth();
      if (month != null) { return month.GetDay(ActiveDayId); }
      return null;
    }

    public void AddYear(YearModel year, bool activate)
    {
      Years.Add(year.Id, year);
      if (IsYearActive() == false)

        if (activate)
        {
          ActivateYear(year.Id);
        }
    }
    public void AddMonth(MonthModel month, bool activate)
    {
      if (IsYearActive() == true)
        Years[ActiveYearId].AddMonth(month);
      if (activate)
      {
        ActivateMonth(month.Id);
      }
    }
    public void AddDay(DayModel day, bool activate)
    {
      bool success = Years[ActiveYearId].GetMonth(ActiveMonthId).AddDay(day);
      if (activate && success)
      {
        ActivateDay(day.Id);
      }
    }
    
    public void ClockIn(DateTime startDateTime)
    {
      // Year
      if (IsYearActive() == false)
      {
        if (Years.ContainsKey(startDateTime.Year) == false)
        {
          YearModel year = new YearModel();
          year.Id = startDateTime.Year;
          AddYear(year, true);
        }
      }

      // Month
      if (IsMonthActive() == false)
      {
        if (ActivateMonth(startDateTime.Month) == false)
        {
          MonthModel month = new MonthModel();
          month.Id = startDateTime.Month;
          AddMonth(month, true);
        }
      }

      // Day
      if (IsDayActive() == false)
      {
        if (Years[ActiveYearId].GetMonth(ActiveMonthId).ContainDayId(startDateTime.Day) == false)
        {
          DayModel day = new DayModel();
          var startTime = GetRoundedTime(startDateTime);
          day.StartTime = startTime;
          day.ExpectedWorkDay = GetExpectedWorkDay(startTime.DayOfWeek);
          day.Breaks.AddRange(GetPlannedBreaks(DateOnly.FromDateTime(startTime)));
          day.Id = startDateTime.Day;
          AddDay(day, true);
        }
      }
      UpdateDeficit();
    }
    public void ClockOut(DateTime endDateTime)
    {
      if (IsDayActive())
      {
        DayModel day = GetActiveDay();
        day.EndTime = GetRoundedTime(endDateTime);
        UpdateDeficit();
      }
    }
    
    public void SetDayStart(DateTime startDatetime)
    {
      if (IsDayActive())
      {
        DayModel day = GetActiveDay();
        day.StartTime = startDatetime;
        UpdateDeficit();
      }
    }
    public void SetDayEnd(DateTime endDateTime)
    {
      if (IsDayActive())
      {
        DayModel day = GetActiveDay();
        day.EndTime = endDateTime;
        UpdateDeficit();
      }
    }
    
    public void SetDayExpectedWorkDay(TimeSpan expectedWorkDay)
    {
      if (IsDayActive())
      {
        DayModel day = GetActiveDay();
        day.ExpectedWorkDay = expectedWorkDay;
        UpdateDeficit();
      }
    }
    
    public void ToggleBreak(string name = "break")
    {
      if (IsDayActive())
      {
        DayModel day = GetActiveDay();
        if (IsOnBreak)
        {
          TimedSegment b = day.Breaks.Last();
          b.EndTime = GetRoundedTime(DateTime.Now);
          UpdateDeficit();
          IsOnBreak = false;
        }
        else
        {
          TimedSegment b = new TimedSegment();
          b.Name = name;
          b.StartTime = GetRoundedTime(DateTime.Now);
          day.AddBreak(b);
          IsOnBreak = true;
        }
      }
    }

    private DateTime GetRoundedTime(DateTime dateTime)
    {
      if (Settings.Rounding == Rounding.None)
      {
        return dateTime;
      }
      // Round Seconds
      dateTime = dateTime.RoundToNearest(TimeSpan.FromSeconds(30));
      return dateTime.RoundToNearest(TimeSpan.FromMinutes((double)Settings.Rounding));
    }
    
    public void UpdateDeficit()
    {
      foreach (YearModel year in Years.Values)
      {
        year.UpdateStatus();
      }
    }

    public void SetExpectedWorkDay(DayOfWeek dayOfWeek, TimeSpan timeSpan)
    {
      if (Settings.ExpectedWorkWeek.ContainsKey(dayOfWeek))
      {
        Settings.ExpectedWorkWeek[dayOfWeek] = timeSpan;
        return;
      }
      Settings.ExpectedWorkWeek.Add(dayOfWeek, timeSpan);
    }
    
    public void SetRounding(Rounding rounding)
    {
      Settings.Rounding = rounding;
    }
    
    public void LoadYears()
    {
      var files = Filesystem.GetFilesInFolder($"{PathsData}");
      foreach (var yearFile in files)
      {
        YearModel year = Filesystem.Deserialize<YearModel>(yearFile);
        Years.Add(year.Id, year);
      }
    }
    public void LoadMonths()
    {
      if (IsYearActive())
      {
        var files = Filesystem.GetFilesInFolder($"{PathsData}/{ActiveYearId}/");
        foreach (var monthFile in files)
        {
          MonthModel month = Filesystem.Deserialize<MonthModel>(monthFile);
          Years[ActiveYearId].AddMonth(month);
        }
      }
    }
    public void LoadDays()
    {
      if (IsMonthActive())
      {
        var files = Filesystem.GetFilesInFolder($"{PathsData}/{ActiveYearId}/{ActiveMonthId:00}/");
        foreach (var dayfile in files)
        {
          DayModel day = Filesystem.Deserialize<DayModel>(dayfile);
          // Backward compatability for adding Index
          if (day.Id == -1)
          {
            day.Id = int.Parse(Path.GetFileNameWithoutExtension(dayfile));
          }
          // Backward compatability for exprected workday not being configuratble.
          if (day.ExpectedWorkDay == new TimeSpan() && day.StartTime.HasValue)
          {
            day.ExpectedWorkDay = GetExpectedWorkDay(day.StartTime.Value.DayOfWeek);
          }
          Years[ActiveYearId].GetMonth(ActiveMonthId).AddDay(day);
        }
      }
    }
    public void Save()
    {
      foreach (YearModel year in Years.Values)
      {
        Filesystem.Serialize<YearModel>($"{PathsData}/{year.Id}.json", year);
        foreach (MonthModel month in year.GetMonths())
        {
          Filesystem.Serialize<MonthModel>($"{PathsData}/{year.Id}/{month.Id:00}.json", month);
          foreach (DayModel day in month.GetDays())
          {
            Filesystem.Serialize<DayModel>($"{PathsData}/{year.Id}/{month.Id:00}/{day.Id:00}.json", day);
          }
        }
      }
    }
  }
}
