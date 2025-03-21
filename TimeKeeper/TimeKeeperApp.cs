using System.Diagnostics;
using TimeKeeper.Models;

namespace TimeKeeper
{
  /// <summary>
  /// The time keeper is a little app for simplyfying office hours.
  /// This app should not handle stuff like vacation or weeks. 
  /// This is only for clocking in and clocking out, and keeping up with flex over time. 
  /// Specefic task time is handled by other applications. 
  /// </summary>
  internal class TimeKeeperApp
  {
    static FileHandler filesystem;
    static TerminalHandler terminal;
    static CalendarHandler calendar;
    static Settings settings = new Settings();
    static bool isRunning = true;

    static void Main(string[] args)
    {
      AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

      //Console.SetWindowSize(44, 20);

      filesystem = new FileHandler(settings.DataLocation);

      LoadSettings();

      terminal = new TerminalHandler();
      calendar = new CalendarHandler(filesystem, settings.ExpectedWorkWeek);

      calendar.SetRounding(settings.Rounding);
      terminal.WriteLine($"Welcome {settings.KeeperName}");
      terminal.Seperator();
      terminal.WriteLine($"Current date : {DateTime.Now.ToString("MMMM dd, yyyy")}");
      terminal.Seperator();
      calendar.LoadYears();
      calendar.ActivateToday();
      terminal.WriteLine($"Loaded {calendar.GetDays().Count} days");
      terminal.WriteLine($"{calendar.GetIncomplteDays().Count} is incomplete");
      terminal.Seperator();
      Thread.Sleep(1500);

      while (isRunning)
      {
        terminal.Clear();
        MainScreen();
        InputHandler();
      }
    }
    // Main.
    static void InputHandler()
    {
      terminal.WriteLine("Ready for input");
      string input = terminal.GetInput();
      string[] commands = terminal.ParseCommand(input);
      if (commands.Length > 0)
      {
        switch (commands[0].ToLower())
        {
          case "exit":
            isRunning = false;
            break;
          case "debug":
            DebugScreen();
            terminal.WaitForKeypress();
            break;
          case "break":
            if (commands.Length == 2)
              calendar.ToggleBreak(commands[1]);
            else
              calendar.ToggleBreak();
            break;
          case "settings":
            if (commands[1].ToLower() == "-keeper" ||
                commands[1].ToLower() == "-k")
            {
              if (commands.Length == 3)
              {
                settings.KeeperName = commands[2];
              }
            }
            else if (commands[1].ToLower() == "-rounding" ||
                     commands[1].ToLower() == "-r")
            {
              if (commands.Length == 3)
              {
                int r = Int32.Parse(commands[2]);
                switch (r)
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
              }
            }
            else if (commands[1].ToLower() == "-showdeficit" ||
                     commands[1].ToLower() == "-sd")
            {
              bool showDeficit = false;
              if (commands[2] == "0")
              {
                // value is allready 0
              }
              else if (commands[2] == "1")
              {
                showDeficit = true;
              }
              else
              {
                showDeficit = Boolean.Parse(commands[2]);
              }
              settings.ShowDeficit = showDeficit;
            }
            else if (commands[1].ToLower() == "-showtotalwork" ||
                     commands[1].ToLower() == "-sw")
            {
              bool showWork = false;
              if (commands[2] == "0")
              {
                // value is allready 0
              }
              else if (commands[2] == "1")
              {
                showWork = true;
              }
              else
              {
                showWork = Boolean.Parse(commands[2]);
              }
              settings.ShowTotalWork = showWork;
            }
            SaveSettings();
            break;
          case "checkin":
          case "clockin":
            calendar.ClockIn(DateTime.Now);
            calendar.Save();
            break;
          case "checkout":
          case "clockout":
            calendar.ClockOut(DateTime.Now);
            calendar.Save();
            break;
          case "days":
            if (commands.Length == 1)
            {
              StatusForActiveMonth();
              InputHandler();
              break;
            }
            if (commands[1] == "-limit" ||
                commands[1] == "-l")
            {
              int l = Int32.Parse(commands[2]);
              StatusForActiveMonth(l);
              InputHandler();
              break;
            }
            if (commands[1] == "-expectedworkday" ||
                commands[1] == "-ew")
              if (commands.Length == 4)
              {
                TimeSpan ew = TimeSpan.Parse(commands[2]);
                int d = Int32.Parse(commands[3]);
                DayOfWeek wd = (DayOfWeek)d;

                calendar.SetExpectedWorkDay(wd, ew);
                if (settings.ExpectedWorkWeek.ContainsKey(wd))
                {
                  settings.ExpectedWorkWeek[wd] = ew;
                  break;
                }
                settings.ExpectedWorkWeek.Add(wd, ew);
              }
            break;
          case "day":
            if (commands[1].ToLower() == "-get" ||
                commands[1].ToLower() == "-g")
            {
              int i = Int32.Parse(commands[2]);
              calendar.ActivateDay(i);
              break;
            }
            if (calendar.IsDayActive() == false)
            {
              terminal.WriteLine("No day loaded.");
              break;
            }
            switch (commands[1])
            {
              case "-start":
              case "-s":
                DateTime startdatetime = DateTime.Parse(commands[2]);
                calendar.SetDayStart(startdatetime);
                break;
              case "-e":
              case "-end":
                DateTime enddateTime = DateTime.Parse(commands[2]);
                calendar.SetDayEnd(enddateTime);
                break;
              case "-l":
              case "-lunch":
                TimeSpan lunchtime = TimeSpan.Parse(commands[2]);
                calendar.SetDayLunch(lunchtime);
                break;
              case "-lt":
              case "-lunchtime":
                TimeSpan completed = TimeSpan.Parse(commands[2]);
                calendar.SetDayLunchCompleted(completed);
                break;
              case "-expectedworkday":
              case "-ew":
                if (commands.Length == 3)
                {
                  TimeSpan ew = TimeSpan.Parse(commands[2]);
                  calendar.SetDayExpectedWorkDay(ew);
                }
                break;
              default:
                terminal.WriteLine($"Unknown tag {commands[1]}");
                terminal.WriteLine("Valid tags: -[s]tart, -[e]nd, -[l]unch");
                break;
            }
            // Save changes to disk.
            calendar.Save();
            break;
          default:
            terminal.WriteLine($"Unknown Command {commands[0]}");
            terminal.WaitForKeypress();
            break;
        }
      }
    }
    static void LoadSettings()
    {
      string settingsFileName = $"settings.json";
      if (filesystem.FileExists(settingsFileName))
      {
        settings = filesystem.Deserialize<Settings>(settingsFileName, true);
      }
    }
    static void SaveSettings()
    {
      filesystem.Serialize<Settings>("settings.json", settings);
    }
    // Events.
    static void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
      terminal.WriteLine("Saving...");
      calendar.Save();
      SaveSettings();
      terminal.WriteLine("Done");
      Thread.Sleep(500);
    }

