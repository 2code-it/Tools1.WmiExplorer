namespace Tools1.WmiExplorer.Core.Models.Messages
{
	public class NewLogEntryMessage
	{
		public NewLogEntryMessage(LogEntry logEntry)
		{
			LogEntry = logEntry;
		}

		public LogEntry LogEntry { get; private set; }

		public static NewLogEntryMessage CreateInfo(string message)
			=> new NewLogEntryMessage(LogEntry.CreateInfo(message));

		public static NewLogEntryMessage CreateWarning(string message)
			=> new NewLogEntryMessage(LogEntry.CreateWarning(message));

		public static NewLogEntryMessage CreateError(string message)
			=> new NewLogEntryMessage(LogEntry.CreateError(message));
	}
}
