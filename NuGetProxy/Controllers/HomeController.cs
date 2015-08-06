﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace NuGetProxy.Controllers
{
    public class HomeController : Controller
    {

        // GET: Home
        public async Task<ActionResult> Index(string all)
        {

            using (HttpClient client = new HttpClient())
            {

                bool isSecure = Request.IsSecureConnection;


                var msg = BuildRequest();
                
                //if(isSecure){
                //    builder.Scheme = "https";
                //}

                var r = await client.SendAsync(msg);

                return await HttpResponseActionResult.New(r);
            }
        }

        HttpRequestMessage BuildRequest() {
            UriBuilder builder = new UriBuilder(Request.Url);
            builder.Host = "www.nuget.org";
            builder.Scheme = "https";
            builder.Port = 143;
            var msg = new HttpRequestMessage(GetMethod(Request.HttpMethod), builder.Uri);

            // transfer all headers 
            foreach (string item in Request.Headers.Keys)
            {
                string value = Request.Headers[item];
                msg.Headers.Add(item, value);
            }


            if (Request.ContentLength > 0)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Request.InputStream.CopyTo(ms);

                    msg.Content = new ByteArrayContent(ms.ToArray());
                }
            }

            return msg;
        }

        private HttpMethod GetMethod(string p)
        {
            if (string.IsNullOrWhiteSpace(p))
                return HttpMethod.Get;
            p = p.ToLower();
            switch (p)
            {
                case "post":
                    return HttpMethod.Post;
                case "put":
                    return HttpMethod.Put;
                case "options":
                    return HttpMethod.Options;
                case "delete":
                    return HttpMethod.Delete;
                case "head":
                    return HttpMethod.Head;
                case "trace":
                    return HttpMethod.Trace;
                default:
                    break;
            }
            return HttpMethod.Get;
        }

        private void SetCache(HttpCachePolicyBase cache, System.Net.Http.Headers.CacheControlHeaderValue cacheIn)
        {
            if (cacheIn.Public)
            {
                cache.SetCacheability(HttpCacheability.Public);
            }
            if (cacheIn.Private)
            {
                cache.SetCacheability(HttpCacheability.Private);
            }
            if (cacheIn.MaxAge != null)
            {
                cache.SetMaxAge(cacheIn.MaxAge.Value);
            }

        }
    }

    public class HttpResponseActionResult : ActionResult
    {

        public static async Task<HttpResponseActionResult> New(HttpResponseMessage msg)
        {
            var s = await msg.Content.ReadAsByteArrayAsync();
            return new HttpResponseActionResult
            {
                Content = s,
                ResponseMessage = msg
            };
        }

        public HttpResponseMessage ResponseMessage { get; set; }

        public byte[] Content { get; set; }

        public override void ExecuteResult(ControllerContext context)
        {
            var Response = context.HttpContext.Response;

            Response.StatusCode = (int)ResponseMessage.StatusCode;
            Response.StatusDescription = ResponseMessage.ReasonPhrase;
            Response.TrySkipIisCustomErrors = true;

            if (Response.StatusCode == 200)
            {
                SetCache(Response.Cache, ResponseMessage.Headers.CacheControl);
            }
            else
            {
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
            }

            var content = ResponseMessage.Content;
            var val = content.Headers.Expires;
            if (val != null)
            {
                Response.ExpiresAbsolute = val.Value.UtcDateTime;
            }

            Response.ContentType = content.Headers.ContentType.ToString();

            if (ResponseMessage.Headers.Location != null)
            {
                Response.RedirectLocation = ResponseMessage.Headers.Location.ToString();
            }
            else
            {
                if (Content != null)
                {
                    Response.OutputStream.Write(Content, 0, Content.Length);
                }
            }
        }

        private void SetCache(HttpCachePolicyBase cache, System.Net.Http.Headers.CacheControlHeaderValue cacheIn)
        {
            if (cacheIn.Public)
            {
                cache.SetCacheability(HttpCacheability.Public);
            }
            if (cacheIn.Private)
            {
                cache.SetCacheability(HttpCacheability.Private);
            }
            if (cacheIn.MaxAge != null)
            {
                cache.SetMaxAge(cacheIn.MaxAge.Value);
            }

        }
    }
}