using System;
using System.Runtime.InteropServices;

namespace SDDLConvertDomainConsole
{

    public static class RPCLookup
    {
        // Импорт функции DsBind из Ntdsapi.dll
        [DllImport("Ntdsapi.dll", CharSet = CharSet.Unicode)]
        public static extern int DsBind(
            string DomainControllerName,
            string DnsDomainName,
            out IntPtr phDS);

        // Импорт функции DsUnBind из Ntdsapi.dll
        [DllImport("Ntdsapi.dll", CharSet = CharSet.Unicode)]
        public static extern int DsUnBind(ref IntPtr phDS);

        // Импорт функции DsCrackNames из Ntdsapi.dll
        [DllImport("Ntdsapi.dll", CharSet = CharSet.Unicode)]
        public static extern int DsCrackNames(
            IntPtr hDS,
            DS_NAME_FLAGS flags,
            DS_NAME_FORMAT formatOffered,
            DS_NAME_FORMAT formatDesired,
            uint cNames,
            [In] string [] rpNames,
            out IntPtr ppResult);

        // Structures and Enums used for DsCrackNames
        [Flags]
        public enum DS_NAME_FLAGS : uint
        {
            DS_NAME_NO_FLAGS = 0x0
        }

        public enum DS_NAME_FORMAT : uint
        {
            DS_UNKNOWN_NAME = 0,
            DS_FQDN_1779_NAME = 1,
            DS_NT4_ACCOUNT_NAME = 2,
            DS_DISPLAY_NAME = 3,
            DS_UNIQUE_ID_NAME = 6,
            DS_CANONICAL_NAME = 7,
            DS_USER_PRINCIPAL_NAME = 8,
            DS_SCHEMA_GUID = 9,
            DS_SID_OR_SID_HISTORY_NAME = 10,
            DS_DNS_DOMAIN_NAME = 12
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DS_NAME_RESULT_ITEM
        {
            public int status;
            public string pDomain;
            public string pName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DS_NAME_RESULT
        {
            public uint cItems;
            public IntPtr rItems; // DS_NAME_RESULT_ITEM*
        }

        // Importing DsFreeNameResult function to free memory
        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
        public static extern void DsFreeNameResult(IntPtr pResult);

        public static string Lookup(string guidString = "05c74c5e-4deb-43b4-bd9f-86664c2a7fd5")
        {

            string result = "";
            IntPtr hDS;
            int bindResult = DsBind(null, null, out hDS);

            if (bindResult != 0)
            {
                Console.WriteLine("Failed to bind to the domain controller. Error code: " + bindResult);
                return result;
            }

            try
            {
                string [] guids = new string [] { guidString };

                IntPtr pResult;
                int crackResult = DsCrackNames(
                    hDS,
                    DS_NAME_FLAGS.DS_NAME_NO_FLAGS,
                    DS_NAME_FORMAT.DS_SCHEMA_GUID,
                    DS_NAME_FORMAT.DS_FQDN_1779_NAME,
                    (uint)guids.Length,
                    guids,
                    out pResult);

                if (crackResult != 0)
                {
                    Console.WriteLine("DsCrackNames failed with error code: " + crackResult);
                    return result;
                }

                // Marshal the result
                DS_NAME_RESULT nameResult = (DS_NAME_RESULT)Marshal.PtrToStructure(pResult, typeof(DS_NAME_RESULT));
                DS_NAME_RESULT_ITEM [] resultItems = new DS_NAME_RESULT_ITEM [nameResult.cItems];

                IntPtr itemPtr = nameResult.rItems;
                int itemSize = Marshal.SizeOf(typeof(DS_NAME_RESULT_ITEM));

                for (int i = 0; i < nameResult.cItems; i++)
                {
                    DS_NAME_RESULT_ITEM item = (DS_NAME_RESULT_ITEM)Marshal.PtrToStructure(itemPtr, typeof(DS_NAME_RESULT_ITEM));
                    resultItems [i] = item;

                    Console.WriteLine($"Status: {item.status}");
                    if (item.status == 0) // 0 means success
                    {
                        Console.WriteLine($"Domain: {item.pDomain}");
                        Console.WriteLine($"Name: {item.pName}");
                        result = item.pName;
                    }
                    else
                    {
                        Console.WriteLine("Could not resolve the GUID.");
                    }

                    itemPtr = IntPtr.Add(itemPtr, itemSize);
                }

                // Free the result
                DsFreeNameResult(pResult);
            }
            finally
            {
                DsUnBind(ref hDS);
            }
            return result;
        }
    }
}
