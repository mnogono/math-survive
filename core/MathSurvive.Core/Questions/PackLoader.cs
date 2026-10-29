using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace MathSurvive.Core.Questions;

public static class PackLoader
{
    private const int OptionCount = 4;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    public static QuestionPack Parse(string sectionJson, string questionsJson)
    {
        var section = Deserialize<SectionInfo>(sectionJson, "section.json");
        var raw = Deserialize<List<Question>>(questionsJson, "questions.json");

        var seen = new HashSet<string>();
        foreach (var q in raw)
        {
            var id = q.Id ?? "<без id>";
            if (!seen.Add(id)) throw new PackFormatException($"Повтор id вопроса: {id}");
            if (q.Options is null || q.Options.Count != OptionCount)
                throw new PackFormatException($"Вопрос {id}: должно быть {OptionCount} варианта");
            if (q.Correct < 0 || q.Correct >= OptionCount)
                throw new PackFormatException($"Вопрос {id}: correct вне диапазона 0..{OptionCount - 1}");
            if (q.Options.Any(o => o is null || (o.Text is null && o.Svg is null)))
                throw new PackFormatException($"Вопрос {id}: у варианта нет ни text, ни svg");
            if (q.Prompt is null && q.PromptSvg is null)
                throw new PackFormatException($"Вопрос {id}: нет ни prompt, ни prompt_svg");
        }

        return new QuestionPack(section, raw);
    }

    private static T Deserialize<T>(string json, string what)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Json)
                   ?? throw new PackFormatException($"{what}: пустой документ");
        }
        catch (JsonException e)
        {
            throw new PackFormatException($"{what}: {e.Message}");
        }
    }
}
