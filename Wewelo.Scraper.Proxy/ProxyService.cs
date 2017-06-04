using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Wewelo.Scraper.Web.Proxy;
using Newtonsoft.Json.Linq;
using NLog;

namespace Wewelo.Scraper.Proxy
{
    public class ProxyService
    {
        public int ProxyRetries { get; set; }
        public bool ForceProxies { get; set; }

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private ConcurrentQueue<JObject> proxies;
        private ConcurrentDictionary<JObject, int> badProxyCounter;

        /// <summary>
        /// Takes a csv file containing proxies, and saves them.
        /// </summary>
        public ProxyService(string path)
        {
            ForceProxies = false;
            ProxyRetries = 3;
            proxies = new ConcurrentQueue<JObject>();
            badProxyCounter = new ConcurrentDictionary<JObject, int>();
            Load(path);
        }

        /// <summary>
        /// TODO
        /// </summary>
        public void RegisterBadProxy()
        {
            //TODO
        }

        /// <summary>
        /// Returns a list of all Proxies as JObjects
        /// </summary>
        public List<JObject> GetAllProxies()
        {
            //remember, it should be JSON. Is IWebProxy JSON?
            List<JObject> proxyList = new List<JObject>();

            JObject proxy;
            while ((proxy = GetProxy()) != null)
            {
                proxyList.Add(proxy);
            }
            return proxyList;
        }

        /// <summary>
        /// Returns a proxy
        /// </summary>
        private JObject GetProxy()
        {
            JObject proxy;
            if (proxies == null ||
                !proxies.TryDequeue(out proxy))
            {
                return null;
            }
            return proxy;
        }

        private void Load(String path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            List<JObject> proxyList = new List<JObject>();
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                proxyList.Add(ProxyJObject(line.Split(',')));
            }

            // we need to randomize the insert order so all instante will not use the same proxy at the same time.
            Random rnd = new Random();
            foreach (var proxy in proxyList.OrderBy(x => rnd.Next()))
            {
                AddProxy(proxy);
            }
        }

        /// <summary>
        /// Create a new proxy from split arguments.
        /// </summary>
        private JObject ProxyJObject(string[] split)
        {
            JObject proxyObject = new JObject();
            JArray jarray = new JArray();
            string[] parameters = new string[] { "ip", "port", "username", "password" };
            for (int i = 0; i < split.Length; i++)
            {
                JObject pObject = new JObject();
                pObject.Add(parameters[i], split[i]);
                jarray.Add(pObject);
            }
            proxyObject.Add("proxies", jarray);
            return proxyObject;
        }

        public void AddProxy(JObject proxy, bool bad = false)
        {
            ForceProxies = true;
            if (bad)
            {
                if (!badProxyCounter.ContainsKey(proxy))
                {
                    badProxyCounter.TryAdd(proxy, 0);
                }

                badProxyCounter[proxy]++;
                if (badProxyCounter[proxy] >= ProxyRetries)
                {
                    logger.Warn("Proxy {0} have failed {1} times, removing. {2} proxies remaining.", proxy, badProxyCounter[proxy]);
                    return;
                }
                logger.Warn("Proxy {0} have failed {1} of {2} times, requeueing.", proxy, badProxyCounter[proxy], ProxyRetries);
            }

            proxies.Enqueue(proxy);
        }


    }
}
