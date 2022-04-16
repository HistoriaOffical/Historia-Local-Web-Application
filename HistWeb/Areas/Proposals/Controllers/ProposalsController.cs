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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using static HistWeb.Controllers.MasternodeController;
using HistWeb.Areas.Masternode.Models;
using System.Threading;
using HistWeb.Areas.Proposals.Models;
using Microsoft.AspNetCore.Http;
using System.Web;
using Newtonsoft.Json;
using Ipfs;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Drawing;
using HistWeb.Helpers;
using Microsoft.AspNetCore.Identity;
using Ganss.XSS;
using OpenGraphNet;
using System.Text.RegularExpressions;

namespace HistWeb.Controllers
{
    [Area("Proposals")]
    public class ProposalsController : Controller
    {
        private string _rpcServerUrl = "http://" + ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;
        private string _userName = ApplicationSettings.HistoriaRPCUserName;
        private string _password = ApplicationSettings.HistoriaRPCPassword;
        private string _ipfsUrl = ApplicationSettings.IPFSHost;
        private string _ipfsApiPort = ApplicationSettings.IPFSApiPort.ToString();
        private string _ipfsWebPort = ApplicationSettings.IPFSPort.ToString();
        private readonly IHostingEnvironment _hostingEnvironment;
        private IConfiguration _configuration = null;
        private readonly ILogger _logger;
        private bool _isTestNet = true;

        public ProposalsController(IHostingEnvironment hostingEnvironment, IConfiguration config, ILoggerFactory loggerFactory, ILogger<HomeController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _configuration = config;
            _logger = logger;

            _ipfsUrl = ApplicationSettings.IPFSHost;
            _ipfsApiPort = ApplicationSettings.IPFSApiPort.ToString();
            _ipfsWebPort = ApplicationSettings.IPFSPort.ToString();

            _rpcServerUrl = "http://" + ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;
            _userName = ApplicationSettings.HistoriaRPCUserName;
            _password = ApplicationSettings.HistoriaRPCPassword;

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> ProposalDetails(string hash, string sort, string cid)
        {

            ProposalViewModel model = new ProposalViewModel();
            var sanitizer = new HtmlSanitizer();

            ProposalModel pm = new ProposalModel();
            int superblock = 0;
            int isArchive = 0;
            //Prepare call
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
            webRequest.Credentials = new NetworkCredential(_userName, _password);
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";
            webRequest.Timeout = 5000;
            string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [\"get\", \"" + hash + "\"] }";

            // serialize json for the request
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
            webRequest.ContentLength = byteArray.Length;
            Stream dataStream = webRequest.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            try
            {
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                _logger.LogInformation("Proposals: getResp: " + getResp);

                dynamic proposals = JObject.Parse(getResp);

                //Get Datastring Info
                var ds = proposals.result.DataString.ToString();
                string p1 = ds;
                dynamic proposal1 = JObject.Parse(p1);

                //Get Voting Info
                var ds1 = proposals.result.FundingResult.ToString();
                string p2 = ds1;
                dynamic proposal2 = JObject.Parse(p2);

                string hostname = _ipfsUrl;

                pm.Hostname = hostname;
                pm.IPFSUrl = _ipfsUrl;
                pm.IPFSApiPort = _ipfsApiPort;
                pm.IPFSWebPort = _ipfsWebPort;
                pm.Hash = hash;
                pm.ProposalName = sanitizer.Sanitize(proposal1.summary.name.ToString());
                pm.ProposalSummary = sanitizer.Sanitize(proposal1.summary.description.ToString());

                pm.ProposalDescriptionUrl = proposal1.ipfscid.ToString();
                pm.PaymentAddress = proposal1.payment_address.ToString();
                pm.YesCount = long.Parse(proposal2.YesCount.ToString());
                pm.NoCount = long.Parse(proposal2.NoCount.ToString());
                pm.AbstainCount = long.Parse(proposal2.AbstainCount.ToString());
                pm.CachedLocked = bool.Parse(proposals.result.fCachedLocked.ToString());

                pm.CachedFunding = bool.Parse(proposals.result.fCachedFunding.ToString());
                pm.PaymentAmount = decimal.Parse(proposal1.payment_amount.ToString());
                pm.PaymentDate = UnixTimeStampToDateTime(double.Parse(proposal1.start_epoch.ToString())).ToString("MM/dd/yyyy");
                DateTime EndDateTemp = UnixTimeStampToDateTime(double.Parse(proposal1.end_epoch.ToString()));
                pm.PaymentEndDate = EndDateTemp.AddDays(-2);
                pm.ProposalDate = pm.PaymentDate;

                pm.PermLocked = bool.Parse(proposals.result.fPermLocked.ToString());
                
                pm.ProposalDescriptionUrlRazor = "https://" + hostname + "/ipfs/" + proposal1.ipfscid.ToString() + "/index.html";
                pm.Type = proposal1.type.ToString();
  
                if (pm.PermLocked)
                {
                    pm.PastSuperBlock = 1;
                }
                else
                {
                    pm.PastSuperBlock = 0;
                }
            }
            catch (Exception ex)
            {

                _logger.LogCritical("ProposalDescription Error: " + ex.ToString());

            }
            model.CurrentModel = pm;
            return View(model);
        }



        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }


    }
}
