using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NxApiCommon
{
	class NxUtil
	{
		public static string getUUID()
		{
			ManagementClass mc = new ManagementClass("Win32_ComputerSystemProduct");
			ManagementObjectCollection moc = mc.GetInstances();
            String uuid = "";
            StringBuilder uuidHash = new StringBuilder();
            foreach (ManagementObject ob in moc)
            {
                if (uuid == "")
                {
                    uuid = ob.Properties["UUID"].Value.ToString().Replace("-", " ");
                }
            }
            using(SHA256 sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.ASCII.GetBytes(uuid));
                foreach(byte thing in hash)
                {
                    uuidHash.Append(thing.ToString("x2"));
                }
            }
            return uuidHash.ToString().ToLower();
		}


	}
}
