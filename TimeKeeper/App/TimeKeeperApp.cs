using System.Diagnostics;
using TimeKeeper.App.Models;
using TimeKeeper.App.Enums;
using TimeKeeper.App.Handlers;
using System.Runtime.InteropServices;

namespace TimeKeeper.App
{
  /// <summary>
  /// The time keeper is a little app for simplyfying office hours.
  /// This app should not handle stuff like vacation or weeks. 
  /// This is only for clocking in and clocking out, and keeping up with flex over time. 
  /// Specefic task time is handled by other applications. 
  /// </summary>
  class TimeKeeperApp
  {
    FileHandler filesystem;
    TerminalHandler terminal;
    CalendarHandler calendar;
    Settings settings = new Settings();
    bool isRunning = true;

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

    // Start
    public void Main()
    {
      AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

      // Initialize Manager
      filesystem = new FileHandler(DataLocation);
      terminal = new TerminalHandler();
      calendar = new CalendarHandler(filesystem);

      // Initialize
      LoadCommands();

      // Load Files and settings
      LoadSettings();
      calendar.LoadYears();
      calendar.ActivateToday();
      calendar.SetRounding(settings.Rounding);
      calendar.AddExpectedWorkWeek(settings.ExpectedWorkWeek);

      // Write welcome screen
      terminal.WriteLine($"Welcome {settings.KeeperName}");
      terminal.Seperator();
      terminal.WriteLine($"Current date : {DateTime.Now.ToString("MMMM dd, yyyy")}");
      terminal.Seperator();
      terminal.WriteLine($"Loaded {calendar.GetDays().Count} days");
      terminal.WriteLine($"{calendar.GetIncomplteDays().Count} is incomplete");
      terminal.Seperator();
      Thread.Sleep(1500);

      // Main Loop
      while (isRunning)
      {
        terminal.Clear();
        MainScreen();
        InputHandler();
      }
    }

    // Utils. 
    void InputHandler()
    {
      terminal.WriteLine("Ready for input");
      terminal.Write("> ");
      string input = terminal.GetInput();
      string[] commands = terminal.ParseCommand(input);
      terminal.ExecuteCommand(commands);


    }
    void LoadSettings()
    {
      string settingsFileName = $"settings.json";
      if (filesystem.FileExists(settingsFileName))
      {
        settings = filesystem.Deserialize<Settings>(settingsFileName, true);
      }
    }
    void SaveSettings()
    {
      filesystem.Serialize<Settings>("settings.json", settings);
    }
    void LoadCommands()
    {

      // Debug
      Command command = new Command("debug");
      command.SetDefaultAction(HandleDebug);
      command.SetDescription("Prints debug screen");
      terminal.AddCommand(command);

      // Exit
      command = new Command("exit");
      command.SetDefaultAction(HandleExit);
      command.SetDescription("Saves and Exits the application");
      terminal.AddCommand(command);

      // Update
      command = new Command("update");
      command.SetDefaultAction(HandleUpdateCalender);
      command.SetDescription("Force Updates the total work performed and the deficit.");

      terminal.AddCommand(command);


      // Checkin
      command = new Command("checkin");
      command.SetDefaultAction(HandleClockIn);
      command.SetDescription("Clocks in for work, start a new day.");


      terminal.AddCommand(command);
      // Clockin
      command = new Command("clockin");
      command.SetDefaultAction(HandleClockIn);
      command.SetDescription("Clocks in for work, start a new day.");

      terminal.AddCommand(command);


      // Checkout
      command = new Command("checkout");
      command.SetDefaultAction(HandleClockOut);
      command.SetDescription("Clocks out of work");

      terminal.AddCommand(command);

      // Clockout
      command = new Command("clockout");
      command.SetDefaultAction(HandleClockOut);
      command.SetDescription("Clocks out of work");

      terminal.AddCommand(command);

      // Break
      command = new Command("break");
      command.SetDescription("Starts and ends breaks");
      command.SetDefaultAction(HandleBreakToggle);
      command.AddFlag("--name", HandleBreakStartWithName);
      //command.AddFlag("--start", HandleBreakSetStart);
      //command.AddFlag("--end", HandleBreakSetEnd);

      terminal.AddCommand(command);

      // Settings
      command = new Command("settings");
      command.AddFlag("--showdeficit", HandleSettingsSetShowDeficit);
      command.AddFlag("--showtotalwork", HandleSettingsSetShowTotalWork);
      command.AddFlag("--keeper", HandleSettingsSetKeeper);
      command.AddFlag("--rounding", HandleSettingsSetRounding);
      command.AddFlag("--expectedworkWeek", HandleSettingsSetExpectedWorkWeek);

      terminal.AddCommand(command);

      // Day
      command = new Command("day");
      command.AddFlag("--get", HandleDayGet);
      command.AddFlag("--start", HandleDaySetStart);
      command.AddFlag("--end", HandleDaySetEnd);
      command.AddFlag("--expectedworkday", HandleDaySetExpectedWorkDay);

      terminal.AddCommand(command);

      // Days
      command = new Command("days");
      command.AddFlag("--limit", HandleDaysStatusWithLimit);
      command.SetDefaultAction(HandleDaysStatus);

      terminal.AddCommand(command);
    }

