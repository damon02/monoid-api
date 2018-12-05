/*______________________________*
 *_________© Monoid INC_________*
 *_______AuthorizeCore.cs_______*
 *______________________________*/

using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace backend_core
{
    public sealed class AuthorizeCore : BaseCore
    {
        private static AuthorizeCore instance = null;
        private static readonly object padlock = new object();

        public static AuthorizeCore Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new AuthorizeCore();
                    }
                    return instance;
                }
            }
        }
        
        /// <summary>
        /// Register a user 
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public DataResult<User> RegisterUser(User user)
        {
            Validator validator = new Validator();

            user.CreationDate = DateTime.Now;

            string hashedPassword = validator.PasswordHasher(user);

            user.Password = hashedPassword;

            user.Activated = true;

            DataResult<User> dr = database.CreateUser(user);

            // Todo => sent an email to the user before the account is activated


            return dr;
        }

        public DataResult<User> Authenticate(User user, string appSecret)
        {
            Validator validator = new Validator();
            user.Password = validator.PasswordHasher(user);

            DataResult<User> dr = database.GetUser(user);

            if (!dr.Success) return dr;

            User authorizedUser = dr.Data.FirstOrDefault();

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            byte[] key = Encoding.ASCII.GetBytes(appSecret);
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("userName", authorizedUser.UserName),
                    new Claim("id", authorizedUser.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddYears(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);

            user.Password = null;

            dr.Data = new List<User>() { user };

            return dr;           
        }
    }
}
