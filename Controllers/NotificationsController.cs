using Models;
using System.Web.Mvc;

namespace Controllers
{
    public class NotificationsController : Controller
    {
        public JsonResult Pop()
        {
            Notification notification = DAL.DB.Notifications.Pop();
            if (notification != null)
                return Json(new { notification.User.Avatar, notification.Message }, JsonRequestBehavior.AllowGet);
            return Json(null, JsonRequestBehavior.AllowGet);
        }
    }
}