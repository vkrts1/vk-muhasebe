using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using ErmayMuhasebe.Avalonia.ViewModels;
using System.Linq;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class SettingsView : UserControl
{
    private ListBox? _listBox;
    private bool _isDragging;
    private OrderableItem? _draggedItem;
    private int _dragStartIndex = -1;
    private int _dropTargetIndex = -1;
    private ListBoxItem? _draggedListBoxItem;
    private ListBoxItem? _lastHighlightedItem;

    public SettingsView()
    {
        InitializeComponent();
        
        _listBox = this.FindControl<ListBox>("StatusBarListBox");
        if (_listBox != null)
        {
            _listBox.AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            _listBox.PointerMoved += OnPointerMoved;
            _listBox.PointerReleased += OnPointerReleased;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;
        if (!properties.IsLeftButtonPressed) return;

        var visual = e.Source as Visual;
        while (visual != null && !(visual is ListBoxItem))
        {
            visual = visual.GetVisualParent() as Visual;
        }

        if (visual is ListBoxItem item && item.DataContext is OrderableItem orderItem)
        {
            var vm = DataContext as SettingsViewModel;
            if (vm == null) return;

            _draggedItem = orderItem;
            _dragStartIndex = vm.StatusBarOrderItems.IndexOf(orderItem);
            _dropTargetIndex = _dragStartIndex;
            _isDragging = true;
            _draggedListBoxItem = item;

            // Visual feedback: highlight the dragged item
            item.Cursor = new Cursor(StandardCursorType.SizeAll);
            item.Opacity = 0.5;
            item.RenderTransform = new ScaleTransform(1.02, 1.02);

            e.Pointer.Capture(_listBox);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || _draggedItem == null || _listBox == null) return;

        var point = e.GetPosition(_listBox);
        var visual = _listBox.InputHitTest(point) as Visual;
        
        while (visual != null && !(visual is ListBoxItem))
        {
            visual = visual.GetVisualParent() as Visual;
        }

        // Clear previous highlight
        if (_lastHighlightedItem != null && _lastHighlightedItem != _draggedListBoxItem)
        {
            _lastHighlightedItem.Opacity = 1.0;
            _lastHighlightedItem.RenderTransform = null;
        }

        if (visual is ListBoxItem targetItem && targetItem.DataContext is OrderableItem targetOrderItem)
        {
            if (targetOrderItem != _draggedItem)
            {
                var vm = DataContext as SettingsViewModel;
                if (vm != null)
                {
                    int targetIndex = vm.StatusBarOrderItems.IndexOf(targetOrderItem);
                    if (targetIndex != -1)
                    {
                        _dropTargetIndex = targetIndex;

                        // Visual hint on the target drop position
                        targetItem.Opacity = 0.7;
                        targetItem.RenderTransform = new ScaleTransform(0.98, 0.98);
                        _lastHighlightedItem = targetItem;
                    }
                }
            }
            else
            {
                // Hovering over the dragged item itself - no drop target highlight needed
                _lastHighlightedItem = null;
            }
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            e.Pointer.Capture(null);

            // Reset visual states
            if (_draggedListBoxItem != null)
            {
                _draggedListBoxItem.Opacity = 1.0;
                _draggedListBoxItem.RenderTransform = null;
                _draggedListBoxItem.Cursor = Cursor.Default;
            }
            if (_lastHighlightedItem != null)
            {
                _lastHighlightedItem.Opacity = 1.0;
                _lastHighlightedItem.RenderTransform = null;
            }

            // Perform the actual move on drop
            var vm = DataContext as SettingsViewModel;
            if (vm != null && _draggedItem != null)
            {
                int currentIndex = vm.StatusBarOrderItems.IndexOf(_draggedItem);
                if (currentIndex != -1 && _dropTargetIndex != -1 && currentIndex != _dropTargetIndex)
                {
                    vm.StatusBarOrderItems.Move(currentIndex, _dropTargetIndex);
                    vm.UpdateLivePreview();
                }
            }

            _isDragging = false;
            _draggedItem = null;
            _draggedListBoxItem = null;
            _lastHighlightedItem = null;
            _dragStartIndex = -1;
            _dropTargetIndex = -1;
        }
    }
}
