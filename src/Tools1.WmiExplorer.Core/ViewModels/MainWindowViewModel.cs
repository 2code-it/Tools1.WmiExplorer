using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Data;
using System.Security;
using Tools1.WmiExplorer.Core.Models;
using Tools1.WmiExplorer.Core.Models.Messages;
using Tools1.WmiMeta;

namespace Tools1.WmiExplorer.Core.ViewModels
{
	public partial class MainWindowViewModel : ObservableObject
	{
		public MainWindowViewModel(IWmiMetaServiceFactory wmiMetaServiceFactory, IMessenger messenger)
		{
			_wmiMetaServiceFactory = wmiMetaServiceFactory;
			_messenger = messenger;
			OnLoad();
		}

		private readonly IWmiMetaServiceFactory _wmiMetaServiceFactory;
		private readonly IMessenger _messenger;
		private IWmiMetaService _wmiMetaService = default!;
		private CancellationTokenSource? _ctsLoadNamespaces;
		private CancellationTokenSource? _ctsLoadClasses;
		private CancellationTokenSource? _ctsLoadProperties;
		private CancellationTokenSource? _ctsFilter;

		private WmiMetaObject[] _allNamespaces = new WmiMetaObject[0];
		private WmiMetaObject[] _allClasses = new WmiMetaObject[0];
		private WmiMetaObject[] _allProperties = new WmiMetaObject[0];

		[ObservableProperty]
		private WmiMetaObject[] _namespaces = new WmiMetaObject[0];
		[ObservableProperty]
		private WmiMetaObject[] _classes = new WmiMetaObject[0];
		[ObservableProperty]
		private WmiMetaObject[] _properties = new WmiMetaObject[0];
		[ObservableProperty]
		private bool _isLoading;
		[ObservableProperty]
		private WmiMetaObject? _selectedNamespace;
		[ObservableProperty]
		private WmiMetaObject? _selectedClass;
		[ObservableProperty]
		private WmiMetaObject? _selectedProperty;
		[ObservableProperty]
		private string? _namespacesFilter;
		[ObservableProperty]
		private string? _classesFilter;
		[ObservableProperty]
		private string? _propertiesFilter;
		[ObservableProperty]
		private string _hostName = "localhost";
		[ObservableProperty]
		private string? _logonUserName;
		[ObservableProperty]
		private bool _enablePrivileges;


		private async Task LoadNamespacesAsync()
		{
			IsLoading = true;
			_ctsLoadNamespaces = ResetCancellationTokenSource(_ctsLoadNamespaces);
			List<WmiMetaObject> list = new List<WmiMetaObject>();
			await Task.Run(() =>
			{
				ResetFor(ResetType.All);
				LoadNamespacesRecursive(null, list);
			}, _ctsLoadNamespaces.Token);
			_allNamespaces = list.ToArray();
			Namespaces = _allNamespaces;
			IsLoading = false;
		}

		private void LoadNamespacesRecursive(WmiMetaObject? parent, List<WmiMetaObject> list)
		{
			WmiMetaObject[] metaObjects = parent is null ?
				_wmiMetaService.GetRootNamespaces() :
				_wmiMetaService.GetNamespaces(parent.Name);

			list.AddRange(metaObjects);

			foreach (WmiMetaObject metaObject in metaObjects)
			{
				LoadNamespacesRecursive(metaObject, list);
			}
		}

		private async Task LoadClassesAsync(string ns)
		{
			IsLoading = true;
			_ctsLoadClasses = ResetCancellationTokenSource(_ctsLoadClasses);
			WmiMetaObject[] classObjects = new WmiMetaObject[0];
			await Task.Run(() =>
			{
				ResetFor(ResetType.Classes);
				classObjects = _wmiMetaService.GetClasses(ns);

			}, _ctsLoadClasses.Token);
			_allClasses = classObjects.OrderBy(x => x.Name).ToArray();
			Classes = _allClasses;
			IsLoading = false;
		}

