using System.Security;

namespace Tools1.WmiMeta
{
	public interface IWmiMetaService
	{
		string HostName { get; set; }
		string? LogonUserName { get; set; }
		SecureString? LogonUserPassword { get; set; }
		bool EnablePrivileges { get; set; }

		event EventHandler<UnhandledExceptionEventArgs>? Error;

		WmiMetaObject[] GetNamespaces(string ns);
		WmiMetaObject[] GetRootNamespaces();
		WmiMetaObject[] GetClasses(string ns);
		WmiMetaObject[] GetProperties(string ns, string className);
	}
}