using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TerminalUX;
using TerminalUX.Models;
using TimeKeeper.App.Common.Extensions;
using TimeKeeper.App.Common.Filesystem;
using TimeKeeper.App.Managers.Calendar;
using TimeKeeper.App.Managers.Calendar.Enums;
using TimeKeeper.App.Managers.Calendar.Models;

namespace TimeKeeper.App
{
  /// <summary>
  /// The time keeper is a little app for simplifying office hours.
  /// This app should not handle stuff like vacation or weeks. 
  /// This is only for clocking in and clocking out, and keeping up with flex over time. 
  /// Specifics task time is handled by other applications. 
  /// </summary>
  /// 

  // TODO: Space saving - Just have one loaded object and a list of all id's of the next model.
  // TODO: Terminal: fix menu placement.
  // TODO: Set default project.

  class TimeKeeperApp
  {
    public static FileSystemManager FileSystem { get; private set; }
    public static Terminal Terminal { get; private set; }
    public static AppSettings Settings { get; private set; } = new AppSettings();

    bool isRunning = true;
    int ActiveProjectId = 0;

    string version = "1.1.3";

    public CalendarManager Calendar { get; private set; }
    public CalendarSettings Project { get; private set; }

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

    public TimeKeeperApp()
    {
      // Initialize Manager
      FileSystem = new FileSystemManager(DataLocation);

      // Load settings
      LoadSettings();

      Terminal = new Terminal();

      // Initialize commands
      LoadCommands();

      // Calendar.
      if (Settings.Projects.Count > 0)
      {
        ActiveProjectId = Settings.ProjectDefault;
        Project = Settings.Projects[ActiveProjectId];
        Calendar = new CalendarManager(Project);
      }
    }

    public void Main()
    {
      AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

      // Write welcome screen
      Terminal.WriteLine($"Welcome {Settings.KeeperName}");
      Terminal.SeparatorLine();
      Terminal.WriteLine($"Current date       : {DateTime.Now.ToString("MMMM dd, yyyy")}");
      Terminal.WriteLine($"TimeKeeper version : {version}");
      Terminal.SeparatorLine();
      Terminal.WriteLine($"Loaded {Calendar.GetLoadedDays().Count} days");
      Terminal.WriteLine($"{Calendar.GetLoadedIncompleteDays().Count} is incomplete");
      Terminal.SeparatorLine();

      Thread.Sleep(2000);

      // Main Loop
      while (isRunning)
      {
        Terminal.ClearScreen();
        MainScreen();
        InputHandler();
      }
    }

    #region Utils 