		private async Task LoadPropertiesAsync(string ns, string className)
		{
			_ctsLoadProperties = ResetCancellationTokenSource(_ctsLoadProperties);
			WmiMetaObject[] classObjects = new WmiMetaObject[0];
			await Task.Run(() =>
			{
				ResetFor(ResetType.Properties);
				classObjects = _wmiMetaService.GetProperties(ns, className);
			}, _ctsLoadProperties.Token);
			_allProperties = classObjects.OrderBy(x => x.Name).ToArray();
			Properties = _allProperties;
		}

		[RelayCommand]
		private void Update(object parameter)
		{
			SecureString? ssp = (SecureString?)parameter.GetType().GetProperty("SecurePassword")?.GetValue(parameter);

			NewWmiMetaService(HostName, EnablePrivileges, LogonUserName, ssp);
			LoadNamespacesAsync().Wait(0);
		}

		partial void OnSelectedNamespaceChanged(WmiMetaObject? value)
		{
			if (value is null) return;
			LoadClassesAsync(value.Name).Wait(0);
		}

		partial void OnSelectedClassChanged(WmiMetaObject? value)
		{
			if (value is null) return;
			LoadPropertiesAsync(SelectedNamespace!.Name, value.Name).Wait(0);
		}

		partial void OnNamespacesFilterChanged(string? value) => ApplyFilter(_allNamespaces, value, (x) => Namespaces = x).Wait(0);
		partial void OnClassesFilterChanged(string? value) => ApplyFilter(_allClasses, value, (x) => Classes = x).Wait(0);
		partial void OnPropertiesFilterChanged(string? value) => ApplyFilter(_allProperties, value, (x) => Properties = x).Wait(0);

		private async Task ApplyFilter(WmiMetaObject[] allObjects, string? filter, Action<WmiMetaObject[]> setter)
		{
			_ctsFilter = ResetCancellationTokenSource(_ctsFilter);
			await Task.Run(() =>
			{
				WmiMetaObject[] newArray = string.IsNullOrEmpty(filter) ?
					allObjects :
					allObjects.Where(x => x.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase)).ToArray();
				setter.Invoke(newArray);
			}, _ctsFilter.Token);
		}

		private void NewWmiMetaService(string hostName, bool enablePrivileges = false, string ? logonUserName = null, SecureString? logonUserPassword = null)
		{
			_wmiMetaService = _wmiMetaServiceFactory.Create(hostName, enablePrivileges, logonUserName, logonUserPassword);
			_wmiMetaService.Error += wmiMetaService_Error;
		}

		private void wmiMetaService_Error(object? sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = (Exception)e.ExceptionObject;
			string message = ex.Message;
			if (ex.InnerException is not null)
			{
				message = $"{message} -> {ex.InnerException.Message}";
			}
			_messenger.Send(NewLogEntryMessage.CreateError(message));
		}

		protected virtual void OnLoad()
		{
			NewWmiMetaService(HostName);
			LoadNamespacesAsync().Wait(0);
		}

		private CancellationTokenSource ResetCancellationTokenSource(CancellationTokenSource? cts)
		{
			if (cts is not null)
			{
				cts.Cancel();
				cts.Dispose();
			}
			return new CancellationTokenSource();
		}

		private void ResetFor(ResetType resetType)
		{
			SelectedProperty = null;
			PropertiesFilter = null;
			_allProperties = Properties = new WmiMetaObject[0];

			if (resetType == ResetType.Properties) return;

			SelectedClass = null;
			ClassesFilter = null;
			_allClasses = Classes = new WmiMetaObject[0];

			if (resetType == ResetType.Classes) return;

			SelectedNamespace = null;
			NamespacesFilter = null;
			_allNamespaces = Namespaces = new WmiMetaObject[0];
		}

		private enum ResetType
		{
			All = 0,
			Classes = 1,
			Properties = 2
		}

	}
}
