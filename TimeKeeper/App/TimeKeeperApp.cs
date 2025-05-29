using System.Diagnostics;
using System.Runtime.InteropServices;
using TimeKeeper.App.Managers.Terminal;
using TimeKeeper.App.Managers.Calendar.Models;
using TimeKeeper.App.Managers.Terminal.Models;
using TimeKeeper.App.Managers.Calendar.Enums;
using TimeKeeper.App.Common.Filesystem;
using TimeKeeper.App.Managers.Calendar;

namespace TimeKeeper.App
{
  /// <summary>
  /// The time keeper is a little app for simplyfying office hours.
  /// This app should not handle stuff like vacation or weeks. 
  /// This is only for clocking in and clocking out, and keeping up with flex over time. 
  /// Specefic task time is handled by other applications. 
  /// </summary>
  /// 

  // TODO: Breaks - Add settings command for adding planned breaks. 
  // TODO: Breaks - Add Edit break start, end and name.
  // TODO: Space saving - Make Days load as well, with all the break objects and project objects this could save space as well.

  class TimeKeeperApp
  {
    FileSystemManager Filesystem;
    TerminalManeger Terminal;
    CalendarManager Calendar;
    AppSettings Settings = new AppSettings();
    bool isRunning = true;
    int ActiveProjectId = 0;
    CalendarSettings Project = null;

    string version = "1.0.3";

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
      Filesystem = new FileSystemManager(DataLocation);

      // Load settings
      LoadSettings();

      Terminal = new TerminalManeger();

      // Initialize commands
      LoadCommands();

      // Calendar.
      if (Settings.Projects.Count > 0)
      {
        Project = Settings.Projects[ActiveProjectId];
        Calendar = new CalendarManager(Filesystem, Project);
      }
    }

    // Start
    // ------------------------------------------------------------
    public void Main()
    {
      AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

      // Write welcome screen
      Terminal.WriteLine($"Welcome {Settings.KeeperName}");
      Terminal.Seperator();
      Terminal.WriteLine($"Current date       : {DateTime.Now.ToString("MMMM dd, yyyy")}");      
      Terminal.WriteLine($"TimeKeeper version : {version}");
      Terminal.Seperator();
      Terminal.WriteLine($"Loaded {Calendar.GetDays().Count} days");
      Terminal.WriteLine($"{Calendar.GetIncomplteDays().Count} is incomplete");
      Terminal.Seperator();
      
      Thread.Sleep(2000);

      // Main Loop
      while (isRunning)
      {
        Terminal.Clear();
        MainScreen();
        InputHandler();
      }
    }

