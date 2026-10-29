using System.Collections.Generic;
using MathSurvive.Core.Questions;

namespace MathSurvive.Core.Session;

/// <summary>Показываемый вопрос. <c>Order[displayIndex]</c> — индекс варианта в <c>Question.Options</c>.</summary>
public sealed record QuizView(Question Question, IReadOnlyList<int> Order);

/// <summary>Итог ответа; индексы — в порядке показа (display).</summary>
public sealed record AnswerResult(bool Correct, int ChosenDisplay, int CorrectDisplay);

/// <summary>Орда: номер (с 1) и число зомби.</summary>
public sealed record HordeInfo(int Number, int ZombieCount);
