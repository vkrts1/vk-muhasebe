using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using System;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public abstract partial class ViewModelBase : ErmayMuhasebe.Shared.ViewModels.ViewModelBase
{
    protected override async Task InvokeOnUIThreadAsync(Action action)
    {
        await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(action);
    }
}
