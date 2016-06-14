using System;
using System.Net.Mail;

namespace CloudMemoryCache.Utils
{
    public static class AccountUtil
    {
        public static bool IsValidEmail(string email)
        {
            try
            {
                email = new MailAddress(email).Address;
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
