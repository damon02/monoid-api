/*______________________________*
 *_________© Monoid INC_________*
 *_________Validator.cs_________*
 *______________________________*/

using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace backend_core
{
    public class Validator
    {
        const int minPasswordLength = 10;
        const int maxUserNameLength = 50;

        /// <summary>
        /// Validate email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public bool IsValidEmail(string email)
        {
            try
            {
                MailAddress validMail = new MailAddress(email);
                return validMail.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate username
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool IsValidUserName(string userName)
        {
            if (userName.Length > maxUserNameLength) return false;

            return true;
        }

        /// <summary>
        /// Validate password
        /// </summary>
        /// <param name="password"></param>
        /// <returns>List of errors if there are any</returns>
        public List<string> IsValidPassword(string password)
        {
            List<string> errors = new List<string>();
            bool hasUpperCase = false;
            bool hasLowerCase = false;
            bool hasDigit = false;
            bool hasSymbol = false;

            if (password.Length < minPasswordLength) { errors.Add("Password must be atleast 10 characters long"); return errors; }

            foreach(char c in password)
            {
                if (char.IsUpper(c)) hasUpperCase = true;
                else if (char.IsLower(c)) hasLowerCase = true;
                else if (char.IsNumber(c)) hasDigit = true;
                else if (char.IsSymbol(c) || c > 32 && c < 127) hasSymbol = true;
            }

            if (!hasUpperCase) errors.Add("Password requires an upper case character");
            if (!hasLowerCase) errors.Add("Password requires a lower case character");
            if (!hasDigit) errors.Add("Password requires a digit");
            if (!hasSymbol) errors.Add("Password requires a symbol");

            return errors;
        }

        public byte[] Salt(User user)
        {
            string u = user.UserName;
            string d = user.CreationDate.ToString();

            char[] uc = u.ToCharArray();
            char[] dc = d.ToCharArray();

            string salt = string.Empty;

            for(int i = 0; i < uc.Length; i++)
            {
                char c = uc[i];
                if (i % 2 == 0)
                {
                    salt += (c+i).ToString();
                }
                else
                {
                    if(dc.Length < i)
                    {
                        salt += dc[i].ToString();
                    }
                    else
                    {
                        salt += (c + 5).ToString();
                    }
                }
            }   
            
            return Encoding.ASCII.GetBytes(salt);
        }

        public string PasswordHasher(User user)
        {
            if (user == null) return null;

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: user.Password,
                salt: Salt(user),
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return hashed;
        }
    }
}
