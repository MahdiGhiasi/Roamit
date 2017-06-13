using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace QuickShare.HelperClasses
{
    internal static class SecureKeyStorage
    {
        internal static bool IsUserIdStored()
        {
            var vault = new PasswordVault();
            var cred = vault.RetrieveAll().FirstOrDefault(x => x.Resource == "userId");

            if (cred == null)
                return false;

            return true;
        }

        internal static string GetUserId()
        {
            var vault = new PasswordVault();
            var cred = vault.RetrieveAll().FirstOrDefault(x => x.Resource == "userId");

            if (cred == null)
                return "";

            cred.RetrievePassword();
            return cred.Password;
        }

        internal static void SetUserId(string userId)
        {
            var vault = new PasswordVault();
            var cred = new PasswordCredential("userId", "user", userId);
            vault.Add(cred);
        }

        internal static void DeleteUserId()
        {
            var vault = new PasswordVault();
            vault.Remove(vault.RetrieveAll().FirstOrDefault(x => x.Resource == "userId"));
        }
    }
}
