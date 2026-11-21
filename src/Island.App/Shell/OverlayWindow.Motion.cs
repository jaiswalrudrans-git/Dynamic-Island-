using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Island.App.Shell;

public partial class OverlayWindow
{
    private void EnterShell(object sender, MouseEventArgs e)
    {
        intent.Stop();
        if (Paused || suppressHover || menuOpen || motion.Expanded) return;
        if (motion.Progress > 0 || settings.Current.HoverDelayMs <= 15) Expand(true);
        else Schedule(true);
    }

    private void LeaveShell(object sender, MouseEventArgs e)
    {
        suppressHover = false;
        intent.Stop();
        if (!Pinned && !Paused && !menuOpen && motion.Expanded) Schedule(false);
    }

    private void Schedule(bool opening)
    {
        intent.Stop();
        openingIntent = opening;
        int delay = opening
            ? settings.Current.HoverDelayMs
            : Math.Max(250, settings.Current.CollapseDelayMs);
        intent.Interval = TimeSpan.FromMilliseconds(delay);
        intent.Start();
    }

    private void AnimateTypographyTransition()
    {
        if (Body == null) return;
        var anim = new System.Windows.Media.Animation.DoubleAnimation(0.3, 1.0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
        };
        Body.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    private void IntentTick(object? sender, EventArgs e)
    {
        intent.Stop();
        if (Paused || menuOpen) return;
        if (openingIntent && Shell.IsMouseOver && !suppressHover) Expand(true);
        if (!openingIntent && !Shell.IsMouseOver && !Pinned) Expand(false);
    }

    private void Expand(bool open)
    {
        intent.Stop();
        if (open)
        {
            UpdateExternalForegroundWindow();
        }
        else
        {
            Keyboard.ClearFocus();
            SetTypingFocus(false);
        }
        motion.Target(open);
        UpdateDemands();
        UpdatePreviewLabel();
        SyncEscape();
        RequestFrame();
    }

    private void Collapse()
    {
        Expand(false);
    }

    private void RequestFrame()
    {
        if (rendering || Paused) return;
        rendering = true;
        lastFrame = Stopwatch.GetTimestamp();
        CompositionTarget.Rendering += RenderFrame;
    }

    private void RenderFrame(object? sender, EventArgs e)
    {
        long now = Stopwatch.GetTimestamp();
        var config = settings.Current;
        double dt = Stopwatch.GetElapsedTime(lastFrame, now).TotalSeconds * config.AnimationSpeed;
        lastFrame = now;
        dt = Math.Clamp(dt, 0.001, 0.033);
        bool moving = motion.Advance(dt, config.ReduceMotion);
        bool switching = navigation.Advance(dt, config.ReduceMotion);
        bool sizingW = currentWidthSpring.Advance(targetIslandWidth, dt);
        bool sizingH = currentHeightSpring.Advance(targetIslandHeight, dt);
        double targetCollapsedW = GetTargetCollapsedWidth();
        if (config.ReduceMotion)
        {
            previewWidth.Set(targetCollapsedW);
            alertSpring.Set(alerts.HasActiveAlert ? 1 : 0);
            currentWidthSpring.Set(targetIslandWidth);
            currentHeightSpring.Set(targetIslandHeight);
            sizingW = false;
            sizingH = false;
        }
        bool widening = previewWidth.Advance(targetCollapsedW, dt);
        bool alerting = alertSpring.Advance(alerts.HasActiveAlert ? 1 : 0, dt);
        AlertY.Y = config.ReduceMotion ? 0 : (1 - alertSpring.Value) * -12;
        DrawPanel();
        if (!moving && !switching && !widening && !alerting && !sizingW && !sizingH) StopFrames();
    }

    private void StopFrames()
    {
        if (!rendering) return;
        CompositionTarget.Rendering -= RenderFrame;
        rendering = false;
    }

    private void Dismiss()
    {
        if (Pinned && !Save(s => s with { IsPinned = false })) return;
        suppressHover = Shell.IsMouseOver;
        Expand(false);
    }

    private void SyncEscape()
    {
        if (source is null) return;
        bool needed = motion.Expanded && !Paused && !menuOpen && preferences is null;
        if (needed == escapeRegistered) return;
        if (needed)
        {
            escapeRegistered = RegisterHotKey(source.Handle, EscapeId, 0x4000, 0x1B);
            if (!escapeRegistered) log("Escape shortcut unavailable while the panel is open.");
        }
        else
        {
            UnregisterHotKey(source.Handle, EscapeId);
            escapeRegistered = false;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hwnd, int id);
}
