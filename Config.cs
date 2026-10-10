namespace ImpulseBot;

public static class Config
{

public static readonly string TelegramToken = 
    Environment.GetEnvironmentVariable("BOT_TOKEN") ?? "";

    public const string SiteUrl = "https://satehm.ru";
    public const string YandexDiskUrl = "https://disk.yandex.ru/d/--S7ETrlw5uFGA";

    public static readonly Dictionary<int, string[]> GroupsByCourse = new()
    {
        [1] = new[] {
            "160-ТМ-26", "161-ТМ-26", "139-ТО-26", "130-СВ-26", "131-КСВ-26",
            "110-ОНС-26", "150-КЭП-26", "108-КДП-26", "109-ПК-26", "103-КАРХ-26",
            "103-КСЭЗ-26", "103-ИМС-26", "101-КТЭС-26", "103-РАП-26"
        },
        [2] = new[] {
            "207-КДП-25", "207-ПК-25", "208-КПК-25", "202-КАРХ-25", "202-КСЭЗ-25",
            "202-ИМС-25", "202-КИСП-25", "202-РАП-25", "258-ТМ-25", "259-ТМ-25",
            "238-ТО-25", "229-СВ-25", "209-ОНС-25", "249-КЭП-25"
        },
        [3] = new[] {
            "356-ТМ-24", "357-ТМ-24", "337-ТО-24", "306-КПК-24", "301-КСЭЗ-24",
            "301-КИСП-24", "348-КЭП-24", "301-КАРХ-24", "301-КИМС-24"
        },
        [4] = new[] {
            "405-КПК-23", "436-ТО-23", "454-ТМ-23", "455-ТМ-23"
        }
    };

    // ⬇⬇⬇ РАСПИСАНИЕ ИЗ CSV ⬇⬇⬇
    public const string ScheduleCsvPath = @"C:\Users\Huawei\ImpulseBot\schedule.csv";

    public static Dictionary<string, Dictionary<string, Dictionary<string, ScheduleEntry>>> Schedule { get; private set; }
        = new();

    public static void LoadSchedule()
    {
        Schedule.Clear();

        if (!File.Exists(ScheduleCsvPath))
        {
            Console.WriteLine($"⚠️ Файл расписания не найден: {ScheduleCsvPath}");
            return;
        }

        var lines = File.ReadAllLines(ScheduleCsvPath, System.Text.Encoding.UTF8);
        int loaded = 0;
        int skipped = 0;

        for (int i = 1; i < lines.Length; i++)  // пропускаем заголовок
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = line.Split(';');

            if (parts.Length < 6)
            {
                skipped++;
                // Отладка: показываем проблемные строки для 160-ТМ-26
                if (line.StartsWith("160-ТМ-26;Пн"))
                    Console.WriteLine($"[CSV-SKIP] Полей {parts.Length} < 6: {line}");
                continue;
            }

            var entry = new ScheduleEntry
            {
                Group = parts[0].Trim(),
                Day = parts[1].Trim(),
                Bell = parts[2].Trim(),
                Subject = parts[3].Trim(),
                Room = parts[4].Trim(),
                Teacher = parts[5].Trim()
            };

            if (!Schedule.ContainsKey(entry.Group))
                Schedule[entry.Group] = new Dictionary<string, Dictionary<string, ScheduleEntry>>();
            if (!Schedule[entry.Group].ContainsKey(entry.Day))
                Schedule[entry.Group][entry.Day] = new Dictionary<string, ScheduleEntry>();

            Schedule[entry.Group][entry.Day][entry.Bell] = entry;
            loaded++;
        }

        Console.WriteLine($"✅ Загружено записей: {loaded}");
        Console.WriteLine($"⚠️ Пропущено строк: {skipped}");
        Console.WriteLine($"✅ Групп: {Schedule.Count}");
    }
}