using System.Reflection;

namespace Tools1.WmiExplorer.Core.Mvvm
{
	public class ViewModelLocator
	{
		private static IServiceProvider? _serviceProvider;

		public static void UseServiceProvider(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		public object? this[string viewModelTypeName]
		{
			get
			{
				return GetViewModelByTypeName(viewModelTypeName);
			}
		}

		private object? GetViewModelByTypeName(string typeName)
		{
			Type? type = Assembly.GetExecutingAssembly().ExportedTypes.Where(x => x.Name == typeName).FirstOrDefault();
			if (type is null) throw new InvalidOperationException($"ViewModel type '{typeName}' not found");
			if (_serviceProvider is null) return Activator.CreateInstance(type);
			return _serviceProvider.GetService(type);
		}
	}
}
