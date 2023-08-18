using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tools1.WmiExplorer.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools1.WmiMeta;
using NSubstitute;
using CommunityToolkit.Mvvm.Messaging;
using NSubstitute.ExceptionExtensions;
using Tools1.WmiExplorer.Core.Models.Messages;
using System.Security;
using System.Net.Http.Headers;

namespace Tools1.WmiExplorer.Core.ViewModels.Tests
{
	[TestClass]
	public class MainWindowViewModelTests
	{
		const string _labelNamespaces = "namespaces";
		const string _labelClasses = "classes";
		const string _labelProperties = "properties";

		[TestMethod]
		public void When_OnLoad_Expect_NamespacesLoaded()
		{
			int objectCount = 6;
			IMessenger messenger = Substitute.For<IMessenger>();
			IWmiMetaServiceFactory metaFactory = GetMetaServiceFactoryWithObjects(_labelNamespaces, objectCount);
			IWmiMetaService metaService = metaFactory.Create("");

			MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(metaFactory, messenger);

			WaitForLoadingViewModel(mainWindowViewModel, 100);
			metaService.Received(objectCount).GetNamespaces(Arg.Any<string>());
			Assert.AreEqual(objectCount, mainWindowViewModel.Namespaces.Length);
		}

		[TestMethod]
		public void When_WmiError_Expect_NewLogMessage()
		{
			string exceptionMessage = "exception message";
			IMessenger messenger = WeakReferenceMessenger.Default;
			IWmiMetaServiceFactory factory = GetMetaServiceFactoryWithObjects(_labelNamespaces, 1);
			IWmiMetaService metaService = factory.Create("");
			MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(factory, messenger);
			LogViewModel logViewModel = new LogViewModel(messenger);

			metaService.Error += Raise.EventWith(new UnhandledExceptionEventArgs(new Exception(exceptionMessage), false));

			Assert.AreEqual(1, logViewModel.Entries.Count);
		}

		[TestMethod]
		public void When_NameSpacesIsFiltered_Expect_FilteredNameSpaces()
		{
			IMessenger messenger = Substitute.For<IMessenger>();
			IWmiMetaServiceFactory factory = GetMetaServiceFactoryWithObjects(_labelNamespaces, 10);
			MainWindowViewModel mainWindowViewModel = new MainWindowViewModel(factory, messenger);
			WaitForLoadingViewModel(mainWindowViewModel, 100);

			mainWindowViewModel.NamespacesFilter = "1";

			Thread.Sleep(10);
			Assert.AreEqual(1, mainWindowViewModel.Namespaces.Length);
		}

		[TestMethod]
		public void When_ClassesIsFiltered_Expect_FilteredClasses()
		{
			IMessenger messenger = Substitute.For<IMessenger>();
			IWmiMetaServiceFactory factory = GetMetaServiceFactoryWithObjects(_labelClasses, 10);
			MainWindowViewModel vm = new MainWindowViewModel(factory, messenger);
			WaitForLoadingViewModel(vm, 100);
			vm.SelectedNamespace = vm.Namespaces[0];
			WaitForLoadingViewModel(vm, 100);

			vm.ClassesFilter = "1";

			Thread.Sleep(10);
			Assert.AreEqual(1, vm.Classes.Length);
		}

		[TestMethod]
		public void When_NamespaceIsSelected_Expect_ClassesLoaded()
		{
			IMessenger messenger = Substitute.For<IMessenger>();
			IWmiMetaServiceFactory factory = GetMetaServiceFactoryWithObjects(_labelClasses, 10);
			MainWindowViewModel vm = new MainWindowViewModel(factory, messenger);
			WaitForLoadingViewModel(vm, 100);
			
			vm.SelectedNamespace = vm.Namespaces[0];
			WaitForLoadingViewModel(vm, 100);

			Assert.AreEqual(10, vm.Classes.Length);
		}

		[TestMethod]
		public void When_ClassIsSelected_Expect_PropertiesLoaded()
		{
			IMessenger messenger = Substitute.For<IMessenger>();
			IWmiMetaServiceFactory factory = GetMetaServiceFactoryWithObjects(_labelProperties, 10);
			MainWindowViewModel vm = new MainWindowViewModel(factory, messenger);
			WaitForLoadingViewModel(vm, 100);

			vm.SelectedNamespace = vm.Namespaces[0];
			WaitForLoadingViewModel(vm, 100);
			vm.SelectedClass = vm.Classes[0];
			Thread.Sleep(10);

			Assert.AreEqual(10, vm.Properties.Length);
		}

		[TestMethod]
		public void When_UpdateCommandExecutes_Expect_UpdatedWmiMetaService()
		{
			IWmiMetaServiceFactory factory = GetMetaServiceFactoryWithObjects(_labelNamespaces, 1);
			IMessenger messenger = Substitute.For<IMessenger>();
			TestPasswordBox passwordBox = new TestPasswordBox();
			SecureString secureString = new SecureString();
			secureString.AppendChar('1');
			secureString.AppendChar('2');
			secureString.AppendChar('3');
			passwordBox.SecurePassword = secureString;
			MainWindowViewModel vm = new MainWindowViewModel(factory, messenger);
			vm.HostName = "host1";
			vm.LogonUserName = "user1";
			vm.EnablePrivileges = true;

			string? newHost = null;
			bool newPrivileges = false;
			string? newUser = null;
			SecureString? newSecureString = null;
			
			factory.Create(
				Arg.Do<string>(x => newHost = x),
				Arg.Do<bool>(x => newPrivileges = x),
				Arg.Do<string?>(x => newUser = x),
				Arg.Do<SecureString?>(x => newSecureString = x)
			);

			vm.UpdateCommand.Execute(passwordBox);

			Assert.AreEqual(vm.HostName, newHost);
			Assert.AreEqual(vm.EnablePrivileges, newPrivileges);
			Assert.AreEqual(vm.LogonUserName, newUser);
			Assert.AreEqual(secureString, newSecureString);

		}


		private void WaitForLoadingViewModel(MainWindowViewModel vm,  int maxMs)
		{
			int current = 0;
			while(vm.IsLoading && current< maxMs) 
			{
				Thread.Sleep(10);
				current+=10;
			}
		}

		private IWmiMetaServiceFactory GetMetaServiceFactoryWithObjects(string target, int objectCount)
		{
			IWmiMetaService metaService = Substitute.For<IWmiMetaService>();
			IWmiMetaServiceFactory metaFactory = Substitute.For<IWmiMetaServiceFactory>();
			metaFactory.Create(Arg.Any<string>()).Returns(metaService);

			WmiMetaObject[] namespaces = Enumerable.Range(0, objectCount).Select(x => new WmiMetaObject { Name = $"{_labelNamespaces}{x}" }).ToArray();
			metaService.GetRootNamespaces().Returns(namespaces);
			if(target == _labelNamespaces) return metaFactory;

			WmiMetaObject[] classes = Enumerable.Range(0, objectCount).Select(x => new WmiMetaObject { Name = $"{_labelClasses}{x}" }).ToArray();
			metaService.GetClasses(Arg.Any<string>()).Returns(classes);
			if (target == _labelClasses) return metaFactory;

			WmiMetaObject[] properties = Enumerable.Range(0, objectCount).Select(x => new WmiMetaObject { Name = $"{_labelProperties}{x}" }).ToArray();
			metaService.GetProperties(Arg.Any<string>(), Arg.Any<string>()).Returns(properties);
			return metaFactory;
		}
	}
}