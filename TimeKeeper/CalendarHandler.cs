using System;
using System.Globalization;
using System.Text.Json;
using TimeKeeper.Models;
using static System.Net.Mime.MediaTypeNames;

namespace TimeKeeper
{
  class CalendarHandler
  {
    string PathsData = "Data";

    int ActiveDayId = -1;
    int ActiveMonthId = -1;
    int ActiveYearId = -1;

    FileHandler filesystem;

    Dictionary<int, MonthModel> Months = new Dictionary<int, MonthModel>();

    public CalendarHandler(FileHandler filehandler)
    {
      filesystem = filehandler;
    }

    public List<DayModel> GetDays()
    {
      if (IsMonthActive())
      {
        return Months[ActiveMonthId].GetDays();
      }
      return new List<DayModel>();
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

    public bool IsMonthActive()
    {
      return Months.ContainsKey(ActiveMonthId);
    }
    public bool IsDayActive()
    {
      if (IsMonthActive())
      {
        return Months[ActiveMonthId].ContainDayId(ActiveDayId);
      }
      return false;
    }

    public void ActivateToday()
    {
      DateTime today = DateTime.Today;
      ActivateMonth(today.Month);
      ActivateDay(today.Day);
    }
    public void ActivateYear()
    {

    }
    public void ActivateMonth(int id)
    {
      ActiveMonthId = id;
      LoadMonth();
    }
    public void ActivateDay(int id)
    {
      if (IsMonthActive())
        if (Months[ActiveMonthId].ContainDayId(id))
        {
          ActiveDayId = id;
        }
    }
    public void DeActivateDay()
    {
      ActiveDayId = -1;
    }

    public MonthModel GetActiveMonth()
    {
      if (IsMonthActive())
      {
        return Months[ActiveMonthId];
      }
      return null;
    }
    public DayModel GetActiveDay()
    {
      var month = GetActiveMonth();
      if(month != null) { return month.GetDay(ActiveDayId); }          
      return null;
    }

    public void AddMonth(MonthModel month, bool activate)
    {
      Months.Add(month.Id, month);
      if (activate)
      {
        ActivateMonth(month.Id);
      }
    }
    public void AddDay(DayModel day, bool activate)
    {
      if (IsMonthActive() == false)
      {
        int id = day.StartTime.HasValue ? day.StartTime.Value.Month : -1;
        if (Months.ContainsKey(id))
        {
          ActiveMonthId = id;
        }
        else
        {
          var month = new MonthModel();
          month.Id = id;
          AddMonth(month, true);
        }
      }

      bool success = Months[ActiveMonthId].AddDay(day);
      if (activate && success)
      {
        ActivateDay(day.Id);
      }

    }
    public void StartDay()
    {
      DayModel day = new DayModel();
      day.StartTime = DateTime.Now;
      AddDay(day, true);
    }
    public void EndDay()
    {
      SetDayEnd(DateTime.Now);
    }
    public void SetDayStart(DateTime startDatetime)
    {
      if (IsDayActive())
      {
        DayModel day = GetActiveDay();
        day.StartTime = startDatetime;
        Months[ActiveMonthId].UpdateDeficit();
      }
    }
    public void SetDayEnd(DateTime endDatetime)
    {
      if (IsDayActive())
      {
        DayModel day = GetActiveDay();
        day.EndTime = endDatetime;
        Months[ActiveMonthId].UpdateDeficit();
      }
    }
    public void SetDayLunch(TimeSpan lunchTime)
    {
      if (IsDayActive())
      {
        DayModel day = GetActiveDay();
        day.Lunch = lunchTime;
        Months[ActiveMonthId].UpdateDeficit();
      }
    }
    public void Load()
    {
      var files = filesystem.GetFilesInFolder($"{PathsData}/2025");
      foreach (var monthfile in files)
      {
        MonthModel month = filesystem.Deserialize<MonthModel>(monthfile);
        Months.Add(month.Id, month);
      }
    }
    public void LoadMonth()
    {
      if (IsMonthActive())
      {
        var files = filesystem.GetFilesInFolder($"{PathsData}/2025/{ActiveMonthId:00}/");
        foreach (var dayfile in files)
        {
          DayModel day = filesystem.Deserialize<DayModel>(dayfile);
          Months[ActiveMonthId].AddDay(day);
        }       
      }
    }
    public void Save()
    {
      foreach (MonthModel month in Months.Values)
      {
        filesystem.Serialize<MonthModel>($"{PathsData}/2025/{month.Id:00}.json", month);
        var days = month.GetDays();
        if (days.Count > 0)
        {
          foreach (DayModel day in days)
          {
            filesystem.Serialize<DayModel>($"{PathsData}/2025/{month.Id:00}/{day.Id:00}.json", day);
          }
        }
      }
    }
  }
}
