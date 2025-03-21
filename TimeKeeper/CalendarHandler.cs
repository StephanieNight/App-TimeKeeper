using TimeKeeper.Models;

namespace TimeKeeper
{
  class CalendarHandler
  {
    string PathsData = "Data";

    int ActiveDayId = -1;
    int ActiveMonthId = -1;
    int ActiveYearId = -1;

    FileHandler filesystem;
    Rounding Rounding;

    Dictionary<int, YearModel> Years = new Dictionary<int, YearModel>();

    Dictionary<DayOfWeek, TimeSpan> ExpectedWorkWeek { get; set; } = new Dictionary<DayOfWeek, TimeSpan>();

    public CalendarHandler(FileHandler filehandler, Dictionary<DayOfWeek, TimeSpan> expectedWorkWeek)
    {
      filesystem = filehandler;
      filesystem.InitializeFolder($"{filesystem.BasePath}/{PathsData}");

      // Load saved expected work week into dictionary.
      foreach (var work in expectedWorkWeek)
      {
        ExpectedWorkWeek.Add(work.Key, work.Value);
      }
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

    public TimeSpan GetExpectedWorkDay(DayOfWeek dayOfWeek){
      if(ExpectedWorkWeek.ContainsKey(dayOfWeek)){
        return ExpectedWorkWeek[dayOfWeek];
      }
      return GetDefaultExpectedWorkDay(dayOfWeek);
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
    public void SetDayLunch(TimeSpan lunchTime)
    {
      if (IsDayActive())
      {
        DayModel day = GetActiveDay();
        day.Lunch = lunchTime;
        UpdateDeficit();
      }
    }
    public void SetDayLunchCompleted(TimeSpan lunchTime)
    {
      if (IsDayActive())
      {
        DayModel day = GetActiveDay();
        var to = TimeOnly.FromTimeSpan(lunchTime);
        day.LunchTimeCompleted = to;
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
    public void ToggleBreak()
    {
      if (IsDayActive())
      {
        DayModel day = GetActiveDay();
        if (day.IsOnBreak)
        {
          day.EndBreak(GetRoundedTime(DateTime.Now));
          UpdateDeficit();
        }
        else
        {
          day.StartBreak(GetRoundedTime(DateTime.Now));
        }
      }
    }
    private DateTime GetRoundedTime(DateTime dateTime)
    {
      if (Rounding == Rounding.None)
      {
        return dateTime;
      }
      // Round Seconds
      dateTime = dateTime.RoundToNearest(TimeSpan.FromSeconds(30));
      return dateTime.RoundToNearest(TimeSpan.FromMinutes((double)Rounding));
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
      if (ExpectedWorkWeek.ContainsKey(dayOfWeek))
      {
        ExpectedWorkWeek[dayOfWeek] = timeSpan;
        return;
      }
      ExpectedWorkWeek.Add(dayOfWeek, timeSpan);
    }
    public void SetRounding(Rounding rounding)
    {
      Rounding = rounding;
    }

    public void LoadYears()
    {
      var files = filesystem.GetFilesInFolder($"{PathsData}");
      foreach (var yearFile in files)
      {
        YearModel year = filesystem.Deserialize<YearModel>(yearFile);
        Years.Add(year.Id, year);
      }
    }
    public void LoadMonths()
    {
      if (IsYearActive())
      {
        var files = filesystem.GetFilesInFolder($"{PathsData}/{ActiveYearId}/");
        foreach (var monthFile in files)
        {
          MonthModel month = filesystem.Deserialize<MonthModel>(monthFile);
          Years[ActiveYearId].AddMonth(month);
        }
      }
    }
    public void LoadDays()
    {
      if (IsMonthActive())
      {
        var files = filesystem.GetFilesInFolder($"{PathsData}/{ActiveYearId}/{ActiveMonthId:00}/");
        foreach (var dayfile in files)
        {
          DayModel day = filesystem.Deserialize<DayModel>(dayfile);
          // Backward compatability for adding Index
          if (day.Id == -1)
          {
            day.Id = Int32.Parse(Path.GetFileNameWithoutExtension(dayfile));
          }
          // Backward compatability for exprected workday not being configuratble.
          if (day.ExpectedWorkDay == new TimeSpan() && day.StartTime.HasValue)
          {
            day.ExpectedWorkDay = GetExpectedWorkDay(day.StartTime.Value.DayOfWeek);
          }         
          // Move lunch to breaks
          if(day.Lunch.TotalSeconds > 0)
          {
            DateTime lunchEnd = new DateTime(DateOnly.FromDateTime(day.StartTime.Value), day.LunchTimeCompleted);
            DateTime lunchStart = lunchEnd - day.Lunch;

            var dayBreak = new BreakModel();
            dayBreak.Name = "Lunch";
            dayBreak.StartTime = lunchStart;
            dayBreak.EndTime = lunchEnd;

            day.Breaks.Add(dayBreak);

            day.Lunch = new TimeSpan();
            day.LunchTimeCompleted = new TimeOnly();
          }

          Years[ActiveYearId].GetMonth(ActiveMonthId).AddDay(day);
        }
      }
    }
    public void Save()
    {
      foreach (YearModel year in Years.Values)
      {
        filesystem.Serialize<YearModel>($"{PathsData}/{year.Id}.json", year);
        foreach (MonthModel month in year.GetMonths())
        {
          filesystem.Serialize<MonthModel>($"{PathsData}/{year.Id}/{month.Id:00}.json", month);
          foreach (DayModel day in month.GetDays())
          {
            filesystem.Serialize<DayModel>($"{PathsData}/{year.Id}/{month.Id:00}/{day.Id:00}.json", day);
          }
        }
      }
    }
  }
}
