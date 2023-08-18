namespace Tools1.WmiExplorer.Core.Models
{
	public class LogEntry
	{
		public const string SeverityError = "Error";
		public const string SeverityInfo = "Info";
		public const string SeverityWarning = "Warning";

		public DateTime Created { get; set; }
		public string? Severity { get; set; }
		public string? Message { get; set; }

		public static LogEntry CreateInfo(string message)
			=> new LogEntry { Created = DateTime.Now, Message = message, Severity = SeverityInfo };

		public static LogEntry CreateWarning(string message)
			=> new LogEntry { Created = DateTime.Now, Message = message, Severity = SeverityWarning };

		public static LogEntry CreateError(string message)
			=> new LogEntry { Created = DateTime.Now, Message = message, Severity = SeverityError };
	}
}
