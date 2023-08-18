using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Tools1.WmiExplorer.Core.Models;
using Tools1.WmiExplorer.Core.Models.Messages;
using Tools1.WmiExplorer.Core.Mvvm;

namespace Tools1.WmiExplorer.Core.ViewModels
{
	public partial class LogViewModel : ObservableObject
	{
		public LogViewModel(IMessenger messenger) :this(messenger, null) { }
		public LogViewModel(IMessenger messenger, LogViewModelOptions? options)
		{
			_messenger = messenger;
			_messenger.Register<NewLogEntryMessage>(this, (r, m) => OnNewLogEntry(m));
			if (options is not null && IsValid(options)) Options = options;
		}

		private readonly IMessenger _messenger;
		private static readonly object _lock = new();

		public LogViewModelOptions Options { get; private set; } = new LogViewModelOptions() { MaxEntries = 300 };
		public SyncedObservableCollection<LogEntry> Entries { get; private set; } = new SyncedObservableCollection<LogEntry>();

		[ObservableProperty]
		private LogEntry? _selectedEntry;

		[RelayCommand]
		private void RemoveSelected()
		{
			if (SelectedEntry is null) return;
			lock (_lock)
			{
				Entries.Remove(SelectedEntry);
				SelectedEntry = null;
			}
		}

		[RelayCommand]
		private void Clear()
		{
			lock (_lock)
			{
				Entries.Clear();
			}
		}

		protected virtual void OnNewLogEntry(NewLogEntryMessage message)
		{
			lock (_lock)
			{
				Entries.Insert(0, message.LogEntry);
				if (Entries.Count > Options.MaxEntries) Entries.Remove(Entries.Last());
			}
		}

		protected virtual bool IsValid(LogViewModelOptions options)
		{
			return options.MaxEntries > 0;
		}
	}
}