    // Utils. 
    // ------------------------------------------------------------
    void InputHandler()
    {
      Terminal.WriteLine("Ready for input");
      Terminal.Write("> ");
      string input = Terminal.Input();
      string[] commands = Terminal.ParseCommand(input);
      if (commands.Length > 0)
      {
        Terminal.ExecuteCommand(commands);
      }
    }
    void LoadSettings()
    {
      string settingsFileName = $"settings.json";
      if (FileSystem.FileExists(settingsFileName))
      {
        Settings = FileSystem.Deserialize<AppSettings>(settingsFileName, true);
        return;
      }
      Settings = new AppSettings(true);
    }
    void LoadProject(int id)
    {
      ActiveProjectId = id;
      Project = Settings.Projects[ActiveProjectId];
      Calendar = new CalendarManager(Project);
    }
    bool IsIndexValidProject(int id)
    {
      return Settings.Projects.Count > id || id > 0;
    }
    void SaveSettings()
    {
      Calendar.Save();
      Settings.Projects[ActiveProjectId] = Project;
      FileSystem.Serialize<AppSettings>("settings.json", Settings);
    }
    void LoadCommands()
    {
      // Debug
      CommandModel command = new CommandModel("debug");
      command.SetCommandDefaultAction(HandleDebug);
      command.SetCommandDescription("Prints debug screen");
      Terminal.AddCommand(command);

      // Exit
      command = new CommandModel("exit");
      command.SetCommandDefaultAction(HandleExit);
      command.SetCommandDescription("Saves and Exits the application");
      Terminal.AddCommand(command);

      // Update
      command = new CommandModel("update");
      command.SetCommandDefaultAction(HandleUpdateCalendar);
      command.SetCommandDescription("Force Updates the total work performed and the deficit.");

      Terminal.AddCommand(command);

      // Project
      command = new CommandModel("project");
      command.AddFlag("get", HandleProjectGet);
      command.AddFlag("create", HandleProjectCreate);
      //command.AddFlag("rename", HandleProjectSetName);
      command.AddFlag("list", HandleProjectList);
      command.AddFlag("default",HandleProjectSetDefault);
      command.GenerateTagsForFlags();

      Terminal.AddCommand(command);

      // Checkin
      command = new CommandModel("checkin");
      command.SetCommandDefaultAction(HandleDayClockIn);
      command.SetCommandDescription("Clocks in for work, start a new day.");


      Terminal.AddCommand(command);

      // Checkout
      command = new CommandModel("checkout");
      command.SetCommandDefaultAction(HandleDayClockOut);
      command.SetCommandDescription("Clocks out of work");

      Terminal.AddCommand(command);

      // Break
      command = new CommandModel("break");
      command.SetCommandDescription("Starts and ends breaks");
      command.SetCommandDefaultAction(HandleBreakToggle);
      command.AddFlag("name", HandleBreakStartWithName);
      command.AddFlag("delete", HandleBreakDelete);
      command.AddFlag("add", HandleBreakAdd);
      command.AddFlag("start", HandleBreakSetStart);
      command.AddFlag("end", HandleBreakSetEnd);
      command.AddFlag("plan", HandleBreakPlan);
      command.GenerateTagsForFlags();

      Terminal.AddCommand(command);

      // Settings
      command = new CommandModel("settings");
      command.AddFlag("showdeficit", HandleSettingsSetShowDeficit);
      command.AddFlag("showtotalwork", HandleSettingsSetShowTotalWork);
      command.AddFlag("keeper", HandleSettingsSetKeeper);
      command.AddFlag("rounding", HandleSettingsSetRounding);
      command.AddFlag("expectedworkWeek", HandleSettingsSetExpectedWorkWeek);
      command.GenerateTagsForFlags();
      Terminal.AddCommand(command);

      // Year
      command = new CommandModel("year");
      //command.AddFlag("get", HandleMonthGet);
      command.AddFlag("avarageworkweek", HandleYearShowAverageWorkWeek);
      command.GenerateTagsForFlags();
      Terminal.AddCommand(command);

      // Month
      command = new CommandModel("month");
      command.AddFlag("get", HandleMonthGet);
      command.AddFlag("averagework", HandleMonthShowAverageWork);
      command.GenerateTagsForFlags();
      Terminal.AddCommand(command);

      // Day
      command = new CommandModel("day");
      command.AddFlag("get", HandleDayGet);
      command.AddFlag("start", HandleDaySetStart);
      command.AddFlag("end", HandleDaySetEnd);
      command.AddFlag("expectedworkday", HandleDaySetExpectedWorkDay);
      command.GenerateTagsForFlags();
      Terminal.AddCommand(command);

      // Days
      command = new CommandModel("days");
      command.AddFlag("limit", HandleDaysStatusWithLimit);
      command.GenerateTagsForFlags();
      command.SetCommandDefaultAction(HandleDaysStatus);
      Terminal.AddCommand(command);
    }
    // Events
    // ------------------------------------------------------------
    void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
      Terminal.WriteLine("Saving...");
      SaveSettings();
      Terminal.WriteLine("Done");
      Thread.Sleep(500);
    }
    #endregion

    #region Command Handler

