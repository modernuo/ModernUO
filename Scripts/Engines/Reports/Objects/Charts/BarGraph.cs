using System;
using System.Collections.Generic;

namespace Server.Engines.Reports
{
  public enum BarGraphRenderMode
  {
    Bars,
    Lines
  }

  public class BarGraph : Chart
  {
    public BarGraph(string name, string fileName, int ticks, string xTitle, string yTitle, BarGraphRenderMode rm)
    {
      m_Name = name;
      m_FileName = fileName;
      Ticks = ticks;
      this.xTitle = xTitle;
      this.yTitle = yTitle;
      RenderMode = rm;
    }

    private BarGraph()
    {
    }

    public int Ticks{ get; set; }

    public BarGraphRenderMode RenderMode{ get; set; }

    public string xTitle{ get; set; }

    public string yTitle{ get; set; }

    public int FontSize{ get; set; } = 7;

    public int Interval{ get; set; } = 1;

    public BarRegion[] Regions{ get; set; }

    public override void SerializeAttributes(PersistanceWriter op)
    {
      base.SerializeAttributes(op);

      op.SetInt32("t", Ticks);
      op.SetInt32("r", (int)RenderMode);

      op.SetString("x", xTitle);
      op.SetString("y", yTitle);

      op.SetInt32("s", FontSize);
      op.SetInt32("i", Interval);
    }

    public override void DeserializeAttributes(PersistanceReader ip)
    {
      base.DeserializeAttributes(ip);

      Ticks = ip.GetInt32("t");
      RenderMode = (BarGraphRenderMode)ip.GetInt32("r");

      xTitle = Utility.Intern(ip.GetString("x"));
      yTitle = Utility.Intern(ip.GetString("y"));

      FontSize = ip.GetInt32("s");
      Interval = ip.GetInt32("i");
    }

    public static int LookupReportValue(Snapshot ss, string reportName, string valueName)
    {
      for (int j = 0; j < ss.Children.Count; ++j)
      {
        if (!(ss.Children[j] is Report report) || report.Name != reportName)
          continue;

        for (int k = 0; k < report.Items.Count; ++k)
        {
          ReportItem item = report.Items[k];

          if (item.Values[0].Value == valueName)
            return Utility.ToInt32(item.Values[1].Value);
        }

        break;
      }

      return -1;
    }

    public static BarGraph DailyAverage(SnapshotHistory history, string reportName, string valueName)
    {
      int[] totals = new int[24];
      int[] counts = new int[24];

      int min = history.Snapshots.Count - 7 * 24; // averages over one week

      if (min < 0)
        min = 0;

      for (int i = min; i < history.Snapshots.Count; ++i)
      {
        Snapshot ss = history.Snapshots[i];

        int val = LookupReportValue(ss, reportName, valueName);

        if (val == -1)
          continue;

        int hour = ss.TimeStamp.TimeOfDay.Hours;

        totals[hour] += val;
        counts[hour]++;
      }

      BarGraph barGraph = new BarGraph("Hourly average " + valueName, "graphs_" + valueName.ToLower() + "_avg", 10,
        "Time", valueName, BarGraphRenderMode.Lines);

      barGraph.FontSize = 6;

      for (int i = 7; i <= totals.Length + 7; ++i)
      {
        int val;

        if (counts[i % totals.Length] == 0)
          val = 0;
        else
          val = (totals[i % totals.Length] + counts[i % totals.Length] / 2) / counts[i % totals.Length];

        int realHours = i % totals.Length;
        int hours;

        if (realHours == 0)
          hours = 12;
        else if (realHours > 12)
          hours = realHours - 12;
        else
          hours = realHours;

        barGraph.Items.Add(hours + (realHours >= 12 ? " PM" : " AM"), val);
      }

      return barGraph;
    }

