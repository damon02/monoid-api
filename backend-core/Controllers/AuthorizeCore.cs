/*______________________________*
 *_________© Monoid INC_________*
 *_______AuthorizeCore.cs_______*
 *______________________________*/

using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        
        /// <summary> Register a new user </summary>
        public DataResult<User> RegisterUser(User user)
        {
            Validator validator = new Validator();

            user.CreationDate = DateTime.Now;

            string hashedPassword = validator.PasswordHasher(user);

            user.Password = hashedPassword;

            user.Activated = true;

            DataResult<User> dr = database.CreateUser(user);

            // Todo => send an email to the user before the account is activated
            

            return dr;
        }

        /// <summary> Authenticate a user and return a token that can be used to authorize the user </summary>
        public DataResult<User> Authenticate(User user, string appSecret)
        {
            Validator validator = new Validator();
            user.Password = validator.PasswordHasher(user);

            DataResult<User> dr = database.GetUser(user);

            if (!dr.Success) return dr;

            User authorizedUser = dr.Data.FirstOrDefault();

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            byte[] secretKey = Encoding.ASCII.GetBytes(appSecret);
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("userName", authorizedUser.UserName),
                    new Claim("id", authorizedUser.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddYears(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            authorizedUser.Token = tokenHandler.WriteToken(token);

            authorizedUser.Password = null;

            dr.Data = new List<User>() { authorizedUser };

            return dr;           
        }

        /// <summary> Generate a token for usage on the embedded system </summary>
        public string GenerateToken(User user)
        {
            DataResult<User> dr = database.GetUser(user);

            if (!dr.Success) return null;

            User nUser = dr.Data.FirstOrDefault();

            string token = Guid.NewGuid().ToString();

            nUser.Token = token;

            database.UpdateUser(nUser);

            return token;
        }

        /// <summary> Get the embedded system token </summary>
        public string GetToken(ObjectId uId)
        {
            DataResult<User> dr = database.GetUser(new User { Id = uId });

            if (!dr.Success) return null;

            return dr.Data.FirstOrDefault().Token;
        }

        /// <summary> Authorize an embedded system token </summary>
        public User AuthorizeToken(string token)
        {
            DataResult<User> dr = database.GetUser(new User { Token = token });

            if (!dr.Success) return null;

            return dr.Data.FirstOrDefault();
        }
    }
}