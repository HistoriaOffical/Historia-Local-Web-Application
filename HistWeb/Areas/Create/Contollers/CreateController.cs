using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HistWeb.Models;
using System.Net;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Dynamic;
using Newtonsoft.Json.Converters;
using HistWeb.Areas.Masternode.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Ganss.XSS;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using HtmlAgilityPack;
using System.Threading;
using System.Web;
using PuppeteerSharp;

namespace HistWeb.Controllers
{
    [Area("Create")]
    public class CreateController : Controller
    {

        private string _rpcServerUrl = "http://" + ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;
        private string _userName = ApplicationSettings.HistoriaRPCUserName;
        private string _password = ApplicationSettings.HistoriaRPCPassword;
        private string _IpfsApiHost = "http://" + ApplicationSettings.IPFSApiHost;
        private string _IpfsApiPort = ApplicationSettings.IPFSApiPort.ToString();
        private IConfiguration _configuration = null;

        public CreateController(IConfiguration iConfig)
        {
            _configuration = iConfig;
            _rpcServerUrl = "http://" + ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;
            _userName = ApplicationSettings.HistoriaRPCUserName;
            _password = ApplicationSettings.HistoriaRPCPassword;
            _IpfsApiHost = "http://" + ApplicationSettings.IPFSApiHost;
            _IpfsApiPort = ApplicationSettings.IPFSApiPort.ToString();

        }

        public async Task<IActionResult> Index()
        {

            using (var conn = new SqliteConnection("Data Source=basex.db"))
            {
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = "DELETE FROM archives";
                    cmd.ExecuteNonQuery();

                }
            }

            return View();
        }



        public async Task<IActionResult> Create()
        {
            return View();
        }