    // Utils. 
    // ------------------------------------------------------------
    void InputHandler()
    {
      Terminal.WriteLine("Ready for input");
      Terminal.Write("> ");
      string input = Terminal.GetInput();
      string[] commands = Terminal.ParseCommand(input);
      if (commands.Length > 0)
      {
        Terminal.ExecuteCommand(commands);
      }
    }
    void LoadSettings()
    {
      string settingsFileName = $"settings.json";
      if (Filesystem.FileExists(settingsFileName))
      {
        Settings = Filesystem.Deserialize<AppSettings>(settingsFileName, true);
        return;
      }
      Settings = new AppSettings(true);
    }
    void LoadProject(int id)
    {
      ActiveProjectId = id;
      Project = Settings.Projects[ActiveProjectId];
      Calendar = new CalendarManager(Filesystem, Project);
    }
    bool IsIndexValidProject(int id)
    {
      return Settings.Projects.Count > id && id > 0;
    }
    void SaveSettings()
    {
      Calendar.Save();
      Settings.Projects[ActiveProjectId] = Project;
      Filesystem.Serialize<AppSettings>("settings.json", Settings);
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
      command.SetCommandDefaultAction(HandleUpdateCalender);
      command.SetCommandDescription("Force Updates the total work performed and the deficit.");

      Terminal.AddCommand(command);

      // Project
      command = new CommandModel("project");
      //command.SetCommandDescription("Starts and ends breaks");
      //command.SetCommandDefaultAction(HandleBreakToggle);
      command.AddFlag("get", HandleProjectGet);
      command.AddFlag("create", HandleProjectCreate);
      command.AddFlag("name", HandleProjectSetName);
      command.AddFlag("list", HandleProjectList);
      command.GenerateTagsForFlags();

      Terminal.AddCommand(command);

      // Checkin
      command = new CommandModel("checkin");
      command.SetCommandDefaultAction(HandleClockIn);
      command.SetCommandDescription("Clocks in for work, start a new day.");


      Terminal.AddCommand(command);

      // Checkout
      command = new CommandModel("checkout");
      command.SetCommandDefaultAction(HandleClockOut);
      command.SetCommandDescription("Clocks out of work");

      Terminal.AddCommand(command);

      // Break
      command = new CommandModel("break");
      command.SetCommandDescription("Starts and ends breaks");
      command.SetCommandDefaultAction(HandleBreakToggle);
      command.AddFlag("name", HandleBreakStartWithName);
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

      // Day
      command = new CommandModel("month");
      command.AddFlag("get", HandleMonthGet);
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

    // Command Handler
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
    void HandleClockIn()
    {
      Calendar.ClockIn(DateTime.Now);
      Calendar.Save();
    }
    void HandleClockOut()
    {
      Calendar.ClockOut(DateTime.Now);
      Calendar.Save();
    }
    void HandleUpdateCalender()
    {
      Calendar.UpdateDeficit();
    }
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
    void HandleMonthGet(string[] args)
    {
      if (args.Length == 0 || !int.TryParse(args[0], out int MonthID))
      {
        Terminal.WriteLine("Usage: day");
        return;
      }
      Calendar.ActivateMonth(MonthID);
      if (Calendar.IsMonthActive() == false)
      {
        Terminal.WriteLine("No day loaded.");
      }
    }
    void HandleDayGet(string[] args)
    {
      if (args.Length == 0 || !int.TryParse(args[0], out int dayID))
      {
        Terminal.WriteLine("Usage: project");
        return;
      }
      Calendar.ActivateDay(dayID);
      if (Calendar.IsDayActive() == false)
      {
        Terminal.WriteLine("No day loaded.");
      }

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
    void HandleBreakToggle()
    {
      Calendar.ToggleBreak();
      Calendar.Save();
    }
    void HandleBreakStartWithName(string[] args)
    {
      Calendar.ToggleBreak(args[0]);
      Calendar.Save();
    }
    void HandleBreakSetStart(string[] args) { }
    void HandleBreakSetEnd(string[] args) { }
    void HandleProjectGet(string[] args)
    {
      if (args.Length == 0 || !int.TryParse(args[0], out int dayID))
      {
        Terminal.WriteLine("Usage: day");
        return;
      }
      if (IsIndexValidProject(dayID))
      {
        SaveSettings();
        LoadProject(dayID);
        return;
      }
      Terminal.WriteLine($"Index :{dayID} invalid");
      Terminal.WaitForKeypress();
    }
    void HandleProjectCreate(string[] args)
    {
      if (args.Length == 0)
      {
        Terminal.WriteLine("Usage: day");
        return;
      }
      if (Filesystem.DirectoryExists(args[0]))
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

    }
    // Screens. 
    // ------------------------------------------------------------
    void MainScreen()
    {

      Terminal.WriteLine($"Project : {Project.Name}");
      Terminal.Seperator();

      var incompleteDays = Calendar.GetIncomplteDays();
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
          Terminal.Seperator();
          Terminal.WriteLine($"Incomplete days");
          foreach (DayModel day in incompleteDays)
          {
            Terminal.WriteLine($"[{day.Id:00}] {(day.StartTime.HasValue ? day.StartTime.Value.ToString("dd MMM yyyy") : "")}");
          }
          Terminal.Seperator();
        }
      }
      TimeSpan deficit = TimeSpan.Zero;
      TimeSpan totalwork = TimeSpan.Zero;
      foreach (var year in Calendar.GetYears())
      {
        deficit += year.Deficit;
        totalwork += year.WorkedHours;
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
        Terminal.Seperator();
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
            Terminal.Seperator();
            Terminal.WriteLine($"Date           :  {(day.StartTime.HasValue ? day.StartTime.Value.ToString("dd MMM yyyy") : "")}");
            Terminal.WriteLine($"Started        :  {(day.StartTime.HasValue ? day.StartTime.Value.ToString("hh:mm:ss") : "")}");
            Terminal.WriteLine($"Ended          :  {(day.EndTime.HasValue ? day.EndTime.Value.ToString("hh:mm:ss") : "")}");
            Terminal.WriteLine($"Staus          :  {Calendar.Status}");
            Terminal.Seperator();
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
              
              Terminal.Seperator();
            }
            Terminal.WriteLine($"Total Session  : {FormatedTimeSpan(day.Duration)}" );
            Terminal.WriteLine($"Total Breaks   : {FormatedTimeSpan(-day.TotalBreaks)}");
            Terminal.Seperator();
            Terminal.WriteLine($"Total Work     : {FormatedActualWorkDay(day)}");        
            Terminal.WriteLine($"Expected work  : {FormatedTimeSpan(-day.ExpectedWorkDay)}");            
            Terminal.Seperator();
            Terminal.WriteLine($"Deficit        : {FormatedTimeSpan(day.Deficit)}");
          }
        }
        Terminal.Seperator();
      }
    }
    void DebugScreen()
    {
      Terminal.Seperator();
      Process p = Process.GetCurrentProcess();
      long ram = p.PrivateMemorySize64;
      Terminal.WriteLine($"RAM: {ram / 1024 / 1024} MB");
      p.Dispose();
      Terminal.Seperator();
      Terminal.WriteLine($"Keeper name: {Settings.KeeperName}");
      Terminal.WriteLine($"Rounding   : {Project.Rounding}");
      Terminal.WaitForKeypress();
      Terminal.Seperator();
      var years = Calendar.GetYears();
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
      Terminal.Seperator();
      Terminal.WriteLine($"Total Deficit : {FormatedTimeSpan(yearsDeficit)}");
      Terminal.Seperator();

      Terminal.WriteLine($"Loaded Years  : {yearsCount}");
      Terminal.WriteLine($"Loaded Months : {monthsCount}");
      Terminal.WriteLine($"Loaded Days   : {daysCount}");
      Terminal.Seperator();
    }
    void StatusForActiveMonth(int limit = -1)
    {
      var days = Calendar.GetDays();
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

    // Formating.
    // ------------------------------------------------------------
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
  }
}