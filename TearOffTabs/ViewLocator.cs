using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System;
using System.Diagnostics.CodeAnalysis;
using TearOffTabs.ViewModels;

namespace TearOffTabs
{
    /// <summary>
    /// Given a view model, returns the corresponding view if possible.
    /// Supports nested namespaces: e.g. ViewModels.Pages.HomePageViewModel → Views.Pages.HomePageView
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

            // ViewModels.Pages.HomePageViewModel → Views.Pages.HomePageView
            var name = param.GetType().FullName!
                .Replace("TearOffTabs.ViewModels", "TearOffTabs.Views", StringComparison.Ordinal)
                .Replace("ViewModel", "View", StringComparison.Ordinal);

            var type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }

            return new TextBlock { Text = "View not found: " + name };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
