using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Tools1.WmiExplorer.Core.Mvvm
{
	public class SyncedObservableCollection<T> : ObservableCollection<T>
	{
		private SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;


		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (_synchronizationContext is null)
			{
				base.OnCollectionChanged(e);
			}
			else
			{
				_synchronizationContext.Send(x => base.OnCollectionChanged(e), null);
			}
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (_synchronizationContext is null)
			{
				base.OnPropertyChanged(e);
			}
			else
			{
				_synchronizationContext.Send(x => base.OnPropertyChanged(e), null);
			}
		}
	}
}
