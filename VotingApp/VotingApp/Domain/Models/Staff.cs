﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VotingApp.Domain.Models
{
    public class Staff: ApplicationUser
    {
        
        public string FirstName { get; set; }
        public string LastName { get; set; }

    }
}