using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using Tools1.WmiExplorer.Core.Models;
using Tools1.WmiExplorer.Core.Mvvm;
using Tools1.WmiExplorer.Core.ViewModels;
using Tools1.WmiMeta;

namespace Tools1.WmiExplorer.ExplorerApp
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			ConfigureServices();

			Views.MainWindow mainWindow = new Views.MainWindow();
			mainWindow.Show();

			base.OnStartup(e);
		}

		private void ConfigureServices()
		{
			IServiceCollection services = new ServiceCollection();

			LogViewModelOptions logViewModelOptions = new LogViewModelOptions() { MaxEntries = 100 };
			services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
			services.AddSingleton<IWmiMetaServiceFactory, WmiMetaServiceFactory>();
			services.AddSingleton(logViewModelOptions);

			services.AddTransient<MainWindowViewModel>();
			services.AddTransient<LogViewModel>();

			IServiceProvider serviceProvider = services.BuildServiceProvider();
			ViewModelLocator.UseServiceProvider(serviceProvider);
		}
	}
}
