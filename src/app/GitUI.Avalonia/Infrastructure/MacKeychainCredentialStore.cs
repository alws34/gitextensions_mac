using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace GitUI.Avalonia.Infrastructure;

[SupportedOSPlatform("macos")]
public class MacKeychainCredentialStore : ICredentialStore
{
    private const string ServiceName = "GitExtensions";

    public bool TryGetCredential(string target, out string username, out string password)
    {
        username = string.Empty;
        password = string.Empty;

        byte[] serviceBytes = Encoding.UTF8.GetBytes(ServiceName);
        byte[] accountBytes = Encoding.UTF8.GetBytes(target);

        int result = NativeMethods.SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)serviceBytes.Length,
            serviceBytes,
            (uint)accountBytes.Length,
            accountBytes,
            out uint passwordLength,
            out IntPtr passwordData,
            out IntPtr itemRef);

        if (result != 0)
        {
            return false;
        }

        try
        {
            if (passwordData == IntPtr.Zero || passwordLength == 0)
            {
                return false;
            }

            string json = Marshal.PtrToStringUTF8(passwordData, (int)passwordLength)!;
            JsonDocument doc = JsonDocument.Parse(json);
            username = doc.RootElement.GetProperty("u").GetString() ?? string.Empty;
            password = doc.RootElement.GetProperty("p").GetString() ?? string.Empty;
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (passwordData != IntPtr.Zero)
            {
                NativeMethods.SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
            }
        }
    }

    public void SaveCredential(string target, string username, string password)
    {
        byte[] serviceBytes = Encoding.UTF8.GetBytes(ServiceName);
        byte[] accountBytes = Encoding.UTF8.GetBytes(target);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { u = username, p = password }));

        int findResult = NativeMethods.SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)serviceBytes.Length,
            serviceBytes,
            (uint)accountBytes.Length,
            accountBytes,
            out _,
            out IntPtr existingData,
            out IntPtr itemRef);

        if (existingData != IntPtr.Zero)
        {
            NativeMethods.SecKeychainItemFreeContent(IntPtr.Zero, existingData);
        }

        if (findResult == 0 && itemRef != IntPtr.Zero)
        {
            NativeMethods.SecKeychainItemModifyContent(itemRef, IntPtr.Zero, (uint)passwordBytes.Length, passwordBytes);
        }
        else
        {
            NativeMethods.SecKeychainAddGenericPassword(
                IntPtr.Zero,
                (uint)serviceBytes.Length,
                serviceBytes,
                (uint)accountBytes.Length,
                accountBytes,
                (uint)passwordBytes.Length,
                passwordBytes,
                out _);
        }
    }

    public void DeleteCredential(string target)
    {
        byte[] serviceBytes = Encoding.UTF8.GetBytes(ServiceName);
        byte[] accountBytes = Encoding.UTF8.GetBytes(target);

        int result = NativeMethods.SecKeychainFindGenericPassword(
            IntPtr.Zero,
            (uint)serviceBytes.Length,
            serviceBytes,
            (uint)accountBytes.Length,
            accountBytes,
            out _,
            out IntPtr passwordData,
            out IntPtr itemRef);

        if (passwordData != IntPtr.Zero)
        {
            NativeMethods.SecKeychainItemFreeContent(IntPtr.Zero, passwordData);
        }

        if (result == 0 && itemRef != IntPtr.Zero)
        {
            NativeMethods.SecKeychainItemDelete(itemRef);
        }
    }

    private static class NativeMethods
    {
        private const string SecurityFramework = "/System/Library/Frameworks/Security.framework/Security";

        [DllImport(SecurityFramework)]
        public static extern int SecKeychainFindGenericPassword(
            IntPtr keychainOrArray,
            uint serviceNameLength,
            byte[] serviceName,
            uint accountNameLength,
            byte[] accountName,
            out uint passwordLength,
            out IntPtr passwordData,
            out IntPtr itemRef);

        [DllImport(SecurityFramework)]
        public static extern int SecKeychainAddGenericPassword(
            IntPtr keychain,
            uint serviceNameLength,
            byte[] serviceName,
            uint accountNameLength,
            byte[] accountName,
            uint passwordLength,
            byte[] passwordData,
            out IntPtr itemRef);

        [DllImport(SecurityFramework)]
        public static extern int SecKeychainItemModifyContent(
            IntPtr itemRef,
            IntPtr attrList,
            uint length,
            byte[] data);

        [DllImport(SecurityFramework)]
        public static extern int SecKeychainItemDelete(IntPtr itemRef);

        [DllImport(SecurityFramework)]
        public static extern int SecKeychainItemFreeContent(IntPtr attrList, IntPtr data);
    }
}
