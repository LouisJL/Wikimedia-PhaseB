using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace PhotosManager.WebServices
{
    public class GeoLocation
    {
        [JsonIgnore]
        public const string ServiceUrl = "http://ip-api.com/json/";

        public string query;
        public string status;
        public string continent;
        public string continentCode;
        public string country;
        public string countryCode;
        public string region;
        public string regionName;
        public string city;
        public string district;
        public string zip;
        public double lat;
        public double lon;
        public string timezone;
        public int offset;
        public string currency;
        public string isp;
        public string org;
        public string asname;
        public bool mobile;
        public bool proxy;
        public bool hosting;

        public static GeoLocation Call(string IP_Address)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ServiceUrl + IP_Address);
            request.Method = "GET";
            try
            {
                WebResponse webResponse = request.GetResponse();
                using (Stream webStream = webResponse.GetResponseStream() ?? Stream.Null)
                using (StreamReader responseReader = new StreamReader(webStream))
                {
                    string response = responseReader.ReadToEnd();
                    return JsonConvert.DeserializeObject<GeoLocation>(response);
                }
            }
            catch (Exception) { /* todo */ }
            return null;
        }
    }

}