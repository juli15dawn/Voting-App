﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VotingApp.Services.Models
{
    public class ApplicationUserDTO
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailConfirmed { get; set; }
        public string PhoneNumberConfirmed { get; set; }
        public string Password { get; set; }
        public string PasswordConfirmed { get; set; }
    }
}