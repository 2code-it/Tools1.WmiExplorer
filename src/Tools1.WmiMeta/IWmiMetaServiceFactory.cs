using System.Security;

namespace Tools1.WmiMeta
{
	public interface IWmiMetaServiceFactory
	{
		IWmiMetaService Create(string hostName, bool enablePrivileges = false, string ? logonUserName = null, SecureString? logonUserPassword = null);
	}
}