using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace QuickShare.Common
{
    public static class SecureKeyStorage
    {
        static readonly string _userId = "userId";
        static readonly string _accountId = "accountId8";
        static readonly string _token = "accountToken";

        public static bool IsUserIdStored()
        {
            return IsStored(_userId);
        }

        public static bool IsAccountIdStored()
        {
            return IsStored(_accountId);
        }

        public static bool IsTokenStored()
        {
            return IsStored(_token);
        }

        private static bool IsStored(string key)
        {
            var vault = new PasswordVault();
            var cred = vault.RetrieveAll().FirstOrDefault(x => x.Resource == key);

            if (cred == null)
                return false;

            return true;
        }

        public static string GetUserId()
        {
            return Get(_userId);
        }

        public static string GetAccountId()
        {
            return Get(_accountId);
        }

        public static string GetToken()
        {
            return Get(_token);
        }

        private static string Get(string key)
        {
            var vault = new PasswordVault();
            var cred = vault.RetrieveAll().FirstOrDefault(x => x.Resource == key);

            if (cred == null)
                return "";

            cred.RetrievePassword();
            return cred.Password;
        }

        public static void SetUserId(string value)
        {
            Set(_userId, value);
        }

        public static void SetAccountId(string value)
        {
            Set(_accountId, value);
        }

        public static void SetToken(string value)
        {
            Set(_token, value);
        }

        private static void Set(string key, string value)
        {
            var vault = new PasswordVault();
            var cred = new PasswordCredential(key, "user", value);
            vault.Add(cred);
        }

        public static void DeleteUserId()
        {
            Delete(_userId);
        }

        public static void DeleteAccountId()
        {
            Delete(_accountId);
        }

        public static void DeleteToken()
        {
            Delete(_token);
        }

        private static void Delete(string key)
        {
            var vault = new PasswordVault();
            vault.Remove(vault.RetrieveAll().FirstOrDefault(x => x.Resource == key));
        }
    }
}
