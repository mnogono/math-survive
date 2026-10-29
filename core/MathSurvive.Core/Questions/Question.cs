using System;
using System.Collections.Generic;

namespace MathSurvive.Core.Questions;

public sealed record QuestionOption(string? Text, string? Svg);

public sealed record Question(string Id, int Level, string? Prompt, string? PromptSvg,
    IReadOnlyList<QuestionOption> Options, int Correct);

public sealed record SectionInfo(string Id, string Title, int Levels);

public sealed record QuestionPack(SectionInfo Section, IReadOnlyList<Question> Questions);

public sealed class PackFormatException(string message) : Exception(message);
