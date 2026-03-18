using DAL;
using Newtonsoft.Json;
using System;

namespace Models
{
    public class Notification : Record
    {
        public int TargetUserId { get; set; }
        public int SourceUserId { get; set; }
        public string Message { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        [JsonIgnore] public User User => DB.Users.Get(SourceUserId);
    }
}