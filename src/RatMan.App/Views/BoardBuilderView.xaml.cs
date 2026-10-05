using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RatMan.App.ViewModels;
using RatMan.App.Animation;

namespace RatMan.App.Views;

public partial class BoardBuilderView : UserControl
{
    private RenderingClock? renderingClock;
    private BoardBuilderViewModel? Model => DataContext as BoardBuilderViewModel;
    public BoardBuilderView() => InitializeComponent();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        renderingClock?.Dispose();
        renderingClock = Model is { } model ? new RenderingClock(model.Advance) : null;
        renderingClock?.Start();
        Focus();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        renderingClock?.Dispose();
        renderingClock = null;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBoxBase) return;
        switch (e.Key)
        {
            case Key.D1: case Key.NumPad1: if (Model is { } wall) wall.SelectedTool = BuilderTool.Wall; break;
            case Key.D2: case Key.NumPad2: if (Model is { } player) player.SelectedTool = BuilderTool.Player; break;
            case Key.D3: case Key.NumPad3: if (Model is { } monster) monster.SelectedTool = BuilderTool.Monster; break;
            case Key.D4: case Key.NumPad4: if (Model is { } normal) normal.SelectedTool = BuilderTool.NormalCheese; break;
            case Key.D5: case Key.NumPad5: if (Model is { } steroid) steroid.SelectedTool = BuilderTool.SteroidCheese; break;
            case Key.R: Model?.Rotate(); break;
            case Key.Escape: Model?.ReturnToMenuCommand.Execute(null); break;
            default: return;
        }
        UpdatePreview();
        e.Handled = true;
    }

    private void UpdatePreview()
    {
        if (!BoardCanvas.IsMouseOver) return;
        Point point = Mouse.GetPosition(BoardCanvas);
        Model?.Preview(point.X, point.Y);
    }

    private void OnBoardMove(object sender, MouseEventArgs e) => UpdatePreview();
    private void OnBoardLeave(object sender, MouseEventArgs e) => Model?.ClearPreview();
    private void OnBoardClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton is not (MouseButton.Left or MouseButton.Right)) return;
        Focus(); Keyboard.Focus(this);
        Point point = e.GetPosition(BoardCanvas);
        if (e.ChangedButton == MouseButton.Left) Model?.Place(point.X, point.Y);
        else Model?.Remove(point.X, point.Y);
        UpdatePreview(); e.Handled = true;
    }

    private void OnDimensionLostFocus(object sender, RoutedEventArgs e) => Model?.ApplyDimensions();
    private void OnDimensionKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        Model?.ApplyDimensions(); e.Handled = true;
    }
}
