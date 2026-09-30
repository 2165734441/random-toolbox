using RandomToolbox.App.Models;

namespace RandomToolbox.App.Services;

public static class HistoryService
{
    public static void Add(AppData data, string type, string settings, string result)
    {
        if (!data.Settings.SaveHistory) return;
        data.History.Insert(0, new HistoryItem
        {
            Time = DateTime.Now,
            Type = type,
            Settings = settings,
            Result = result
        });

        if (data.Settings.HistoryLimit > 0 && data.History.Count > data.Settings.HistoryLimit)
            data.History.RemoveRange(data.Settings.HistoryLimit, data.History.Count - data.Settings.HistoryLimit);
    }
}
