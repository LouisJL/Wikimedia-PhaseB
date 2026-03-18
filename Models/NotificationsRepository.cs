using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models
{
    public class NotificationsRepository : Repository<Notification>
    {
        public void Push(int targetUserId, string Message)
        {
            User targetUser = DB.Users.Get(targetUserId); 
            if (User.ConnectedUser != null && targetUser.Notify)
                Add(new Notification { TargetUserId = targetUserId, SourceUserId = User.ConnectedUser.Id, Message = Message });
        }
        public Notification Pop()
        {
            if (User.ConnectedUser != null)
            {
                Notification notification = ToList().Where(n => n.TargetUserId == User.ConnectedUser.Id).FirstOrDefault()?.Copy();
                if (notification != null)
                {
                    Delete(notification.Id);
                    return notification;
                }
            }
            return null;
        }
    }
}