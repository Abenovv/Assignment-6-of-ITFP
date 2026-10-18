using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tracker.Domain;

namespace Tracker.Input;

// Nullable raw fields let validation distinguish missing values from 0 and false.
public sealed record RawTask(
    string? Id, string? Title, string? Date, int? Importance, int? Minutes, bool? Completed);

public static class TaskParser
{
    public static bool TryParse(string? json, out IReadOnlyList<TaskItem> tasks, out string error)
    {
        tasks = Array.Empty<TaskItem>();
        error = "";
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "A JSON array is required. Use [] for an empty dataset.";
            return false;
        }

        RawTask?[]? raw;
        try
        {
            raw = JsonSerializer.Deserialize<RawTask?[]>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
            });
        }
        catch (JsonException exception)
        {
            error = $"Invalid JSON or field type at {exception.Path ?? "$"}. {exception.Message}";
            return false;
        }

        if (raw is null)
        {
            error = "Input cannot be null. Use [] for an empty dataset.";
            return false;
        }

        var validated = new List<TaskItem>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < raw.Length; index++)
        {
            RawTask? item = raw[index];
            string prefix = $"Record {index + 1}: ";
            if (item is null)
                error = prefix + "a task object is required.";
            else if (string.IsNullOrWhiteSpace(item.Id))
                error = prefix + "id is required.";
            else if (string.IsNullOrWhiteSpace(item.Title))
                error = prefix + "title is required.";
            else if (!DateOnly.TryParseExact(item.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                         DateTimeStyles.None, out _))
                error = prefix + "date is required and must be a real date in yyyy-MM-dd format.";
            else if (item.Importance is null or < 1 or > 5)
                error = prefix + "importance is required and must be an integer from 1 to 5.";
            else if (item.Minutes is null or < 0 or > 480)
                error = prefix + "minutes is required and must be an integer from 0 to 480.";
            else if (item.Completed is null)
                error = prefix + "completed is required and must be true or false.";
            else if (!ids.Add(item.Id.Trim()))
                error = prefix + "id must be unique.";

            if (error.Length > 0)
                return false;

            // Validation above established all required values. No defaults are inserted.
            if (item is { Id: not null, Title: not null, Date: string dateText, Importance: int importance,
                          Minutes: int minutes, Completed: bool completed })
            {
                var date = DateOnly.ParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                validated.Add(new TaskItem(item.Id.Trim(), item.Title.Trim(), date, importance, minutes, completed));
            }
        }

        tasks = Array.AsReadOnly(validated.ToArray());
        return true;
    }
}