    // System
    // ------------------------------------------------------------
    void HandleDebug()
    {
      DebugScreen();
      Terminal.WaitForKeypress();
    }
    void HandleExit()
    {
      isRunning = false;
    }
    // Settings
    // ------------------------------------------------------------
    void HandleSettingsSetRounding(string[] args)
    {
      if (args.Length == 0 || !int.TryParse(args[0], out int rounding))
      {
        Terminal.WriteLine("Usage: Setting --rounding <0,5,10,15,30>");
        return;
      }
      switch (rounding)
      {
        case (int)Rounding.FiveMinutes:
          Settings.Projects[ActiveProjectId].Rounding = Rounding.FiveMinutes;
          break;
        case (int)Rounding.TenMinutes:
          Settings.Projects[ActiveProjectId].Rounding = Rounding.TenMinutes;
          break;
        case (int)Rounding.FifteenMinutes:
          Settings.Projects[ActiveProjectId].Rounding = Rounding.FifteenMinutes;
          break;
        case (int)Rounding.ThirtyMinutes:
          Settings.Projects[ActiveProjectId].Rounding = Rounding.ThirtyMinutes;
          break;
        case (int)Rounding.None:
        default:
          Settings.Projects[ActiveProjectId].Rounding = Rounding.None;
          break;
      }
      Calendar.SetRounding(Settings.Projects[ActiveProjectId].Rounding);
      SaveSettings();
    }
    void HandleSettingsSetShowDeficit(string[] args)
    {
      if (args.Length == 1)
      {
        if (args[0] == "0")
        {
          Settings.ShowTotalWork = false;
        }
        else if (args[0] == "1")
        {
          Settings.ShowDeficit = true;
        }
        else if (Boolean.TryParse(args[0], out bool showDeficit))
        {
          Settings.ShowDeficit = showDeficit;
        }
        SaveSettings();
        return;
      }
      Terminal.WriteLine("Usage: Setting");
    }
    void HandleSettingsSetShowTotalWork(string[] args)
    {
      if (args.Length == 1)
      {
        if (args[0] == "0")
        {
          Settings.ShowTotalWork = false;
        }
        else if (args[0] == "1")
        {
          Settings.ShowTotalWork = true;
        }
        else if (Boolean.TryParse(args[0], out bool showTotalWork))
        {
          Settings.ShowTotalWork = showTotalWork;
        }
        SaveSettings();
        return;
      }
      Terminal.WriteLine("Usage: Setting");
    }
    void HandleSettingsSetKeeper(string[] args)
    {
      if (args.Length == 0)
      {
        Terminal.WriteLine("Usage: Setting");
        return;
      }
      Settings.KeeperName = args[0];
    }
    void HandleSettingsSetExpectedWorkWeek(string[] args)
    {
      // set Mon-Fri and Weekend off
      if (args.Length == 1)
      {
        if (TimeSpan.TryParse(args[0], out TimeSpan weekdays))
        {
          Project.ExpectedWorkWeek = new Dictionary<DayOfWeek, TimeSpan>();
          Project.ExpectedWorkWeek.Add(DayOfWeek.Monday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Tuesday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Wednesday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Thursday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Friday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Saturday, new TimeSpan());
          Project.ExpectedWorkWeek.Add(DayOfWeek.Sunday, new TimeSpan());
        }
      }
      // Set Mon-Thurs, friday and weekend off
      else if (args.Length == 2)
      {
        if (TimeSpan.TryParse(args[0], out TimeSpan weekdays)
        && TimeSpan.TryParse(args[1], out TimeSpan friday))
        {
          Project.ExpectedWorkWeek = new Dictionary<DayOfWeek, TimeSpan>();
          Project.ExpectedWorkWeek.Add(DayOfWeek.Monday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Tuesday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Wednesday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Thursday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Friday, friday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Saturday, new TimeSpan());
          Project.ExpectedWorkWeek.Add(DayOfWeek.Sunday, new TimeSpan());

          return;
        }
      }
      // Set Mon-Thurs, friday and Weekend
      else if (args.Length == 3)
      {
        if (TimeSpan.TryParse(args[0], out TimeSpan weekdays)
        && TimeSpan.TryParse(args[1], out TimeSpan friday)
        && TimeSpan.TryParse(args[2], out TimeSpan weekend))
        {
          Project.ExpectedWorkWeek = new Dictionary<DayOfWeek, TimeSpan>();
          Project.ExpectedWorkWeek.Add(DayOfWeek.Monday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Tuesday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Wednesday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Thursday, weekdays);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Friday, friday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Saturday, weekend);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Sunday, weekend);

          return;
        }
      }
      // Set mon,tue,wens,thurs,fri and weekend off
      else if (args.Length == 5)
      {
        if (TimeSpan.TryParse(args[0], out TimeSpan monday)
         && TimeSpan.TryParse(args[1], out TimeSpan tuesday)
         && TimeSpan.TryParse(args[2], out TimeSpan wednesday)
         && TimeSpan.TryParse(args[3], out TimeSpan thursday)
         && TimeSpan.TryParse(args[4], out TimeSpan friday))
        {
          Project.ExpectedWorkWeek = new Dictionary<DayOfWeek, TimeSpan>();
          Project.ExpectedWorkWeek.Add(DayOfWeek.Monday, monday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Tuesday, tuesday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Wednesday, wednesday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Thursday, thursday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Friday, friday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Saturday, new TimeSpan());
          Project.ExpectedWorkWeek.Add(DayOfWeek.Sunday, new TimeSpan());

          return;
        }
      }
      // set all days in the week
      else if (args.Length == 7)
      {
        if (TimeSpan.TryParse(args[0], out TimeSpan monday)
         && TimeSpan.TryParse(args[1], out TimeSpan tuesday)
         && TimeSpan.TryParse(args[2], out TimeSpan wednesday)
         && TimeSpan.TryParse(args[3], out TimeSpan thursday)
         && TimeSpan.TryParse(args[4], out TimeSpan friday)
         && TimeSpan.TryParse(args[5], out TimeSpan saturday)
         && TimeSpan.TryParse(args[6], out TimeSpan sunday))
        {
          Project.ExpectedWorkWeek = new Dictionary<DayOfWeek, TimeSpan>();
          Project.ExpectedWorkWeek.Add(DayOfWeek.Monday, monday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Tuesday, tuesday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Wednesday, wednesday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Thursday, thursday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Friday, friday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Saturday, saturday);
          Project.ExpectedWorkWeek.Add(DayOfWeek.Sunday, sunday);

          return;
        }
      }
      Terminal.WriteLine("Usage: Setting");
    }
    // Calendar
    // ------------------------------------------------------------
    void HandleUpdateCalendar()
    {
      Calendar.UpdateDeficit();
    }
    // Project 
    // ------------------------------------------------------------
    void HandleProjectGet(string[] args)
    {
      if (args.Length == 0 || !int.TryParse(args[0], out int ProjectID))
      {
        Terminal.WriteLine("Usage: day");
        return;
      }
      if (IsIndexValidProject(ProjectID))
      {
        SaveSettings();
        LoadProject(ProjectID);
        return;
      }
      Terminal.WriteLine($"Index :{ProjectID} invalid");
      Terminal.WaitForKeypress();
    }
    void HandleProjectCreate(string[] args)
    {
      if (args.Length == 0)
      {
        Terminal.WriteLine("Usage: day");
        return;
      }
      if (FileSystem.DirectoryExists(args[0]))
      {
        Terminal.WriteLine("Project Name must be unique");
        Terminal.WaitForKeypress();
        return;
      }
      var calender = new CalendarSettings();
      calender.Name = args[0];
      Settings.Projects.Add(calender);
      SaveSettings();
      LoadProject(Settings.Projects.IndexOf(calender));
    }
    void HandleProjectSetName(string[] args)
    {

    }
    void HandleProjectList(string[] args)
    {
        for( int i = 0; i < Settings.Projects.Count; i++){
          var p = Settings.Projects[i];
          Terminal.WriteLine($"[{i:00}] {p.Name}");          
        }
        Terminal.WaitForKeypress();
    }
    void HandleProjectSetDefault(string[] args)
    {
      if (args.Length == 0 || !int.TryParse(args[0], out int ProjectID))
      {
        Terminal.WriteLine("Usage: set project default");
        return;        
      }
      if(!IsIndexValidProject(ProjectID)){
        Terminal.WriteLine($"Project id of {ProjectID} is invalid");
        Terminal.WaitForKeypress();
        return;        
      }
      Settings.ProjectDefault = ProjectID;
      SaveSettings();
    }
    // Year 
    // ------------------------------------------------------------
    void HandleYearShowAverageWorkWeek(string[] args)
    {
      var activeMonth = Calendar.GetActiveMonth().Id;
      var activeDay = Calendar.GetActiveDay().Id;

      if (Calendar.IsYearActive())
      {
        Dictionary<int, double> weekCounter = new Dictionary<int, double>();
        foreach (var month in Calendar.GetAllMonths())
        {
          Calendar.ActivateMonth(month);
          foreach (var day in Calendar.GetActiveMonth().GetDays())
          {
            if (day.IsComplete)
            {
              var weekOfYear = day.StartTime.Value.GetIsoWeekNumber();
              if (weekCounter.ContainsKey(weekOfYear))
              {
                var current = weekCounter[weekOfYear] += day.Worked.TotalHours;
           
                weekCounter[weekOfYear] = current;
              }
              else
              {
                weekCounter[weekOfYear] = day.Worked.TotalHours;
              }
            }
          }
          Calendar.DeActiveMonth();
        }
        var counter = 0;
        var orderedKeys = weekCounter.Keys.ToList();
        orderedKeys.Sort();

        foreach (var key in orderedKeys)
        {      
          var totalHours = weekCounter[key];       
          Terminal.WriteLine($"[{key:00}] : {totalHours:0.00}");
          counter++;
          if (counter == 10)
          {
            counter = 0;
            Terminal.InputContinue();
          }
        }
        Terminal.InputContinue("End");
      }

      Calendar.ActivateMonth(activeMonth);
      Calendar.ActivateDay(activeDay);
    }
    void HandleYearShowAverageDailyWorkPerWeek(string[] args)
    {
      if (Calendar.IsYearActive())
      {
        Dictionary<int, (double TotalHours, int Count)> weekCounter = new Dictionary<int, (double TotalHours, int Count)>();
        foreach(var month in Calendar.GetAllMonths())
        {
          Calendar.ActivateMonth(month);
          foreach (var day in Calendar.GetActiveMonth().GetDays())
          {
            if (day.IsComplete)
            {
              var weekOfYear = day.StartTime.Value.GetIsoWeekNumber();
              if (weekCounter.ContainsKey(weekOfYear))
              {
                var current = weekCounter[weekOfYear];
                current.TotalHours += day.Worked.TotalHours;
                current.Count++;
                weekCounter[weekOfYear] = current;
              }
              else
              {
                weekCounter[weekOfYear] = (day.Worked.TotalHours, 1);
              }
            }
          }
        }
        var counter = 0;
        foreach(var key in weekCounter.Keys)
        {
          counter++;
          var current = weekCounter[key];
          var average = current.TotalHours / current.Count;
          Terminal.WriteLine($"[{key:00}] : {average:0.00}");
          if(counter == 10)
          {
            counter = 0;
            Terminal.InputContinue();
          }
        }
        Terminal.InputContinue("End");
      }          
    }
    // Month
    // ------------------------------------------------------------
    void HandleMonthGet(string[] args)
    {
      if (args.Length == 0 || !int.TryParse(args[0], out int MonthID))
      {
        Terminal.WriteLine("Usage: Month");
        return;
      }
      Calendar.ActivateMonth(MonthID);
      if (Calendar.IsMonthActive() == false)
      {
        Terminal.WriteLine("No day loaded.");
      }
    }
    void HandleMonthShowAverageWork(string[] args)
    {
      if (args.Length == 0)
      {
        if (Calendar.IsMonthActive())
        {
          var month = Calendar.GetActiveMonth();
          var awd = month.AverageWorkDay;
          
        Terminal.WriteLine($"Month Average daily work: {awd.Hours:00}:{awd.Minutes:00}:{awd.Seconds:00}");
        Terminal.Input();
      }
      }
    }
    // Day
    // ------------------------------------------------------------
    void HandleDaysStatus()
    {
      StatusForActiveMonth();
    }
    void HandleDaysStatusWithLimit(string[] args)
    {
      if (args.Length == 0 || !int.TryParse(args[0], out int dayLimit))
      {
        Terminal.WriteLine("Usage: days");
        return;
      }
      StatusForActiveMonth(dayLimit);
    }
    void HandleDayGet(string[] args)
    {
      int dayID = -1;

      // Validate input
      if (args.Length != 0 && !int.TryParse(args[0], out dayID))
      {
        Terminal.WriteLine("Usage:");
        Terminal.WriteLine(" - Get known day provide day: days -g 5");
        Terminal.WriteLine(" - select from list of days : days -g ");
        Terminal.WaitForKeypress();
        return;
      }

      if (args.Length == 0)
      {
        var dayslist = new List<string>();
        var days = Calendar.GetActiveMonth().GetDays();
        foreach (var day in days)
        {
          dayslist.Add($"[{day.Id:00}] {day.StartTime.Value.ToString("yyyy MMM dd")} - Worked [{day.Worked.TotalHours:0.00}]");
        }
        var index = Terminal.SingleSelectMenu.StartMenu(dayslist.ToArray());
        if (index >= 0)
        {
          dayID = days[index].Id;
        }
      }
      Calendar.ActivateDay(dayID);
      if (Calendar.IsDayActive() == false)
      {
        Terminal.WriteLine($"No day loaded. invalid id [{dayID}]");
      }
    }
    void HandleDayClockIn()
    {
      Calendar.ClockIn(DateTime.Now);
      Calendar.Save();
    }
    void HandleDayClockOut()
    {
      Calendar.ClockOut(DateTime.Now);
      Calendar.Save();
    }
    void HandleDaySetStart(string[] args)
    {
      if (args.Length == 0 || !DateTime.TryParse(args[0], out DateTime startdatetime))
      {
        Terminal.WriteLine("Usage: day");
        return;
      }
      Calendar.SetDayStart(startdatetime);
      Calendar.Save();
    }
    void HandleDaySetEnd(string[] args)
    {
      if (args.Length == 0 || !DateTime.TryParse(args[0], out DateTime enddateTime))
      {
        Terminal.WriteLine("Usage: day");
        return;
      }
      Calendar.SetDayEnd(enddateTime);
      Calendar.Save();
    }
    void HandleDaySetExpectedWorkDay(string[] args)
    {
      if (args.Length == 0 || !TimeSpan.TryParse(args[0], out TimeSpan ew))
      {
        Terminal.WriteLine("Usage: day");
        return;
      }
      Calendar.SetDayExpectedWorkDay(ew);
      Calendar.Save();
    }
    // Break 
    // ------------------------------------------------------------
    void HandleBreakToggle()
    {
      Calendar.ToggleBreak();
      Calendar.Save();
    }
    void HandleBreakStartWithName(string[] args)
    {
      if (args.Length == 1)
      {
        Calendar.ToggleBreak(args[0]);
        Calendar.Save();
      }
    }
    void HandleBreakSetStart(string[] args)
    {
      if (args.Length == 0 || !DateTime.TryParse(args[0], out DateTime startDateTime))
      {
        Terminal.WriteLine("Usage: day");
        return;
      }
      Calendar.SetBreakStart(startDateTime);
      Calendar.Save();
    }
    void HandleBreakSetEnd(string[] args)
    {
      if (args.Length == 0 || !DateTime.TryParse(args[0], out DateTime endDateTime))
      {
        Terminal.WriteLine("Usage: day");
        return;
      }
      Calendar.SetBreakEnd(endDateTime);
      Calendar.Save();
    }
    void HandleBreakDelete(string[] args)
    {
      if (args.Length == 0)
      {
        Terminal.WriteLine("Select breaks to remove");
        var breaklist = new List<string>();
        var index = 0;
        foreach (var b in Calendar.GetActiveDay().Breaks)
        {
          var complete = b.IsCompleted ? "D" : "G";
          var duration = b.Duration;
          breaklist.Add($"[{index}] [{complete}] [{duration}]");
          index++;
        }
        index = Terminal.SingleSelectMenu.StartMenu(breaklist.ToArray());
        if (index >= 0)
        {
          Calendar.GetActiveDay().Breaks.RemoveAt(index);
        }
      }
      else if (args.Length == 1 && Int32.TryParse(args[0], out int index))
      {
        if (Calendar.GetActiveDay().Breaks.Count() - 1 >= index)
        {
          Calendar.GetActiveDay().Breaks.RemoveAt(index);
        }
      }
    }
    void HandleBreakAdd(string[] args)
    {
      if (args.Length == 1 && TimeSpan.TryParse(args[0], out TimeSpan timespan))
      {
        Calendar.AddBreak(timespan);
      }
    }
    void HandleBreakPlan(string[] args)
    {
      TimeOnly startTime;
      TimeOnly endTime;

      while (!TimeOnly.TryParse(Terminal.Prompt("Time of start of break:"), out startTime)) ;
      while (!TimeOnly.TryParse(Terminal.Prompt("Time of end of break:"), out endTime)) ;

      var name = Terminal.Prompt("Name of break");

      var days = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
      var result = Terminal.MultiSelectMenu.StartMenu(days, Console.CursorTop,Console.CursorLeft);
      var planed = new PlannedBreakModel()
      {
        Name = name == "" ? "break": name,
        Start = startTime,
        End = endTime
      };
      for(int i = 0; i < result.Length; i ++)
      {
        if (result[i])
        {
          planed.ActiveOnDays.Add((DayOfWeek)Enum.Parse(typeof(DayOfWeek), days[i]));
        }
      }
      Project.PlannedBreaks.Add(planed);
      SaveSettings();
    }
    #endregion

    #region Screens

    void MainScreen()
    {

      Terminal.WriteLine($"Project : {Project.Name}");
      Terminal.SeparatorLine();

      var incompleteDays = Calendar.GetLoadedIncompleteDays();
      if (incompleteDays.Count > 0)
      {
        if (incompleteDays.Count == 1 &&
            incompleteDays[0].StartTime.HasValue &&
            incompleteDays[0].StartTime.Value.Date == DateTime.Now.Date)
        {
          // Do nothing this is expected for the current date to not be complete.
        }
        else
        {
          Terminal.SeparatorLine();
          Terminal.WriteLine($"Incomplete days");
          foreach (DayModel day in incompleteDays)
          {
            Terminal.WriteLine($"[{day.Id:00}] {(day.StartTime.HasValue ? day.StartTime.Value.ToString("dd MMM yyyy") : "")}");
          }
          Terminal.SeparatorLine();
        }
      }
      TimeSpan deficit = TimeSpan.Zero;
      TimeSpan totalwork = TimeSpan.Zero;
      foreach (var year in Calendar.GetLoadedYears())
      {
        deficit += year.Deficit;
        totalwork += year.Worked;
      }
      if (Settings.ShowDeficit || Settings.ShowTotalWork)
      {
        if (Settings.ShowDeficit)
        {
          Terminal.WriteLine($"Total Deficit  : {FormatedTimeSpan(deficit)}");
        }
        if (Settings.ShowTotalWork)
        {
          Terminal.WriteLine($"Total Work     :  {totalwork.TotalHours:00.0} h");
        }
        Terminal.SeparatorLine();
      }
      if (Calendar.IsYearActive())
      {
        DateTime currentDate = new DateTime();

        YearModel year = Calendar.GetActiveYear();
        currentDate = currentDate.AddYears(year.Id - 1);
        Terminal.WriteLine($"Active Year    :  [{currentDate.ToString("yy")}] {currentDate.ToString("yyyy")}");

        if (Calendar.IsMonthActive())
        {
          MonthModel month = Calendar.GetActiveMonth();
          currentDate = currentDate.AddMonths(month.Id - 1);
          Terminal.WriteLine($"Active Month   :  [{currentDate.Month:00}] {currentDate.ToString("MMMM")}");
          if (Calendar.IsDayActive())
          {
            DayModel day = Calendar.GetActiveDay();
            currentDate = currentDate.AddDays(day.Id - 1);
            Terminal.WriteLine($"Active day     :  [{currentDate.Day:00}] {currentDate.ToString("dddd")}");
            Terminal.SeparatorLine();
            Terminal.WriteLine($"Date           :  {(day.StartTime.HasValue ? day.StartTime.Value.ToString("dd MMM yyyy") : "")}");
            Terminal.WriteLine($"Started        :  {(day.StartTime.HasValue ? day.StartTime.Value.ToString("hh:mm:ss") : "")}");
            Terminal.WriteLine($"Ended          :  {(day.EndTime.HasValue ? day.EndTime.Value.ToString("hh:mm:ss") : "")}");
            Terminal.WriteLine($"Staus          :  {Calendar.Status}");
            Terminal.SeparatorLine();
            // Breaks
            // get all completed Breaks and breaks that are in the past.
            var breaks = day.Breaks.ToArray();

            if (breaks.Length > 0)
            {
              Terminal.Write($"Breaks         :  ");
              for (int i = 0; i < breaks.Length; i++)
              {
                var dayBreak = breaks[i];
                if (i == 0)
                {
                  Terminal.WriteLine($"{FormatedBreak(dayBreak)}");
                }
                else
                {
                  Terminal.WriteLine($"               :  {FormatedBreak(dayBreak)}");
                }
              }

              Terminal.SeparatorLine();
            }
            Terminal.WriteLine($"Total Session  : {FormatedTimeSpan(day.Duration)}");
            Terminal.WriteLine($"Total Breaks   : {FormatedTimeSpan(-day.TotalBreaks)}");
            Terminal.SeparatorLine();
            Terminal.WriteLine($"Total Work     : {FormatedActualWorkDay(day)}");
            Terminal.WriteLine($"Expected work  : {FormatedTimeSpan(-day.ExpectedWorkDay)}");
            Terminal.SeparatorLine();
            Terminal.WriteLine($"Deficit        : {FormatedTimeSpan(day.Deficit)}");
          }
        }
        Terminal.SeparatorLine();
      }
    }
    void DebugScreen()
    {
      Terminal.SeparatorLine();
      Process p = Process.GetCurrentProcess();
      long ram = p.PrivateMemorySize64;
      Terminal.WriteLine($"RAM: {ram / 1024 / 1024} MB");
      p.Dispose();
      Terminal.SeparatorLine();
      Terminal.WriteLine($"Keeper name: {Settings.KeeperName}");
      Terminal.WriteLine($"Rounding   : {Project.Rounding}");
      Terminal.WaitForKeypress();
      Terminal.SeparatorLine();
      var years = Calendar.GetLoadedYears();
      var yearsDeficit = TimeSpan.Zero;
      var daysCount = 0;
      var monthsCount = 0;
      var yearsCount = years.Count;
      DateOnly date;

      foreach (YearModel year in years)
      {
        date = new DateOnly(year.Id, 1, 1);
        // date = date.AddYears(year.Id - 1);
        Terminal.WriteLine($"[{date.ToString("yy")}] {year.Id}.");

        var months = year.GetMonths();
        var monthDeficit = TimeSpan.Zero;
        monthsCount += months.Count;

        Terminal.WriteLine($" - Months loaded: {months.Count}");

        foreach (MonthModel month in months)
        {
          date = new DateOnly(year.Id, month.Id, 1);
          Terminal.WriteLine($"   [{month.Id:00}] {date.ToString("MMMM")}.");

          var days = month.GetDays();
          var dayDeficit = TimeSpan.Zero;

          daysCount += days.Count;

          Terminal.WriteLine($"    - Days loaded: {days.Count}");
          foreach (DayModel day in days)
          {
            if (day.IsComplete)
            {
              Terminal.WriteLine($"       - [{day.Id:00}] Deficit : {FormatedTimeSpan(day.Deficit)}");
              dayDeficit += day.Deficit;
            }
            else
            {
              Terminal.WriteLine($"       - [{day.Id:00}] Deficit : 00:00:00");
            }
          }
          Terminal.WriteLine($"              - Total : {FormatedTimeSpan(dayDeficit)}");
          Terminal.WriteLine($"    - Month Deficit   : {FormatedTimeSpan(month.Deficit)}");
          Terminal.WriteLine($"    - counted Deficit : {FormatedTimeSpan(dayDeficit)}");

          monthDeficit += month.Deficit;

        }

        Terminal.WriteLine($" - year Deficit    : {FormatedTimeSpan(year.Deficit)}");
        Terminal.WriteLine($" - counted Deficit : {FormatedTimeSpan(monthDeficit)}");

        yearsDeficit += year.Deficit;
      }
      Terminal.SeparatorLine();
      Terminal.WriteLine($"Total Deficit : {FormatedTimeSpan(yearsDeficit)}");
      Terminal.SeparatorLine();

      Terminal.WriteLine($"Loaded Years  : {yearsCount}");
      Terminal.WriteLine($"Loaded Months : {monthsCount}");
      Terminal.WriteLine($"Loaded Days   : {daysCount}");
      Terminal.SeparatorLine();
    }
    void StatusForActiveMonth(int limit = -1)
    {
      var days = Calendar.GetLoadedDays();
      var startindex = 0;
      var endindex = days.Count;
      if (limit > -1)
      {
        startindex = endindex - limit;
      }
      for (int i = startindex; i < endindex; i++)
      {
        DayModel day = days[i];
        Terminal.WriteLine($"[{day.Id:00}] {day.StartTime.Value.ToString("yyyy MMM dd")} - Worked [{day.Worked.TotalHours:0.00}]");
      }
      Terminal.WaitForKeypress();
    }
    #endregion

    #region Formatting
    
    string FormatedActualWorkDay(DayModel day)
    {
      var worked = day.Worked;
      string formated = "";
      if (worked > day.ExpectedWorkDay)
      {
        formated += "+";
      }
      else if (worked.TotalSeconds >= 0)
      {
        formated += " ";
      }
      else
      {
        formated += "-";
      }
      formated += $"{Math.Abs(worked.Hours):00}:{Math.Abs(worked.Minutes):00}:{Math.Abs(worked.Seconds):00} [{worked.TotalHours:0.00}]";
      return formated;
    }
    string FormatedTimeSpan(TimeSpan timeSpan)
    {
      return $"{(timeSpan.TotalMilliseconds >= 0 ? "+" : "-")}{Math.Abs(timeSpan.Hours):00}:{Math.Abs(timeSpan.Minutes):00}:{Math.Abs(timeSpan.Seconds):00}";
    }
    string FormatedBreak(TimedSegment daybreak)
    {
      string formatedString = $"{daybreak.Duration.ToString("hh':'mm':'ss")}";
      formatedString += $" {daybreak.Name}";
      formatedString += $" {(daybreak.EndTime > DateTime.Now ? "[PLANNED]" : "")}";
      return formatedString;
    }
    #endregion
  }
}