    public static BarGraph Growth(SnapshotHistory history, string reportName, string valueName)
    {
      BarGraph barGraph = new BarGraph("Growth of " + valueName + " over time",
        "graphs_" + valueName.ToLower() + "_growth", 10, "Time", valueName, BarGraphRenderMode.Lines);

      barGraph.FontSize = 6;
      barGraph.Interval = 7;

      DateTime startPeriod = history.Snapshots[0].TimeStamp.Date + TimeSpan.FromDays(1.0);
      DateTime endPeriod = history.Snapshots[history.Snapshots.Count - 1].TimeStamp.Date;

      List<BarRegion> regions = new List<BarRegion>();

      DateTime curDate = DateTime.MinValue;
      int curPeak = -1;
      int curLow = 1000;
      int curTotl = 0;
      int curCont = 0;
      int curValu;

      for (int i = 0; i < history.Snapshots.Count; ++i)
      {
        Snapshot ss = history.Snapshots[i];
        DateTime timeStamp = ss.TimeStamp;

        if (timeStamp < startPeriod || timeStamp >= endPeriod)
          continue;

        int val = LookupReportValue(ss, reportName, valueName);

        if (val == -1)
          continue;

        DateTime thisDate = timeStamp.Date;

        if (curDate == DateTime.MinValue)
          curDate = thisDate;

        curCont++;
        curTotl += val;
        curValu = curTotl / curCont;

        if (curDate != thisDate && curValu >= 0)
        {
          string mnthName = thisDate.ToString("MMMM");

          if (regions.Count == 0)
          {
            regions.Add(new BarRegion(barGraph.Items.Count, barGraph.Items.Count, mnthName));
          }
          else
          {
            BarRegion region = regions[regions.Count - 1];

            if (region.m_Name == mnthName)
              region.m_RangeTo = barGraph.Items.Count;
            else
              regions.Add(new BarRegion(barGraph.Items.Count, barGraph.Items.Count, mnthName));
          }

          barGraph.Items.Add(thisDate.Day.ToString(), curValu);

          curPeak = val;
          curLow = val;
        }
        else
        {
          if (val > curPeak)
            curPeak = val;

          if (val > 0 && val < curLow)
            curLow = val;
        }

        curDate = thisDate;
      }

      barGraph.Regions = regions.ToArray();

      return barGraph;
    }

    public static BarGraph OverTime(SnapshotHistory history, string reportName, string valueName, int step, int max,
      int ival)
    {
      BarGraph barGraph = new BarGraph(valueName + " over time", "graphs_" + valueName.ToLower() + "_ot", 10, "Time",
        valueName, BarGraphRenderMode.Lines);

      TimeSpan ts = TimeSpan.FromHours(max * step - 0.5);

      DateTime mostRecent = history.Snapshots[history.Snapshots.Count - 1].TimeStamp;
      DateTime minTime = mostRecent - ts;

      barGraph.FontSize = 6;
      barGraph.Interval = ival;

      List<BarRegion> regions = new List<BarRegion>();

      for (int i = 0; i < history.Snapshots.Count; ++i)
      {
        Snapshot ss = history.Snapshots[i];
        DateTime timeStamp = ss.TimeStamp;

        if (timeStamp < minTime)
          continue;

        if (i % step != 0)
          continue;

        int val = LookupReportValue(ss, reportName, valueName);

        if (val == -1)
          continue;

        int realHours = timeStamp.TimeOfDay.Hours;
        int hours;

        if (realHours == 0)
          hours = 12;
        else if (realHours > 12)
          hours = realHours - 12;
        else
          hours = realHours;

        string dayName = timeStamp.DayOfWeek.ToString();

        if (regions.Count == 0)
        {
          regions.Add(new BarRegion(barGraph.Items.Count, barGraph.Items.Count, dayName));
        }
        else
        {
          BarRegion region = regions[regions.Count - 1];

          if (region.m_Name == dayName)
            region.m_RangeTo = barGraph.Items.Count;
          else
            regions.Add(new BarRegion(barGraph.Items.Count, barGraph.Items.Count, dayName));
        }

        barGraph.Items.Add(hours + (realHours >= 12 ? " PM" : " AM"), val);
      }

      barGraph.Regions = regions.ToArray();

      return barGraph;
    }

    #region Type Identification

    public static readonly PersistableType ThisTypeID = new PersistableType("bg", Construct);

    private static PersistableObject Construct()
    {
      return new BarGraph();
    }

    public override PersistableType TypeID => ThisTypeID;

    #endregion
  }
}