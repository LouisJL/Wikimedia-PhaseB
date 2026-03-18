using Newtonsoft.Json;
using System;

namespace DAL
{

    public class Record
    {
        public int Id { get; set; }
    }
    public static class JsonUtilities
    {

        public static T Copy<T>(this T source)
        {
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
        }
    }
}
