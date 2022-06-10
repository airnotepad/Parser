using AngleSharp.Dom;
using AngleSharp.Io;
using AngleSharp.Io.Cookie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    public class CookieProvider : ICookieProvider
    {
        private Dictionary<string, CookieContainer> cookies = new();
        private RWLock _lock = new RWLock();

        public string GetCookie(Url url)
        {
            using (_lock.ReadLock())
                if (cookies.ContainsKey(url.Origin))
                    return cookies[url.Origin].GetCookieHeader(new Uri(url.Origin));
                else return null;
        }

        public void SetCookie(Url url, string value)
        {
            string cookie;
            if (value.Split(';').Length > 0)
                cookie = value.Split(';')[0];
            else
                cookie = value;
            string name = cookie.Split('=')[0];
            string val = cookie.Split('=')[1];

            using (_lock.WriteLock())
                if (cookies.ContainsKey(url.Origin))
                    cookies[url.Origin].Add(new Uri(url.Origin), new Cookie(name, val));
                else
                {
                    var container = new CookieContainer();
                    container.Add(new Uri(url.Origin), new Cookie(name, val));
                    cookies.Add(url.Origin, container);
                }
        }
    }
}