        public JsonResult GetDrafts()
        {
            var sanitizer = new HtmlSanitizer();
            try
            {
                List<object> items = new List<object>();
                var connString = _configuration.GetConnectionString("HistoriaContextConnection");

                using (var conn = new SqliteConnection("Data Source=basex.db"))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT * FROM items";

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                var id = rdr.GetInt32(rdr.GetOrdinal("Id"));
                                var Name = !rdr.IsDBNull(rdr.GetOrdinal("Name")) ? rdr.GetString(rdr.GetOrdinal("Name")) : "";
                                var Summary = !rdr.IsDBNull(rdr.GetOrdinal("Summary")) ? rdr.GetString(rdr.GetOrdinal("Summary")) : "";
                                var Type = !rdr.IsDBNull(rdr.GetOrdinal("Type")) ? rdr.GetString(rdr.GetOrdinal("Type")) : "";
                                var urlParams = id + "&Type=" + Type + "&Template=none";
                                var item = new { Id = urlParams, draftName = sanitizer.Sanitize(Name), draftSummary = sanitizer.Sanitize(Summary) };
                                items.Add(item);
                            }

                        }
                    }
                }


                return Json(items);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }
        }

        [HttpGet]
        public IActionResult CreateBuilder([FromQuery(Name = "Id")] string Id)
        {
            return View();
        }


        [HttpGet]
        public IActionResult CreateBuilderBlank()
        {
            return View();
        }


        [HttpGet]
        public IActionResult CreateBuilderLoad([FromQuery(Name = "Id")] string Id)
        {
            List<object> items = new List<object>();
            var sanitizer = new HtmlSanitizer();

            if (Id == "0")
            {
                var prepRespJson1 = new { id = "0" };
                return Json(prepRespJson1);
            }
            if (!string.IsNullOrEmpty(Id))
            {
                Console.WriteLine("Resuming with Id: " + Id);
                try
                {
                    using (var conn = new SqliteConnection("Data Source=basex.db"))
                    {
                        using (var cmd = conn.CreateCommand())
                        {

                            conn.Open();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = "SELECT * FROM items WHERE Id = @Id";
                            cmd.Parameters.AddWithValue("Id", Id);

                            using (SqliteDataReader rdr = cmd.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    var Name = !rdr.IsDBNull(rdr.GetOrdinal("Name")) ? rdr.GetString(rdr.GetOrdinal("Name")) : "";
                                    var Summary = !rdr.IsDBNull(rdr.GetOrdinal("Summary")) ? rdr.GetString(rdr.GetOrdinal("Summary")) : "";
                                    var html = !rdr.IsDBNull(rdr.GetOrdinal("html")) ? rdr.GetString(rdr.GetOrdinal("html")) : "";
                                    var css = !rdr.IsDBNull(rdr.GetOrdinal("css")) ? rdr.GetString(rdr.GetOrdinal("css")) : "";
                                    var PaymentAddress = !rdr.IsDBNull(rdr.GetOrdinal("PaymentAddress")) ? rdr.GetString(rdr.GetOrdinal("PaymentAddress")) : "";
                                    var PaymentAmount = !rdr.IsDBNull(rdr.GetOrdinal("PaymentAmount")) ? rdr.GetString(rdr.GetOrdinal("PaymentAmount")) : "";
                                    var IsDraft = !rdr.IsDBNull(rdr.GetOrdinal("IsDraft")) ? rdr.GetString(rdr.GetOrdinal("IsDraft")) : "";
                                    var Type = !rdr.IsDBNull(rdr.GetOrdinal("Type")) ? rdr.GetString(rdr.GetOrdinal("Type")) : "";
                                    var item = new { Id = Id, Name = sanitizer.Sanitize(Name), Summary = sanitizer.Sanitize(Summary), html = html, css = css, PaymentAddress = PaymentAddress, PaymentAmount = PaymentAmount, IsDraft = IsDraft, Type = Type };
                                    items.Add(item);
                                }
                            }

                        }
                    }
                    return Json(items);

                }
                catch (Exception ex)
                {
                    var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                    return Json(prepRespJson1);
                }
            }

            return Json(new { Success = false, Error = "Can not load" });
        }

        [HttpGet]
        public IActionResult CreateBuilderLoadTemplate([FromQuery(Name = "Template")] string Template)
        {
            List<object> items = new List<object>();
            var sanitizer = new HtmlSanitizer();
            var html = "";
            var css = "";
            if (!string.IsNullOrEmpty(Template))
            {
                try
                {
                    using (var conn = new SqliteConnection("Data Source=basex.db"))
                    {
                        using (var cmd = conn.CreateCommand())
                        {

                            conn.Open();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = "SELECT * FROM templates WHERE name = @name";
                            cmd.Parameters.AddWithValue("name", Template);
                            html = "Something went wrong. Template Not Found!";
                            using (SqliteDataReader rdr = cmd.ExecuteReader())
                            {
                                var item = new { Id = 0, html = html, css = css };
                                if (rdr.Read())
                                {
                                    html = !rdr.IsDBNull(rdr.GetOrdinal("html")) ? rdr.GetString(rdr.GetOrdinal("html")) : "";
                                    css = !rdr.IsDBNull(rdr.GetOrdinal("css")) ? rdr.GetString(rdr.GetOrdinal("css")) : "";
                                    item = new { Id = 0, html = html, css = css };
                                    
                                }
                                items.Add(item);
                            }

                        }
                    }
                    return Json(items);

                }
                catch (Exception ex)
                {
                    var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                    return Json(prepRespJson1);
                }
            }

            return Json(new { Success = false, Error = "Can not load" });

         }

        [HttpGet]
        public async Task<IActionResult> CreateBuilderLoadArchiveURL(string html)
        {
            List<object> items = new List<object>();
            var sanitizer = new HtmlSanitizer();

            try
            {
                using (var conn = new SqliteConnection("Data Source=basex.db"))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        //Need to do this because of issues with the extension.
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "DELETE FROM archives";
                        cmd.ExecuteNonQuery();

                    }
                }

                var prepRespJson = new { Success = true };
                return Json(prepRespJson);

            }
            catch (Exception ex)
            {

                var prepRespJsonFail = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJsonFail);
            }

        }


        [HttpGet]
        public async Task<IActionResult> PollForArchive()
        {
            List<object> items = new List<object>();
            var sanitizer = new HtmlSanitizer();

            try
            {
                while (items.Count == 0)
                {
                    using (var conn = new SqliteConnection("Data Source=basex.db"))
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            conn.Open();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = "SELECT * FROM archives LIMIT 1";
                            cmd.ExecuteNonQuery();

                            using (SqliteDataReader rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    var url = rdr.GetString(rdr.GetOrdinal("url"));
                                    var html = !rdr.IsDBNull(rdr.GetOrdinal("html")) ? rdr.GetString(rdr.GetOrdinal("html")) : "";
                                    var image = !rdr.IsDBNull(rdr.GetOrdinal("image")) ? rdr.GetString(rdr.GetOrdinal("image")) : "";

                                    string indexHtml = "<!DOCTYPE html><html lang=\"en\"> <head> <meta charset=\"utf-8\"/> <meta name=\"viewport\" " +
                                        "content=\"width=device-width, initial-scale = 1, user-scalable = yes\"/> <link rel=\"stylesheet\" " +
                                        "href=\"https://maxcdn.bootstrapcdn.com/bootstrap/4.4.1/css/bootstrap.min.css\"/> " +
                                        "<script src=\"https://ajax.googleapis.com/ajax/libs/jquery/3.5.1/jquery.min.js\"></script> <script " +
                                        "src=\"https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.16.0/umd/popper.min.js\"></script> <script " +
                                        "src=\"https://maxcdn.bootstrapcdn.com/bootstrap/4.4.1/js/bootstrap.min.js\"></script> </head> <body> <center>" +
                                        "This page was archived on " + DateTime.UtcNow.ToString("F") + " UTC</center> <div class=\"container\"> <div class=\"row\" id=\"ArchivePreview\" style=\"max-width:1500px\"> " +
                                        "<div class=\"col-12\"> <ul id=\"tabsJustified\" class=\"nav nav-tabs\" style=\"background: #fff;\"> " +
                                        "<li class=\"nav-item\"><a href=\"#archiveHtml\" data-target=\"#archiveHtml\" data-toggle=\"tab\" role=\"tab\" " +
                                        "class=\"nav-link small text-uppercase\" aria-selected=\"true\">HTML</a></li><li class=\"nav-item\">" +
                                        "<a href=\"#archiveImage\" data-target=\"#archiveImage\" data-toggle=\"tab\" role=\"tab\" class=\"nav-link small " +
                                        "text-uppercase\" aria-selected=\"false\">IMAGE</a></li></ul> <div class=\"tab-content\" style=\"max-width: 1318px;\">" +
                                        "<div id=\"archiveHtml\" class=\"tab-pane fade show active\"><div style=\"height: 100%;\"><div class=\"row mt-4\">" +
                                        "<div class=\"col-12 mx-auto\"><iframe id=\"archiveHtmlContainer\" height=\"1000px\" width=\"100%\" srcdoc=\"" + HttpUtility.HtmlEncode(html) + "\"></iframe>" +
                                        "</div></div></div></div><div id=\"archiveImage\" class=\"tab-pane fade\"><div style=\"height: 100%;\"><div class=\"row mt-4\">" +
                                        "<div class=\"col-12 mx-auto\"><div id=\"archiveImageContainer\" style=\"width: 100%; height: 100%; overflow-y: scroll;" +
                                        " overflow-x: scroll;\"><img style=\"width:100%; height:100%;\" src=\"data:image/png;base64, " + image + "\"/></div></div></div></div></div></div>" +
                                        "</div></div></div><script type=\"text/javascript\">var iframe = document.getElementById(\"archiveHtmlContainer\");" + 
                                        "iframe.onload = function(){iframe.style.height = iframe.contentWindow.document.body.scrollHeight + 50 + 'px';} " +
                                        "</script></body></html>";

                                    var size = indexHtml.Length;
                                    var item = new { Id = 0, url = url, html = html, template = indexHtml, totalSize = size };
                                    items.Add(item);
                                }

                            }

                        }
                    }
                    Thread.Sleep(1000);
                }
                return Json(items);

            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString(), html = "none" };
                return Json(prepRespJson1);
            }

        }

        public string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", ""));
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        [HttpPost]
        public async Task<JsonResult> SaveCreateDraft(IFormCollection formCollection)
        {
            var sanitizer = new HtmlSanitizer();
            try
            {
                string html = formCollection["html"];
                string css = sanitizer.Sanitize(formCollection["css"]);
                string id = sanitizer.Sanitize(formCollection["id"]);
                string Name = sanitizer.Sanitize(formCollection["Name"]);
                string Summary = sanitizer.Sanitize(formCollection["Summary"]);
                string PaymentAddress = sanitizer.Sanitize(formCollection["PaymentAddress"]);
                string PaymentAmount = sanitizer.Sanitize(formCollection["PaymentAmount"]);
                string IsDraft = sanitizer.Sanitize(formCollection["IsDraft"]);
                string Type = sanitizer.Sanitize(formCollection["Type"]);
                //string ParentIPFSCID = sanitizer.Sanitize(formCollection["ParentIPFSCID"]);

                if (id == "0")
                {
                    using (var conn = new SqliteConnection("Data Source=basex.db"))
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            conn.Open();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = "Insert into items (Name, Summary, html, css, PaymentAddress, PaymentAmount, IsDraft, Type) " +
                                "VALUES (@Name, @Summary, @html, @css, @PaymentAddress, @PaymentAmount, @IsDraft, @Type)";
                            cmd.Parameters.AddWithValue("Name", Name);
                            cmd.Parameters.AddWithValue("Summary", Summary);
                            cmd.Parameters.AddWithValue("html", html);
                            cmd.Parameters.AddWithValue("css", css);
                            cmd.Parameters.AddWithValue("PaymentAddress", PaymentAddress);
                            cmd.Parameters.AddWithValue("PaymentAmount", PaymentAmount);
                            cmd.Parameters.AddWithValue("IsDraft", IsDraft);
                            cmd.Parameters.AddWithValue("Type", Type);
                            //cmd.Parameters.AddWithValue("@ParentIPFSCID", ParentIPFSCID);
                            cmd.ExecuteNonQuery();

                        }
                    }
                }
                else
                {

                    using (var conn = new SqliteConnection("Data Source=basex.db"))
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            conn.Open();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = "UPDATE items " +
                                "SET Name = @Name, Summary = @Summary, html = @html, css = @css, " +
                                "PaymentAddress = @PaymentAddress, PaymentAmount = @PaymentAmount, " +
                                "IsDraft = @IsDraft, Type = @Type where id = @id";
                            cmd.Parameters.AddWithValue("Name", Name);
                            cmd.Parameters.AddWithValue("Summary", Summary);
                            cmd.Parameters.AddWithValue("html", html);
                            cmd.Parameters.AddWithValue("css", css);
                            cmd.Parameters.AddWithValue("PaymentAddress", PaymentAddress);
                            cmd.Parameters.AddWithValue("PaymentAmount", PaymentAmount);
                            cmd.Parameters.AddWithValue("IsDraft", IsDraft);
                            cmd.Parameters.AddWithValue("Type", Type);
                            cmd.Parameters.AddWithValue("id", id);
                            cmd.ExecuteNonQuery();

                        }
                    }
                }
                dynamic prepRespJson = new { Success = true };
                return Json(prepRespJson);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }
        }

        [HttpPost]
        public async Task<JsonResult> DeleteCreateDraft(IFormCollection formCollection)
        {
            var sanitizer = new HtmlSanitizer();
            try
            {
                string id = sanitizer.Sanitize(formCollection["id"]);

                if (id != "0")
                {
                    using (var conn = new SqliteConnection("Data Source=basex.db"))
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            conn.Open();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = "DELETE FROM items WHERE id=@id";
                            cmd.Parameters.AddWithValue("id", id);
                            cmd.ExecuteNonQuery();

                        }
                    }
                }
                dynamic prepRespJson = new { Success = true };
                return Json(prepRespJson);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }
        }


        [HttpPost]
        public async Task<JsonResult> Submit(IFormCollection formCollection)
        {
            var sanitizer = new HtmlSanitizer();
            string html = formCollection["html"];
            string css = sanitizer.Sanitize(formCollection["css"]);
            string id = sanitizer.Sanitize(formCollection["id"]);
            bool isArchive = false;
            if(sanitizer.Sanitize(formCollection["isArchive"]) == "1")
            {
                isArchive = true;
            }
            string SummaryName = sanitizer.Sanitize(formCollection["Name"]);
            string Summary = sanitizer.Sanitize(formCollection["Summary"]);
            string PaymentAddress = sanitizer.Sanitize(formCollection["PaymentAddress"]);
            string PaymentAmount = sanitizer.Sanitize(formCollection["PaymentAmount"]);
            string PaymentDate = sanitizer.Sanitize(formCollection["PaymentDate"]);
            string IsDraft = sanitizer.Sanitize(formCollection["IsDraft"]);
            string Type = sanitizer.Sanitize(formCollection["Type"]);
            string passphrase = sanitizer.Sanitize(formCollection["passphrase"]);
            long uniqueName = DateTimeOffset.Now.ToUnixTimeSeconds();

            try
            {
                bool validAddress = ValidateAddress(PaymentAddress);
                if (!validAddress)
                {
                    dynamic prepRespJsonFail = new { Success = false, Error = "Invalid Payment Address" };
                    return Json(prepRespJsonFail);
                }
            }
            catch (Exception ex)
            {
                dynamic prepRespJsonFail = new { Success = false, Error = "Unable to validate Payment Address" };
                return Json(prepRespJsonFail);
            }

            if (!string.IsNullOrEmpty(passphrase))
            {
                try
                {
                    bool validPassphrase = UnlockWallet(passphrase);
                    if (!validPassphrase)
                    {
                        dynamic prepRespJsonFail = new { Success = false, Error = "Invalid Passphrase" };
                        return Json(prepRespJsonFail);
                    }
                }
                catch (Exception ex)
                {
                    dynamic prepRespJsonFail = new { Success = false, Error = "Unable to validate unlock wallet. Is Historia Core running?" };
                    return Json(prepRespJsonFail);
                }
            }
            string endEpoch = CalcEndEpoch(PaymentDate);
            string HtmlFinal = GenerateHTMLForIpfs(html, css, SummaryName, Summary, isArchive);
            string IpfsCid, IpfsPid = "";

            if (!string.IsNullOrEmpty(HtmlFinal))
            {
                try
                {
                    Ipfs.Http.IpfsClient client = new Ipfs.Http.IpfsClient($"{_IpfsApiHost}:{_IpfsApiPort}");
                    var filePath = GetTemporaryDirectory();
                    using (var stream = new FileStream(Path.Combine(filePath, "index.html"), FileMode.Create))
                    {
                        byte[] info = new UTF8Encoding(true).GetBytes(HtmlFinal);
                        stream.Write(info, 0, info.Length);
                    }
                    //Add the file/directory to IPFS.
                    Ipfs.IFileSystemNode ret = await client.FileSystem.AddDirectoryAsync(filePath, true, null);
                    IpfsCid = ret.Id.Hash.ToString();


                }
                catch (Exception ex)
                {
                    dynamic prepRespJsonFail = new { Success = false, Error = "Can not add file to IPFS. Is your IPFS API Server setup properly?" };
                    return Json(prepRespJsonFail);
                }
            }
            else
            {
                dynamic prepRespJsonFail = new { Success = false, Error = "Something went wrong" };
                return Json(prepRespJsonFail);
            }

            //Do submission via gobject submit
            if (!GobjectPrepareSubmit(endEpoch, uniqueName, PaymentAddress, PaymentAmount, Summary, SummaryName, Type, IpfsCid, IpfsPid))
            {
                dynamic prepRespJsonFail = new { Success = false, Error = "Could Not Submit Proposal. Do you have available coins in your wallet?" };
                return Json(prepRespJsonFail);
            }

            try
            {
                dynamic prepRespJson = new { Success = true };
                return Json(prepRespJson);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false };
                return Json(prepRespJson1);
            }
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        [HttpPost]
        public bool ValidateAddress(IFormCollection formCollection)
        {
            string paymentAddress = formCollection["rewardPaymentAddress"];
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                webRequest.Timeout = 5000;
                string jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"validateaddress\", \"params\": [\"{paymentAddress}\"] }}";
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                dynamic va = JObject.Parse(getResp);
                var isValid = bool.Parse(va.result.isvalid.ToString().ToLower());
                return isValid;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool ValidateAddress(string address)
        {
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                webRequest.Timeout = 5000;
                string jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"validateaddress\", \"params\": [\"{address}\"] }}";

                Debug.WriteLine(" for proposals: " + jsonstring);
                // serialize json for the request
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                dynamic va = JObject.Parse(getResp);

                //Get Datastring Info
                var isValid = bool.Parse(va.result.isvalid.ToString().ToLower());
                return isValid;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool UnlockWallet(string passphrase)
        {
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                webRequest.Timeout = 5000;
                string jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"walletpassphrase\", \"params\": [\"{passphrase}\", 60] }}";

                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string CalcEndEpoch(string PaymentDate)
        {
            string endEpoch = "";
            if (string.IsNullOrEmpty(PaymentDate))
            {
                string[] splitDate = DateTime.Now.ToString("MM/dd/yyyy").Split('/');
                DateTimeOffset dto = new DateTimeOffset(Int16.Parse(splitDate[2]), Int16.Parse(splitDate[0]), Int16.Parse(splitDate[1]), 0, 0, 0, TimeSpan.Zero);
                endEpoch = dto.ToUnixTimeSeconds().ToString();
                endEpoch = (Int64.Parse(endEpoch) + 1296000).ToString(); // Add 15 days to end of epoch timeframe for safe time period
            }
            else
            {
                string[] splitDate = PaymentDate.Split('/');
                DateTimeOffset dto = new DateTimeOffset(Int16.Parse(splitDate[2]), Int16.Parse(splitDate[0]), Int16.Parse(splitDate[1]), 0, 0, 0, TimeSpan.Zero);
                endEpoch = dto.ToUnixTimeSeconds().ToString();
                endEpoch = (Int64.Parse(endEpoch) + 1296000).ToString(); // Add 15 days to end of epoch timeframe for safe time period
            }
            return endEpoch;
        }

        private string GenerateHTMLForIpfs(string html, string css, string title, string description, bool isArchive)
        {
            string htmlRet;

            if (isArchive) { 
                htmlRet =
                "<!DOCTYPE html><html lang = \"en-US\" ><head><title>" + title + "</title><meta name = \"description\" content = \"" + description + "\" />" +
                "<meta name = \"keywords\" content = \"Historia, History, blockchain, cryptocurrency, HTA\" />" +
                "<meta name =\"robots\" content = \"index, follow\" ><meta http-equiv = \"Content-Type\" content = \"text/html; charset=utf-8\" />" +
                "<meta name = \"viewport\" content = \"width=device-width, initial-scale=1\" /><meta http-equiv = \"X-UA-Compatible\" " +
                "content = \"IE=edge\" ></head>" + html + "</html>";
            } else { 
                htmlRet =
                "<!DOCTYPE html><html lang = \"en-US\" ><head><title>" + title + "</title><meta name = \"description\" content = \"" + description + "\" />" +
                "<meta name = \"keywords\" content = \"Historia, History, blockchain, cryptocurrency, HTA\" />" +
                "<meta name =\"robots\" content = \"index, follow\" ><meta http-equiv = \"Content-Type\" content = \"text/html; charset=utf-8\" />" +
                "< meta name = \"viewport\" content = \"width=device-width, initial-scale=1\" /><meta http-equiv = \"X-UA-Compatible\" " +
                "content = \"IE=edge\" ><style>" + css + "</style></head>" + html + "</html>";
            }
            return htmlRet;
        }

        public class PageData
        {
            public string title { get; set; }
            public string filename { get; set; }
            public string content { get; set; }

        }

        [HttpPost]
        public async Task<JsonResult> ArchiveImport([FromBody] PageData pageData)
        {
            string URL = "";
            string HTML = pageData.content;
            var sanitizer = new HtmlSanitizer();
            //Get URL
            foreach (Match item in Regex.Matches(HTML, @"(http|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?"))
            {
                URL = item.Value;
                break;
            }

            //Get Screenshot of Archive Site via PuppeteerSharp
            var image = "";
            using var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync(BrowserFetcher.DefaultRevision);
            var launchOptions = new LaunchOptions()
            {
                Headless = true,
            };


            using (var browser = await Puppeteer.LaunchAsync(launchOptions))
            using (var page = await browser.NewPageAsync())
            {
                await page.GoToAsync(URL);
                var SetViewportOptions = new ViewPortOptions()
                {
                    Width = 1280,
                };
                await page.SetViewportAsync(SetViewportOptions);
                var screenshotOptions = new ScreenshotOptions()
                {
                    FullPage = true,
                };
                image = await page.ScreenshotBase64Async(screenshotOptions);
            }

            try
            {
                using (var conn = new SqliteConnection("Data Source=basex.db"))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "Insert into archives (url, html, image) VALUES (@url, @html, @image)";
                        cmd.Parameters.AddWithValue("url", sanitizer.Sanitize(URL));
                        cmd.Parameters.AddWithValue("html", HTML);
                        cmd.Parameters.AddWithValue("image", image);
                        cmd.ExecuteNonQuery();

                    }
                }
                dynamic prepRespJson = new { Success = true, Error = "Success" };
                return Json(prepRespJson);
            }
            catch (Exception ex)
            {
                dynamic prepRespJsonFail = new { Success = false, Error = "Something Went Wrong" };
                return Json(prepRespJsonFail);
            }
        }
        private bool GobjectPrepareSubmit(string EndEpoch, long name, string PaymentAddress, string PaymentAmount,
            string ProposalSummary, string ProposalSummaryName, string Type, string IpfsCID, string ipfsPID)
        {

            long BlockHeightBefore = GetBlockCount();
            long BlockHeightAfter = GetBlockCount();

            string PrepareResp = "0";
            var hexString = "";

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                webRequest.Timeout = 5000;
  
                string proposalJson = string.Format(@"{{""end_epoch"":{0},""name"":""{1}"",""payment_address"":""{2}"",""payment_amount"":{3},""start_epoch"":{4},""type"":{5},""ipfscid"":""{6}"",""ipfspid"":""{7}"",""summary"":{{""name"":""{8}"", ""description"":""{9}""}}}}",
    EndEpoch, name, PaymentAddress, PaymentAmount, name, (Type == "5" ? "4" : Type), IpfsCID, ipfsPID, ProposalSummaryName, ProposalSummary.Length > 255 ? HttpUtility.JavaScriptStringEncode(ProposalSummary.Substring(0, 254)) : HttpUtility.JavaScriptStringEncode(ProposalSummary));
                byte[] proposalBytes = Encoding.Default.GetBytes(proposalJson);
                hexString = BitConverter.ToString(proposalBytes);
                hexString = hexString.Replace("-", "");
                string jsonstring = string.Format("{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [ \"prepare\", \"0\", \"1\", \"{0}\", \"{1}\" ]}}", name, hexString);

                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                PrepareResp = sr.ReadToEnd();


            }
            catch (Exception ex)
            {
                return false;
            }


            while (BlockHeightAfter <= BlockHeightBefore)
            {
                BlockHeightAfter = GetBlockCount();
                Thread.Sleep(5000);
            }

            try
            {
                var resp = JObject.Parse(PrepareResp);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                webRequest.Timeout = 5000;
                string jsonstring = string.Format("{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [ \"submit\", \"0\", \"1\", \"{0}\", \"{1}\", \"{2}\" ]}}", name, hexString, ((string)JObject.Parse(PrepareResp)["result"]).ToUpper());

                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                PrepareResp = sr.ReadToEnd();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        private long GetBlockCount()
        {
            long currentBlock = 0;
            HttpWebRequest webRequestGetInfo = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
            webRequestGetInfo.Credentials = new NetworkCredential(_userName, _password);
            webRequestGetInfo.ContentType = "application/json-rpc";
            webRequestGetInfo.Method = "POST";
            string jsonstring = string.Format("{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"getinfo\", \"params\": []}}");
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
            webRequestGetInfo.ContentLength = byteArray.Length;
            Stream dataStreamGetInfo = webRequestGetInfo.GetRequestStream();
            dataStreamGetInfo.Write(byteArray, 0, byteArray.Length);
            dataStreamGetInfo.Close();

            WebResponse webResponseGetInfo = webRequestGetInfo.GetResponse();
            StreamReader srGetInfo = new StreamReader(webResponseGetInfo.GetResponseStream());
            string getRespInfo = srGetInfo.ReadToEnd();
            srGetInfo.Close();
            dynamic respGetInfo = JObject.Parse(getRespInfo);
            string strGetInfo = respGetInfo.ToString();

            currentBlock = respGetInfo.result.blocks;
            return currentBlock;
        }

    }


}
