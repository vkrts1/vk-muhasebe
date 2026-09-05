using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace ErmayMuhasebe.Avalonia.Views;

public partial class ButcePlanlamaView : UserControl
{
    public ButcePlanlamaView()
    {
        try
        {
            InitializeComponent();
        }
        catch (System.Exception ex)
        {
             // Fallback or silent fail to prevent whole app crash
             System.Diagnostics.Debug.WriteLine($"View Init Error: {ex.Message}");
        }
    }

    private void InitializeComponent()
    {
        try 
        {
            AvaloniaXamlLoader.Load(this);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"XAML Load Error: {ex.Message}");
            throw; 
        }
    }

}
