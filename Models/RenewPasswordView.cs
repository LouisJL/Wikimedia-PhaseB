using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Models
{
    public class RenewPasswordView
    {
        public String Code { get; set; }
        public String Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}