    // Events
    void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
      terminal.WriteLine("Saving...");
      calendar.Save();
      SaveSettings();
      terminal.WriteLine("Done");
      Thread.Sleep(500);
    }

    // Command Handler
    void HandleDebug()
    {
      DebugScreen();
      terminal.WaitForKeypress();
    }
    void HandleExit()
    {
      isRunning = false;
    }
    void HandleClockIn()
    {
      calendar.ClockIn(DateTime.Now);
      calendar.Save();
    }
    void HandleClockOut()
    {
      calendar.ClockOut(DateTime.Now);
      calendar.Save();
    }
    void HandleUpdateCalender()
    {
      calendar.UpdateDeficit();
    }
    void HandleSettingsSetRounding(string[] args)
    {
      if (args.Length == 0 || !int.TryParse(args[0], out int rounding))
      {
        terminal.WriteLine("Usage: Setting --rounding <0,5,10,15,30>");
        return;
      }
      switch (rounding)
      {
        case (int)Rounding.FiveMinutes:
          settings.Rounding = Rounding.FiveMinutes;
          break;
        case (int)Rounding.TenMinutes:
          settings.Rounding = Rounding.TenMinutes;
          break;
        case (int)Rounding.FifteenMinutes:
          settings.Rounding = Rounding.FifteenMinutes;
          break;
        case (int)Rounding.ThirtyMinutes:
          settings.Rounding = Rounding.ThirtyMinutes;
          break;
        default:
          settings.Rounding = Rounding.None;
          break;
      }
      calendar.SetRounding(settings.Rounding);
      SaveSettings();
    }
    void HandleSettingsSetShowDeficit(string[] args)
    {
      if (args.Length == 1)
      {
        if (args[0] == "0")
        {
          settings.ShowTotalWork = false;
        }
        else if (args[0] == "1")
        {
          settings.ShowDeficit = true;
        }
        else if (Boolean.TryParse(args[0], out bool showDeficit))
        {
          settings.ShowDeficit = showDeficit;
        }
        SaveSettings();
        return;
      }
      terminal.WriteLine("Usage: Setting");
    }
    void HandleSettingsSetShowTotalWork(string[] args)
    {
      if (args.Length == 1)
      {
        if (args[0] == "0")
        {
          settings.ShowTotalWork = false;
        }
        else if (args[0] == "1")
        {
          settings.ShowTotalWork = true;
        }
        else if (Boolean.TryParse(args[0], out bool showTotalWork))
        {
          settings.ShowTotalWork = showTotalWork;
        }
        SaveSettings();
        return;
      }
      terminal.WriteLine("Usage: Setting");
    }
    void HandleSettingsSetKeeper(string[] args)
    {
      if (args.Length == 0)
      {
        terminal.WriteLine("Usage: Setting");
        return;
      }
      settings.KeeperName = args[0];
    }
    void HandleSettingsSetExpectedWorkWeek(string[] args)
    {
      // set Mon-Fri and Weekend off
      if (args.Length == 1)
      {
        if (TimeSpan.TryParse(args[0], out TimeSpan weekdays))
        {
          settings.ExpectedWorkWeek = new Dictionary<DayOfWeek, TimeSpan>();
          settings.ExpectedWorkWeek.Add(DayOfWeek.Monday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Tuesday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Wednesday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Thursday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Friday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Saturday, new TimeSpan());
          settings.ExpectedWorkWeek.Add(DayOfWeek.Sunday, new TimeSpan());
          return;
        }
      }
      // Set Mon-Thurs, friday and weekend off
      if (args.Length == 2)
      {
        if (TimeSpan.TryParse(args[0], out TimeSpan weekdays)
        && TimeSpan.TryParse(args[1], out TimeSpan friday))
        {
          settings.ExpectedWorkWeek = new Dictionary<DayOfWeek, TimeSpan>();
          settings.ExpectedWorkWeek.Add(DayOfWeek.Monday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Tuesday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Wednesday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Thursday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Friday, friday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Saturday, new TimeSpan());
          settings.ExpectedWorkWeek.Add(DayOfWeek.Sunday, new TimeSpan());
          return;
        }
      }
      // Set Mon-Thurs, friday and Weekend
      if (args.Length == 3)
      {
        if (TimeSpan.TryParse(args[0], out TimeSpan weekdays)
        && TimeSpan.TryParse(args[1], out TimeSpan friday)
        && TimeSpan.TryParse(args[2], out TimeSpan weekend))
        {
          settings.ExpectedWorkWeek = new Dictionary<DayOfWeek, TimeSpan>();
          settings.ExpectedWorkWeek.Add(DayOfWeek.Monday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Tuesday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Wednesday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Thursday, weekdays);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Friday, friday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Saturday, weekend);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Sunday, weekend);
          return;
        }
      }
      // Set mon,tue,wens,thurs,fri and weekend off
      if (args.Length == 5)
      {
        if (TimeSpan.TryParse(args[0], out TimeSpan monday)
         && TimeSpan.TryParse(args[1], out TimeSpan tuesday)
         && TimeSpan.TryParse(args[2], out TimeSpan wednesday)
         && TimeSpan.TryParse(args[3], out TimeSpan thursday)
         && TimeSpan.TryParse(args[4], out TimeSpan friday))
        {
          settings.ExpectedWorkWeek = new Dictionary<DayOfWeek, TimeSpan>();
          settings.ExpectedWorkWeek.Add(DayOfWeek.Monday, monday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Tuesday, tuesday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Wednesday, wednesday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Thursday, thursday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Friday, friday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Saturday, new TimeSpan());
          settings.ExpectedWorkWeek.Add(DayOfWeek.Sunday, new TimeSpan());
          return;
        }
      }
      // set all days in the week
      if (args.Length == 7)
      {
        if (TimeSpan.TryParse(args[0], out TimeSpan monday)
         && TimeSpan.TryParse(args[1], out TimeSpan tuesday)
         && TimeSpan.TryParse(args[2], out TimeSpan wednesday)
         && TimeSpan.TryParse(args[3], out TimeSpan thursday)
         && TimeSpan.TryParse(args[4], out TimeSpan friday)
         && TimeSpan.TryParse(args[5], out TimeSpan saturday)
         && TimeSpan.TryParse(args[6], out TimeSpan sunday))
        {
          settings.ExpectedWorkWeek = new Dictionary<DayOfWeek, TimeSpan>();
          settings.ExpectedWorkWeek.Add(DayOfWeek.Monday, monday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Tuesday, tuesday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Wednesday, wednesday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Thursday, thursday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Friday, friday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Saturday, saturday);
          settings.ExpectedWorkWeek.Add(DayOfWeek.Sunday, sunday);
          return;
        }
      }
      terminal.WriteLine("Usage: Setting");

    }
    void HandleDaysStatus()
    {
      StatusForActiveMonth();
    }
    void HandleDaysStatusWithLimit(string[] args)
    {
      if (args.Length == 0 || !int.TryParse(args[0], out int dayLimit))
      {
        terminal.WriteLine("Usage: days");
        return;
      }
      StatusForActiveMonth(dayLimit);
    }
    void HandleDayGet(string[] args)
    {
      if (args.Length == 0 || !int.TryParse(args[0], out int dayID))
      {
        terminal.WriteLine("Usage: day");
        return;
      }
      calendar.ActivateDay(dayID);
      if (calendar.IsDayActive() == false)
      {
        terminal.WriteLine("No day loaded.");
      }
    }
    void HandleDaySetStart(string[] args)
    {
      if (args.Length == 0 || !DateTime.TryParse(args[0], out DateTime startdatetime))
      {
        terminal.WriteLine("Usage: day");
        return;
      }
      calendar.SetDayStart(startdatetime);
      calendar.Save();
    }
    void HandleDaySetEnd(string[] args)
    {
      if (args.Length == 0 || !DateTime.TryParse(args[0], out DateTime enddateTime))
      {
        terminal.WriteLine("Usage: day");
        return;
      }
      calendar.SetDayEnd(enddateTime);
      calendar.Save();
    }
    void HandleDaySetExpectedWorkDay(string[] args)
    {
      if (args.Length == 0 || !TimeSpan.TryParse(args[0], out TimeSpan ew))
      {
        terminal.WriteLine("Usage: day");
        return;
      }
      calendar.SetDayExpectedWorkDay(ew);
      calendar.Save();
    }
    void HandleBreakToggle()
    {
      calendar.ToggleBreak();
      calendar.Save();
    }
    void HandleBreakStartWithName(string[] args)
    {
      calendar.ToggleBreak(args[0]);
      calendar.Save();
    }
    void HandleBreakSetStart(string[] args) { }
    void HandleBreakSetEnd(string[] args) { }

    // Screens. 
    void MainScreen()
    {
      var incompleteDays = calendar.GetIncomplteDays();
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
          terminal.Seperator();
          terminal.WriteLine($"Incomplete days");
          foreach (DayModel day in incompleteDays)
          {
            terminal.WriteLine($"[{day.Id:00}] {(day.StartTime.HasValue ? day.StartTime.Value.ToString("dd MMM yyyy") : "")}");
          }
          terminal.Seperator();
        }
      }
      TimeSpan deficit = TimeSpan.Zero;
      TimeSpan totalwork = TimeSpan.Zero;
      foreach (var year in calendar.GetYears())
      {
        deficit += year.Deficit;
        totalwork += year.WorkedHours;
      }
      if (settings.ShowDeficit || settings.ShowTotalWork)
      {
        if (settings.ShowDeficit)
        {
          terminal.WriteLine($"Total Deficit  : {FormatedTimeSpan(deficit)}");
        }
        if (settings.ShowTotalWork)
        {
          terminal.WriteLine($"Total Work     :  {totalwork.TotalHours:00.0} h");
        }
        terminal.Seperator();
      }
      if (calendar.IsYearActive())
      {
        DateTime currentDate = new DateTime();

        YearModel year = calendar.GetActiveYear();
        currentDate = currentDate.AddYears(year.Id - 1);
        terminal.WriteLine($"Active Year    :  [{currentDate.ToString("yy")}] {currentDate.ToString("yyyy")}");

        if (calendar.IsMonthActive())
        {
          MonthModel month = calendar.GetActiveMonth();
          currentDate = currentDate.AddMonths(month.Id - 1);
          terminal.WriteLine($"Active Month   :  [{currentDate.Month:00}] {currentDate.ToString("MMMM")}");
          if (calendar.IsDayActive())
          {
            DayModel day = calendar.GetActiveDay();
            currentDate = currentDate.AddDays(day.Id - 1);
            terminal.WriteLine($"Active day     :  [{currentDate.Day:00}] {currentDate.ToString("dddd")}");
            terminal.Seperator();
            terminal.WriteLine($"Date           :  {(day.StartTime.HasValue ? day.StartTime.Value.ToString("dd MMM yyyy") : "")}");
            terminal.WriteLine($"Started        :  {(day.StartTime.HasValue ? day.StartTime.Value.ToString("hh:mm:ss") : "")}");
            terminal.WriteLine($"Ended          :  {(day.EndTime.HasValue ? day.EndTime.Value.ToString("hh:mm:ss") : "")}");
            if (day.IsOnBreak)
            {
              terminal.WriteLine($"Staus          :  IS ON BREAK!");
            }
            terminal.Seperator();

            // Breaks
            if (day.Breaks.Count > 0)
            {
              if (day.Breaks.Count == 1 && day.IsOnBreak)
              { // do not print when on break and no breaks ready.
              }
              else
              {
                terminal.Write($"Breaks         :  ");
                for (int i = 0; i < day.Breaks.Count; i++)
                {
                  var dayBreak = day.Breaks[i];
                  if (dayBreak.IsCompleted == false)
                  {
                    // Skip non completed breaks.
                    continue;
                  }
                  if (i == 0)
                  {
                    terminal.WriteLine($"{dayBreak.Duration().ToString("hh':'mm':'ss")} {dayBreak.Name}");
                  }
                  else
                  {
                    terminal.WriteLine($"               :  {dayBreak.Duration().ToString("hh':'mm':'ss")} {dayBreak.Name}");
                  }
                }
                terminal.Seperator();
              }
            }
            terminal.WriteLine($"Expected work  :  {day.ExpectedWorkDay}");
            terminal.WriteLine($"Actual worked  : {FormatedActualWorkDay(day)}");
            terminal.WriteLine($"Deficit        : {FormatedTimeSpan(day.Deficit)}");
          }
        }
        terminal.Seperator();
      }
    }
    void DebugScreen()
    {
      terminal.Seperator();
      Process p = Process.GetCurrentProcess();
      long ram = p.PrivateMemorySize64;
      terminal.WriteLine($"RAM: {ram / 1024 / 1024} MB");
      p.Dispose();
      terminal.Seperator();
      terminal.WriteLine($"Keeper name: {settings.KeeperName}");
      terminal.WriteLine($"Rounding   : {settings.Rounding}");
      terminal.WaitForKeypress();
      terminal.Seperator();
      var years = calendar.GetYears();
      var yearsDeficit = TimeSpan.Zero;
      var daysCount = 0;
      var monthsCount = 0;
      var yearsCount = years.Count;

      foreach (YearModel year in years)
      {

        DateOnly date = new DateOnly();
        date = date.AddYears(year.Id - 1);
        terminal.WriteLine($"[{date.ToString("yy")}] {year.Id}.");

        var months = year.GetMonths();
        var monthDeficit = TimeSpan.Zero;
        monthsCount += months.Count;

        terminal.WriteLine($" - Months loaded: {months.Count}");

        foreach (MonthModel month in months)
        {
          date = date.AddMonths(month.Id - 1);
          terminal.WriteLine($"   [{month.Id:00}] {date.ToString("MMMM")}.");

          var days = month.GetDays();
          var dayDeficit = TimeSpan.Zero;

          daysCount += days.Count;

          terminal.WriteLine($"    - Days loaded: {days.Count}");
          foreach (DayModel day in days)
          {
            if (day.IsComplete)
            {
              terminal.WriteLine($"       - [{day.Id:00}] Deficit : {FormatedTimeSpan(day.Deficit)}");
              dayDeficit += day.Deficit;
            }
            else
            {
              terminal.WriteLine($"       - [{day.Id:00}] Deficit : 00:00:00");
            }
          }
          terminal.WriteLine($"              - Total : {FormatedTimeSpan(dayDeficit)}");
          terminal.WriteLine($"    - Month Deficit   : {FormatedTimeSpan(month.Deficit)}");
          terminal.WriteLine($"    - counted Deficit : {FormatedTimeSpan(dayDeficit)}");

          monthDeficit += month.Deficit;

        }

        terminal.WriteLine($" - year Deficit    : {FormatedTimeSpan(year.Deficit)}");
        terminal.WriteLine($" - counted Deficit : {FormatedTimeSpan(monthDeficit)}");

        yearsDeficit += year.Deficit;
      }
      terminal.Seperator();
      terminal.WriteLine($"Total Deficit : {FormatedTimeSpan(yearsDeficit)}");
      terminal.Seperator();

      terminal.WriteLine($"Loaded Years  : {yearsCount}");
      terminal.WriteLine($"Loaded Months : {monthsCount}");
      terminal.WriteLine($"Loaded Days   : {daysCount}");
      terminal.Seperator();
    }
    void StatusForActiveMonth(int limit = -1)
    {
      var days = calendar.GetDays();
      var startindex = 0;
      var endindex = days.Count;
      if (limit > -1)
      {
        startindex = endindex - limit;
      }
      for (int i = startindex; i < endindex; i++)
      {
        DayModel day = days[i];
        terminal.WriteLine($"[{day.Id:00}] {day.StartTime.Value.ToString("yyyy MMM dd")} - Worked [{day.Worked.TotalHours:0.00}]");
      }
    }

    // Formating.
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
  }
}