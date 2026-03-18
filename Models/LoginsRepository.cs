using DAL;
using Models;
using PhotosManager.WebServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotosManager.Models
{
    public class LoginsRepository : Repository<Login>
    {
        public int Add(int userId)
        {
            try
            {
                Login login = new Login();
                login.LoginDate = login.LogoutDate = DateTime.Now;
                login.UserId = userId;
                login.IpAddress = HttpContext.Current.Request.UserHostAddress;
                if (login.IpAddress != "::1")
                {
                    GeoLocation gl = GeoLocation.Call(login.IpAddress);
                    login.City = gl.city;
                    login.RegionName = gl.regionName;
                    login.CountryCode = gl.countryCode;
                }
                login.Id = DB.Logins.Add(login);
                return login.Id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddLogin failed : Message - {ex.Message}");
                return 0;
            }
        }
        public bool UpdateLogout(int loginId)
        {
            try
            {
                Login login = DB.Logins.Get(loginId);
                if (login != null)
                {
                    login.LogoutDate = DateTime.Now;
                    return DB.Logins.Update(login);
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateLogout failed : Message - {ex.Message}");
                return false;
            }
        }
        public bool UpdateLogoutByUserId(int userId)
        {
            try
            {
                Login login = DB.Logins.ToList().Where(l => l.UserId == userId).OrderByDescending(l => l.LoginDate).FirstOrDefault();
                if (login != null)
                {
                    login.LogoutDate = DateTime.Now;
                    return DB.Logins.Update(login);
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateLogoutByUserId failed : Message - {ex.Message}");
                return false;
            }
        }
        public bool DeleteLoginsJournalDay(DateTime day)
        {
            try
            {
                BeginTransaction();
                DateTime dayAfter = day.AddDays(1);
                List<Login> logins = DB.Logins.ToList().Where(l => l.LoginDate >= day && l.LoginDate < dayAfter).ToList().Copy();
                // Notice: You can delete items of List<T> collection in a foreach loop but it will fail with items of IEnumerable<T> collection
                foreach (Login login in logins)
                {
                    DB.Logins.Delete(login.Id);
                }
                EndTransaction();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteLoginsJournalDay failed : Message - {ex.Message}");
                EndTransaction();
                return false;
            }
        }
    }

}