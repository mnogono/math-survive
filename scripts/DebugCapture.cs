using Godot;

namespace MathSurvive.Game;

/// <summary>Автозагрузка: по --quit-after ждёт, снимает кадр в --shot (если задан) и закрывает игру. Работает для любой сцены.</summary>
public partial class DebugCapture : Node
{
    public override async void _Ready()
    {
        var args = DebugArgs.Current;
        if (args.QuitAfterSec is not { } sec)
            return;

        await ToSignal(GetTree().CreateTimer(sec), SceneTreeTimer.SignalName.Timeout);
        // В режиме экономии кадр статичной сцены не перерисовывается — кадра можно ждать бесконечно.
        OS.LowProcessorUsageMode = false;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

        if (args.ShotPath is { } path)
        {
            var err = GetViewport().GetTexture().GetImage().SavePng(path);
            if (err == Error.Ok)
                GD.Print($"DebugCapture: снимок сохранён в {path}");
            else
                GD.PushError($"DebugCapture: не удалось сохранить снимок в {path}: {err}");
        }

        GetTree().Quit();
    }
}
