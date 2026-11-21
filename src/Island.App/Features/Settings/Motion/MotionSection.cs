using Island.Core.Settings;

namespace Island.App.Features.Settings.Views;

public partial class MotionSettingsWindow
{
    private void InitializeMotion(AppSettings current)
    {
        Reduced.IsChecked = current.ReduceMotion;
        Speed.Value = current.AnimationSpeed;
        Hover.Value = current.HoverDelayMs;
        Leave.Value = current.CollapseDelayMs;

        Reduced.Click += (_, _) => save(s => s with { ReduceMotion = Reduced.IsChecked == true });

        Bind(Speed, s => s with { AnimationSpeed = Speed.Value });
        Bind(Hover, s => s with { HoverDelayMs = (int)Hover.Value });
        Bind(Leave, s => s with { CollapseDelayMs = (int)Leave.Value });
    }
}
