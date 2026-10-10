namespace ImpulseBot;

public class Lesson
{
    public string Group { get; set; } = "";
    public string Date { get; set; } = "";
    public string Bell { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Room { get; set; } = "";
    public bool IsReplacement { get; set; } = false;
}

public class UserState
{
    public long ChatId { get; set; }
    public int? SelectedCourse { get; set; }
    public string? SelectedGroup { get; set; }
    public int? SelectedDay { get; set; }
}

public class ScheduleEntry
{
    public string Group { get; set; } = "";
    public string Day { get; set; } = "";      // "Пн", "Вт", ...
    public string Bell { get; set; } = "";     // "1", "2", ...
    public string Subject { get; set; } = "";
    public string Room { get; set; } = "";
    public string Teacher { get; set; } = "";
}