using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MathSurvive.Core.Questions;

namespace MathSurvive.Core.Progress;

/// <summary>Файл прогресса: чтение без исключений, запись через временный файл в том же каталоге.</summary>
public sealed class ProgressFileStore(string path)
{
    public string Path { get; } = path;

    /// <summary>Временный файл записи; после успешного сохранения его нет.</summary>
    public string TempPath => Path + ".tmp";

    /// <returns><c>null</c>, если файла нет, его не прочитать или содержимое недопустимо.</returns>
    public QuizProgressSnapshot? Load(IReadOnlyList<QuestionPack> packs, GameConfig config)
    {
        string json;
        try
        {
            if (!File.Exists(Path)) return null;
            json = File.ReadAllText(Path, Encoding.UTF8);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
        return ProgressSaveCodec.Deserialize(json, packs, config);
    }

    /// <summary>Записать во временный файл, сбросить на диск и заменить им <see cref="Path"/>.</summary>
    public void Save(QuizProgressSnapshot snapshot)
    {
        var json = ProgressSaveCodec.Serialize(snapshot);
        var dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        try
        {
            using (var stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(json);
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(TempPath, Path, overwrite: true);
        }
        catch
        {
            TryDelete(TempPath);
            throw;
        }
    }

    private static void TryDelete(string file)
    {
        try { File.Delete(file); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}
