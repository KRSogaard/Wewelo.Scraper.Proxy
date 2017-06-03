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
        }

        /// <summary>
        /// TODO
        /// </summary>
        public void RegisterBadProxy()
        {
            //TODO
        }

        /// <summary>
        /// Returns a list of all Proxies in JSON
        /// </summary>
        public List<JObject> GetAllProxies()
        {
            //remember, it should be JSON. Is IWebProxy JSON?
            List< JObject > proxyList = new List<JObject>();

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
                string[] split = line.Split(',');
                //proxyList.Add(new ProxyBonanzaProxy(split[0], split[1], split[2], split[3]));
                JObject proxyObject = proxyJObject(split[0], split[1], split[2], split[3]); 
                //add to proxyList
            }

            // we need to random the insert order so all instante will not use the same proxy at the same time.
            Random rnd = new Random();
            foreach (var proxy in proxyList.OrderBy(x => rnd.Next()))
            {
                AddProxy(proxy);
            }
        }
        //TODO
        private JObject proxyJObject(string v1, string v2, string v3, string v4)
        {
            throw new NotImplementedException();
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
