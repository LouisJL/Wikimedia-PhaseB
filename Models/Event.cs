using DAL;
using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models
{
    public class Event : Record
    {
        public DateTime CreationDate { get; set; }
        public string Comment { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; }
        [JsonIgnore] public User User => DB.Users.Get(UserId);
           
    }
}