    // Screens. 
    private static void MainScreen()
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
            terminal.WriteLine($"Deficit        : {FormatedTimeSpan(day.GetDeficit())}");
          }
        }
        terminal.Seperator();
      }
    }
    private static void DebugScreen()
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
            terminal.WriteLine($"       - [{day.Id:00}] Deficit : {FormatedTimeSpan(day.GetDeficit())}");
            dayDeficit += day.GetDeficit();
            terminal.WriteLine($"                Total : {FormatedTimeSpan(dayDeficit)}");
          }
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
    private static void StatusForActiveMonth(int limit = -1)
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
        terminal.WriteLine($"[{day.Id:00}] {day.StartTime.Value.ToString("yyyy MMM dd")} - Worked [{day.GetActualWorkDay().TotalHours:0.00}]");
      }
    }

    // Formating.
    private static string FormatedActualWorkDay(DayModel day)
    {
      var worked = day.GetActualWorkDay();
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
    private static string FormatedTimeSpan(TimeSpan timeSpan)
    {
      return $"{(timeSpan.TotalMilliseconds >= 0 ? "+" : "-")}{Math.Abs(timeSpan.Hours):00}:{Math.Abs(timeSpan.Minutes):00}:{Math.Abs(timeSpan.Seconds):00}";
    }
  }

}