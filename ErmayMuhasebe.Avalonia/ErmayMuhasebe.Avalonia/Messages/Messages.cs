using CommunityToolkit.Mvvm.Messaging.Messages;
using ErmayMuhasebe.Models;
using System;

namespace ErmayMuhasebe.Avalonia.Messages;

public class CekSavedMessage : ValueChangedMessage<Cek>
{
    public CekSavedMessage(Cek value) : base(value)
    {
    }
}

public class FinancialDataChangedMessage : MessageBase
{
}

public class NavigationRequestMessage : ValueChangedMessage<Type>
{
    public NavigationRequestMessage(Type viewModelType) : base(viewModelType)
    {
    }
}

public class NavigateViewModelMessage : ValueChangedMessage<ErmayMuhasebe.Shared.ViewModels.ViewModelBase>
{
    public NavigateViewModelMessage(ErmayMuhasebe.Shared.ViewModels.ViewModelBase viewModel) : base(viewModel)
    {
    }
}

public class ShowCariDetailMessage : ValueChangedMessage<int>
{
    public ShowCariDetailMessage(int cariId) : base(cariId)
    {
    }
}

public abstract class MessageBase { }
