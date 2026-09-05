using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ErmayMuhasebe.Avalonia.ViewModels;

namespace ErmayMuhasebe.Avalonia;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var typeName = param.GetType().Name;
        var viewName = typeName.Replace("ViewModel", "View");
        
        // Try precise name in Avalonia.Views namespace
        var asm = typeof(ViewLocator).Assembly;
        var type = asm.GetType($"ErmayMuhasebe.Avalonia.Views.{viewName}") 
                   ?? asm.GetType($"ErmayMuhasebe.Avalonia.Views.{typeName.Replace("ViewModel", "Window")}")
                   ?? Type.GetType($"ErmayMuhasebe.Avalonia.Views.{viewName}, {asm.FullName}");

        if (type == null)
        {
            // Fallback to basic reflection replace
            var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            type = Type.GetType(name) ?? asm.GetType(name);
        }

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        // Fallback for tool modules that don't have a specific view implementation yet
        if (param.GetType().Name.EndsWith("ViewModel") && param.GetType().Namespace!.Contains("ViewModels"))
        {
            var genericView = new Views.GenericToolModuleView();
            genericView.FindControl<TextBlock>("TitleText")!.Text = param.GetType().Name.Replace("ViewModel", "");
            return genericView;
        }

        return new TextBlock { Text = "Not Found: " + viewName };
    }

    public bool Match(object? data)
    {
        return data is ErmayMuhasebe.Shared.ViewModels.ViewModelBase;
    }
}
