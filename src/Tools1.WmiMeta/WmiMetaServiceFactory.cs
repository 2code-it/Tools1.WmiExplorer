using System.Security;

namespace Tools1.WmiMeta
{
	public class WmiMetaServiceFactory : IWmiMetaServiceFactory
	{
		public IWmiMetaService Create(string hostName, bool enablePrivileges = false, string ? logonUserName = null, SecureString? logonUserPassword = null)
		{
			return new WmiMetaService(hostName, enablePrivileges, logonUserName, logonUserPassword);
		}
	}
}
