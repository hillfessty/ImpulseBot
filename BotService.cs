using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ImpulseBot;

public class BotService
{
    private readonly TelegramBotClient _bot;
    private readonly Dictionary<long, UserState> _states = new();

    public BotService(string token)
    {
        var options = new TelegramBotClientOptions(
            token,
            baseUrl: "https://late-dust-46f7.balysevasveta000.workers.dev"
        );
        _bot = new TelegramBotClient(options);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var me = await _bot.GetMe(ct);
        Console.WriteLine($"✅ Бот запущен: @{me.Username}");
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken ct)
    {
        try
        {
            if (update.CallbackQuery is { } cb)
            {
                await HandleCallbackAsync(cb, ct);
                return;
            }

            if (update.Message is not { } msg) return;
            if (msg.Text is not { } text) return;

            var chatId = msg.Chat.Id;
            var name = msg.From?.FirstName ?? "студент";

            if (!_states.ContainsKey(chatId))
                _states[chatId] = new UserState { ChatId = chatId };

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {name}: {text}");

            if (text == "/start" || text == "/menu")
            {
                await SendMainMenu(chatId, name, ct);
                return;
            }

            if (text == "/help")
            {
                await SendHelp(chatId, ct);
                return;
            }

            await _bot.SendMessage(chatId, "Не понимаю. Открой /menu.", cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
        }
    }

    // ГЛАВНОЕ МЕНЮ
    private async Task SendMainMenu(long chatId, string name, CancellationToken ct)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("📅 Расписание", "menu_schedule") },
            new[] { InlineKeyboardButton.WithUrl("🔄 Замены (Яндекс.Диск)", Config.YandexDiskUrl) },
            new[] { InlineKeyboardButton.WithUrl("🌐 Сайт техникума", Config.SiteUrl) },
            new[] { InlineKeyboardButton.WithCallbackData("ℹ️ Помощь", "menu_help") }
        });

        await _bot.SendMessage(chatId,
            $"👋 Привет, {name}!\n\n" +
            "Я бот техникума «Импульс».\n" +
            "Показываю расписание по дням.\n\n" +
            "Выбери, что тебе нужно:",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    // ПОМОЩЬ
    private async Task SendHelp(long chatId, CancellationToken ct)
    {
        await _bot.SendMessage(chatId,
            "ℹ️ <b>Помощь</b>\n\n" +
            "📅 <b>Расписание</b> — выбрать курс, группу и день недели\n" +
            "🔄 <b>Замены</b> — открыть Яндекс.Диск техникума\n" +
            "🌐 <b>Сайт</b> — официальный сайт satehm.ru\n\n" +
            "Если в расписании нет кабинета или предмета — " +
            "уточните у старосты или в заменах.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    // ВЫБОР КУРСА
    private async Task SendCoursePicker(long chatId, CancellationToken ct)
    {
        var rows = Config.GroupsByCourse.Keys
            .OrderBy(c => c)
            .Select(c => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"🎓 {c} курс",
                    $"course_{c}")
            })
            .Append(new[] { InlineKeyboardButton.WithCallbackData("🏠 Главное меню", "menu_main") })
            .ToArray();

        await _bot.SendMessage(chatId,
            "🎓 <b>Выбери свой курс:</b>",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(rows),
            cancellationToken: ct);
    }

    // ВЫБОР ГРУППЫ
    private async Task SendGroupPicker(long chatId, int course, CancellationToken ct)
    {
        if (!Config.GroupsByCourse.TryGetValue(course, out var groups))
        {
            await _bot.SendMessage(chatId, "Курс не найден.", cancellationToken: ct);
            return;
        }

        // Группы по 2 в ряд для компактности
        var rows = new List<InlineKeyboardButton[]>();
        for (int i = 0; i < groups.Length; i += 2)
        {
            var row = new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(groups[i], $"group_{groups[i]}")
            };
            if (i + 1 < groups.Length)
                row.Add(InlineKeyboardButton.WithCallbackData(groups[i + 1], $"group_{groups[i + 1]}"));
            rows.Add(row.ToArray());
        }

        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ К курсам", "menu_schedule") });
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🏠 Главное меню", "menu_main") });

        await _bot.SendMessage(chatId,
            $"🎓 <b>{course} курс</b>\n\nВыбери свою группу:",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(rows),
            cancellationToken: ct);
    }

    // ВЫБОР ДНЯ НЕДЕЛИ
    private async Task SendDayPicker(long chatId, string group, CancellationToken ct)
    {
        var dayNames = new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };

        var rows = new List<InlineKeyboardButton[]>();
        for (int i = 0; i < 6; i += 2)
        {
            var row = new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(dayNames[i], $"day_{group}_{i + 1}")
            };
            if (i + 1 < 6)
                row.Add(InlineKeyboardButton.WithCallbackData(dayNames[i + 1], $"day_{group}_{i + 2}"));
            rows.Add(row.ToArray());
        }

        var backCourse = _states.TryGetValue(chatId, out var st) ? st.SelectedCourse ?? 1 : 1;

        rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"⬅️ К группам {backCourse} курса", $"course_{backCourse}") });
        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🏠 Главное меню", "menu_main") });

        await _bot.SendMessage(chatId,
            $"📚 Группа: <b>{group}</b>\n\n<b>Выбери день недели:</b>",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(rows),
            cancellationToken: ct);
    }

    // РАСПИСАНИЕ НА ДЕНЬ
    private async Task SendScheduleForDay(long chatId, string group, int day, CancellationToken ct)
    {
        var dayNames = new Dictionary<int, string>
        {
            [1] = "Пн", [2] = "Вт", [3] = "Ср", [4] = "Чт", [5] = "Пт", [6] = "Сб"
        };

        var dayFullNames = new Dictionary<int, string>
        {
            [1] = "Понедельник", [2] = "Вторник", [3] = "Среда",
            [4] = "Четверг", [5] = "Пятница", [6] = "Суббота"
        };

        var bellEmoji = new Dictionary<string, string>
        {
            ["1"] = "1️⃣", ["2"] = "2️⃣", ["3"] = "3️⃣", ["4"] = "4️⃣"
        };

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📅 <b>{group}</b>");
        sb.AppendLine($"<b>{dayFullNames[day]}</b>");
        sb.AppendLine("─────────────────────");

        // ДИАГНОСТИКА
        Console.WriteLine($"[DEBUG] Группа: {group}, День: {dayNames[day]}");

        if (Config.Schedule.TryGetValue(group, out var byDay))
        {
            Console.WriteLine($"[DEBUG] Найдено дней для {group}: {byDay.Count}");
            Console.WriteLine($"[DEBUG] Дни: {string.Join(", ", byDay.Keys)}");

            if (byDay.TryGetValue(dayNames[day], out var byBell))
            {
                Console.WriteLine($"[DEBUG] Найдено лент для {dayNames[day]}: {byBell.Count}");

                var lessons = byBell.Values
                    .OrderBy(l => int.TryParse(l.Bell, out var n) ? n : 99)
                    .ToList();

                if (lessons.Count == 0)
                {
                    sb.AppendLine("🎉 Пар нет!");
                }
                else
                {
                    foreach (var lesson in lessons)
                    {
                        var subject = string.IsNullOrEmpty(lesson.Subject)
                            ? "<i>— уточните у старосты</i>"
                            : $"<b>{lesson.Subject}</b>";

                        var bellIcon = bellEmoji.ContainsKey(lesson.Bell)
                            ? bellEmoji[lesson.Bell]
                            : $"<b>{lesson.Bell}</b>";

                        sb.AppendLine($"{bellIcon} {subject}");

                        var parts = new List<string>();
                        if (!string.IsNullOrEmpty(lesson.Room)) parts.Add($"📍 {lesson.Room}");
                        if (!string.IsNullOrEmpty(lesson.Teacher)) parts.Add($"👤 {lesson.Teacher}");

                        if (parts.Count > 0)
                            sb.AppendLine($"    {string.Join("  ·  ", parts)}");

                        sb.AppendLine();
                    }
                }
            }
            else
            {
                Console.WriteLine($"[DEBUG] День {dayNames[day]} не найден у {group}");
                sb.AppendLine("⚠️ <i>Для этого дня нет данных.</i>");
            }
        }
        else
        {
            Console.WriteLine($"[DEBUG] Группа {group} не найдена в Config.Schedule");
            sb.AppendLine("⚠️ <i>Группа не найдена.</i>");
        }

        var backCourse = _states.TryGetValue(chatId, out var st) ? st.SelectedCourse ?? 1 : 1;

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("🔄 Замены", Config.YandexDiskUrl) },
            new[] { InlineKeyboardButton.WithCallbackData("📆 Другой день", $"group_{group}") },
            new[] { InlineKeyboardButton.WithCallbackData("📚 Другая группа", $"course_{backCourse}") },
            new[] { InlineKeyboardButton.WithCallbackData("🎓 Другой курс", "menu_schedule") },
            new[] { InlineKeyboardButton.WithCallbackData("🏠 Главное меню", "menu_main") }
        });

        await _bot.SendMessage(chatId, sb.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    // ОБРАБОТКА КНОПОК
    private async Task HandleCallbackAsync(CallbackQuery cb, CancellationToken ct)
    {
        var chatId = cb.Message!.Chat.Id;
        var data = cb.Data ?? "";

        await _bot.AnswerCallbackQuery(cb.Id, cancellationToken: ct);

        if (!_states.ContainsKey(chatId))
            _states[chatId] = new UserState { ChatId = chatId };

        var state = _states[chatId];

        switch (data)
        {
            case "menu_main":
                await SendMainMenu(chatId, cb.From.FirstName, ct);
                return;

            case "menu_schedule":
                await SendCoursePicker(chatId, ct);
                return;

            case "menu_help":
                await SendHelp(chatId, ct);
                return;
        }

        if (data.StartsWith("course_"))
        {
            if (int.TryParse(data.Substring("course_".Length), out var course))
            {
                state.SelectedCourse = course;
                state.SelectedGroup = null;
                state.SelectedDay = null;
                await SendGroupPicker(chatId, course, ct);
            }
            return;
        }

        if (data.StartsWith("group_"))
        {
            var group = data.Substring("group_".Length);
            state.SelectedGroup = group;
            state.SelectedDay = null;
            await SendDayPicker(chatId, group, ct);
            return;
        }

        if (data.StartsWith("day_"))
        {
            var rest = data.Substring("day_".Length);
            var lastUnderscore = rest.LastIndexOf('_');
            if (lastUnderscore > 0)
            {
                var group = rest.Substring(0, lastUnderscore);
                var dayStr = rest.Substring(lastUnderscore + 1);

                if (int.TryParse(dayStr, out var day))
                {
                    state.SelectedGroup = group;
                    state.SelectedDay = day;
                    await SendScheduleForDay(chatId, group, day, ct);
                }
            }
            return;
        }
    }
}