using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tools1.WmiExplorer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using NSubstitute;
using Tools1.WmiExplorer.Core.Models.Messages;
using Tools1.WmiExplorer.Core.Models;

namespace Tools1.WmiExplorer.Core.ViewModels.Tests
{
	[TestClass]
	public class LogViewModelTests
	{
		[TestMethod]
		public void When_NewLogMessage_Expect_EntriesIncrease()
		{
			IMessenger messenger = WeakReferenceMessenger.Default;
			LogViewModel logViewModel = new LogViewModel(messenger);

			messenger.Send(NewLogEntryMessage.CreateInfo(""));
			messenger.Cleanup();

			Assert.AreEqual(1, logViewModel.Entries.Count);
		}

		[TestMethod]
		[DataRow(10)]
		public void When_ParallelNewLogMessage_Expect_EntriesCount(int messageCount)
		{
			IMessenger messenger = WeakReferenceMessenger.Default;
			LogViewModel logViewModel = new LogViewModel(messenger);
			int[] items = Enumerable.Range(0, messageCount).ToArray();

			Parallel.ForEach(items, i => messenger.Send(NewLogEntryMessage.CreateInfo($"message {i}")));
			messenger.Cleanup();

			Assert.AreEqual(messageCount, logViewModel.Entries.Count);
		}

		[TestMethod]
		public void When_UsingInvalidOptions_Expect_DefaultOptionValue()
		{
			IMessenger messenger = Substitute.For<IMessenger>();
			LogViewModelOptions options = new LogViewModelOptions { MaxEntries = -1 };
			LogViewModel logViewModel2 = new LogViewModel(messenger);
			LogViewModel logViewModel = new LogViewModel(messenger, options);

			Assert.AreEqual(logViewModel2.Options.MaxEntries, logViewModel.Options.MaxEntries);
		}

		[TestMethod]
		public void When_ClearCommandExecuted_Expect_EmptyEntries()
		{
			IMessenger messenger = Substitute.For<IMessenger>();
			LogViewModel logViewModel = new LogViewModel(messenger);
			logViewModel.Entries.Add(LogEntry.CreateInfo(""));

			logViewModel.ClearCommand.Execute(null);

			Assert.AreEqual(0, logViewModel.Entries.Count);
		}

		[TestMethod]
		public void When_RemoveSelectedCommandExecuted_Expect_SelectedEntryRemovedAndNulled()
		{
			IMessenger messenger = Substitute.For<IMessenger>();
			LogViewModel logViewModel = new LogViewModel(messenger);
			LogEntry entry = LogEntry.CreateInfo("");
			logViewModel.Entries.Add(entry);
			logViewModel.SelectedEntry = entry;

			logViewModel.RemoveSelectedCommand.Execute(null);

			Assert.AreEqual(0, logViewModel.Entries.Count);
			Assert.IsNull(logViewModel.SelectedEntry);
		}
	}
}