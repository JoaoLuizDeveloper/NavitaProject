﻿using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NavitaAPI.Data;
using NavitaAPI.Models;
using NavitaAPI.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace NavitaAPI.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly AppSettings _appSettings;
        public UserRepository(ApplicationDbContext db, IOptions<AppSettings> appSettings)
        {
            _db = db;
            _appSettings = appSettings.Value;
        }

        public User Authenticate(string username,string email, string password)
        {
            var user = _db.Users.SingleOrDefault(x => x.UserName == username && x.Password == password);
            //User not Found
            if (user == null)
            {
                return null;
            }

            // User found and generate Jwt Token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescription = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescription);
            user.Token = tokenHandler.WriteToken(token);
            user.Password = "";
            return user;
        }

        public bool isUniqueUser(string email)
        {
            var user = _db.Users.SingleOrDefault(x => x.Email == email);

            //return true if user is unique
            if (user == null)
                return true;

            return false;
        }

        public User Register(string username, string email, string password)
        {
            User userObj = new User()
            {
                UserName = username,
                Email = email,
                Password = password,
                Role = "Admin",
            };

            _db.Users.Add(userObj);
            _db.SaveChanges();
            userObj.Password = "";

            return userObj;
        }
    }
}
