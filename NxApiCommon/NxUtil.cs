using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace NxApiCommon
{
	class NxUtil
	{
		public static string getUUID()
		{
			ManagementClass mc = new ManagementClass("Win32_ComputerSystemProduct");
			ManagementObjectCollection moc = mc.GetInstances();
			foreach (ManagementObject ob in moc)
				return ob.Properties["UUID"].Value.ToString();
			return null;
		}


	}
}
