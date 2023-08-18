using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Tools1.WmiExplorer.Core.ViewModels.Tests
{
	internal class TestPasswordBox
	{
		public SecureString SecurePassword { get; set; } = new SecureString();
	}
}
