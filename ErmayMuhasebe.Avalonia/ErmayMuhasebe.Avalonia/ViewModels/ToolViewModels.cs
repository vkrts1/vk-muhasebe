using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class GenericToolViewModel : ViewModelBase 
{
    [ObservableProperty] private string _toolName;
    public GenericToolViewModel(string name) => ToolName = name;
}
