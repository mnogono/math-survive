using Godot;
using MathSurvive.Core.Session;

namespace MathSurvive.Game;

public partial class TrayController : Node
{
    private const int HordeItemId = 1;

    private GameSession _session = null!;
    private PopupMenu _menu = null!;

    public void Setup(GameSession session)
    {
        _session = session;
        _menu = new PopupMenu();
        _menu.AddItem("Запустить орду", HordeItemId);
        _menu.IdPressed += OnItemPressed;
        AddChild(_menu);

        var indicator = new StatusIndicator
        {
            Icon = GD.Load<Texture2D>("res://assets/tray.svg"),
            Tooltip = "Math Survive",
        };
        AddChild(indicator);
        indicator.Menu = _menu.GetPath();

        _session.ModeChanged += _ => UpdateMenu();
        UpdateMenu();
    }

    private void OnItemPressed(long id)
    {
        if (id == HordeItemId)
            _session.ForceHordeNow();
    }

    private void UpdateMenu()
    {
        _menu.SetItemDisabled(_menu.GetItemIndex(HordeItemId), !_session.CanForceHordeNow);
    }
}
