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
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using System.Net.Http;

namespace HistWeb.Controllers
{
    [Area("Masternode")]
    public class MasternodeController : Controller
    {
        private string _rpcServerUrl = "http://"+ ApplicationSettings.HistoriaClientIPAddress + ":"+ ApplicationSettings.HistoriaRPCPort;
        private string _userName = ApplicationSettings.HistoriaRPCUserName;
        private string _password = ApplicationSettings.HistoriaRPCPassword;
        private IConfiguration _configuration = null;

        public MasternodeController(IConfiguration iConfig)
        {
            _configuration = iConfig;
            _rpcServerUrl = "http://"+ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;
            _userName = ApplicationSettings.HistoriaRPCUserName;
            _password = ApplicationSettings.HistoriaRPCPassword;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Registration()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public JsonResult GetCurrentSuperBlockInfo()
        {
            try
            {
                //Get Governance Info
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";

                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"getgovernanceinfo\", \"params\": [] }";
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();

                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                var expConverter = new ExpandoObjectConverter();
                dynamic json = JsonConvert.DeserializeObject<ExpandoObject>(getResp, expConverter);
                string nextsuperblock = "";
                string lastsuperblock = "";

                foreach (var tmp in json.result)
                {
                    var data = tmp.ToString();
                    string[] substrings = Regex.Split(data, "\\s+");
                    switch (substrings[0])
                    {
                        case "[lastsuperblock,":
                            lastsuperblock = substrings[1];
                            lastsuperblock = lastsuperblock.Remove(lastsuperblock.Length - 1, 1);
                            break;
                        case "[nextsuperblock,":
                            nextsuperblock = substrings[1];
                            nextsuperblock = nextsuperblock.Remove(nextsuperblock.Length - 1, 1);
                            break;
                        default: 
                            break;
                    }
                }

                //Get Current Supply
                
                HttpWebRequest webRequest1 = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest1.Credentials = new NetworkCredential(_userName, _password);
                webRequest1.ContentType = "application/json-rpc";
                webRequest1.Method = "POST";
                jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gettxoutsetinfo\", \"params\": [] }";
                byte [] byteArray1 = Encoding.UTF8.GetBytes(jsonstring);
                webRequest1.ContentLength = byteArray1.Length;
                dataStream = webRequest1.GetRequestStream();

                dataStream.Write(byteArray1, 0, byteArray1.Length);
                dataStream.Close();
                webResponse = webRequest1.GetResponse();
                sr = new StreamReader(webResponse.GetResponseStream());
                getResp = sr.ReadToEnd();

                expConverter = new ExpandoObjectConverter();
                json = JsonConvert.DeserializeObject<ExpandoObject>(getResp, expConverter);
                string cSupply = "";

                foreach (var tmp in json.result)
                {
                    var data = tmp.ToString();
                    string[] substrings = Regex.Split(data, "\\s+");
                    switch (substrings[0])
                    {
                        case "[total_amount,":
                            cSupply = substrings[1];
                            cSupply = cSupply.Remove(cSupply.Length - 1, 1);
                            break;
                        default:
                            break;
                    }
                }
                
                
                //Get Current Block
                HttpWebRequest webRequest2 = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest2.Credentials = new NetworkCredential(_userName, _password);
                webRequest2.ContentType = "application/json-rpc";
                webRequest2.Method = "POST";
                jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"getinfo\", \"params\": [] }";
                byte[] byteArray2 = Encoding.UTF8.GetBytes(jsonstring);
                webRequest2.ContentLength = byteArray2.Length;
                dataStream = webRequest2.GetRequestStream();

                dataStream.Write(byteArray2, 0, byteArray2.Length);
                dataStream.Close();
                webResponse = webRequest2.GetResponse();
                sr = new StreamReader(webResponse.GetResponseStream());
                getResp = sr.ReadToEnd();

                expConverter = new ExpandoObjectConverter();
                json = JsonConvert.DeserializeObject<ExpandoObject>(getResp, expConverter);
                string currentBlock = "";

                foreach (var tmp in json.result)
                {
                    var data = tmp.ToString();
                    string[] substrings = Regex.Split(data, "\\s+");
                    switch (substrings[0])
                    {
                        case "[blocks,":
                            currentBlock = substrings[1];
                            currentBlock = currentBlock.Remove(currentBlock.Length - 1, 1);
                            break;
                        default:
                            break;
                    }
                }

                //Get Total Budget aka getsuperblockbudget blocknum
                webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"getsuperblockbudget\", \"params\": [" + nextsuperblock +"] }";
                byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                dataStream = webRequest.GetRequestStream();

                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                webResponse = webRequest.GetResponse();
                sr = new StreamReader(webResponse.GetResponseStream());
                getResp = sr.ReadToEnd();

                expConverter = new ExpandoObjectConverter();
                json = JsonConvert.DeserializeObject<ExpandoObject>(getResp, expConverter);

                var totalBudget = json.result;

                //Get Passing Budget aka gobject list funding
                //Get Total Budget aka getsuperblockbudget blocknum
                webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [\"list\", \"funding\"] }";
                byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                webResponse = webRequest.GetResponse();
                sr = new StreamReader(webResponse.GetResponseStream());
                getResp = sr.ReadToEnd();
                expConverter = new ExpandoObjectConverter();
                json = JsonConvert.DeserializeObject<ExpandoObject>(getResp, expConverter);
                //string totalPassingBudget = json.result.ToString();
                //if (string.IsNullOrEmpty(totalPassingBudget))
                //{
                  string  totalPassingBudget = "0";
                //} 

                var res = "";
                res = "{ \"result\": { \"currentBlock\": " + currentBlock +
                         ", \"nextsuperblock\": " + nextsuperblock +
                         ", \"cSupply\": " + cSupply +
                         ", \"totalBudget\": " + totalBudget +
                         ", \"totalPassingCoins\": " + totalPassingBudget +
                         "} }";
                return Json(res);

            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }


        }

        [HttpGet]
        public JsonResult GetMasterNodes()
        {
            try
            {
                //Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";

                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"masternodelist\", \"params\": [\"full\"] }";
                System.Diagnostics.Debug.WriteLine("GetMasterNodes: " + jsonstring);

                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                WebResponse webResponse = webRequest.GetResponse();

                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                var expConverter = new ExpandoObjectConverter();
                dynamic json = JsonConvert.DeserializeObject<ExpandoObject>(getResp, expConverter);

                List<MasternodeModel> masternodes = new List<MasternodeModel>();
                foreach (var mn in json.result)
                {
                    var data = mn.ToString();
                    string[] substrings = Regex.Split(data, "\\s+");    // Split on hyphens

                    MasternodeModel masternode = new MasternodeModel();
                    if (substrings[1] == "ENABLED" || substrings[1] == "POSE_BANNED")
                    {
                        masternode.Status = substrings[1];
                        masternode.Payee = substrings[2];
                        masternode.LastSeen = substrings[3];
                        masternode.ActiveSeconds = substrings[3];
                        masternode.IPAddress = substrings[5];
                        masternode.Identity = "https://" + substrings[6].Trim(']');
                        masternodes.Add(masternode);
                    }
                }
                var json1 = JsonConvert.SerializeObject(masternodes);

                return Json(json1);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }

        }

        [HttpGet]
        public JsonResult GetMasterNodesSetup()
        {
            try
            {
                // Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";

                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"masternodelist\", \"params\": [\"full\"] }";
                System.Diagnostics.Debug.WriteLine("GetMasterNodes: " + jsonstring);

                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                using (Stream dataStream = webRequest.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }

                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    using (StreamReader sr = new StreamReader(webResponse.GetResponseStream()))
                    {
                        string getResp = sr.ReadToEnd();
                        var expConverter = new ExpandoObjectConverter();
                        dynamic json = JsonConvert.DeserializeObject<ExpandoObject>(getResp, expConverter);

                        List<MasternodeModel> masternodes = new List<MasternodeModel>();
                        foreach (var mn in json.result)
                        {
                            var data = mn.ToString();
                            string[] substrings = Regex.Split(data, "\\s+");  // Split on spaces

                            if (substrings[1] == "ENABLED")
                            {
                                MasternodeModel masternode = new MasternodeModel
                                {
                                    Status = substrings[1],
                                    Payee = substrings[2],
                                    LastSeen = substrings[3],
                                    ActiveSeconds = substrings[3],
                                    IPAddress = substrings[5],
                                    Identity = "https://" + substrings[6].Trim(']')
                                };
                                masternodes.Add(masternode);
                            }
                        }

                        // Constructing the response object with a success flag
                        var responseObj = new
                        {
                            success = true,
                            data = masternodes
                        };
                        return Json(responseObj);  // Serializing the response object directly
                    }
                }
            }
            catch (Exception ex)
            {
                var prepRespJson = new { success = false, error = ex.Message };
                return Json(prepRespJson);
            }
        }



        [HttpGet]
        public async Task<JsonResult> TestMasterNode(string Identity)
        {
            try
            {
                string cleanIdentity = Identity.Replace("https://", "");
                long PingTime = PingMasternode(cleanIdentity);
                bool HTTPSEnabled = CheckHttpsAvailability("https://" + Identity);

                var prepRespJson = new { pingtime = PingTime, identity = Identity, httpsenabled = HTTPSEnabled, success = true };
                return Json(prepRespJson);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }
        }

        [HttpGet]
        public JsonResult CheckMasternodeSetup()
        {
            string IPFSHost = "";
            try
            {
                using (var conn = new SqliteConnection("Data Source=basex.db"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT IPFSHost from basexConfiguration WHERE Id = 1";

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                IPFSHost = rdr.GetString(rdr.GetOrdinal("IPFSHost"));
                            }
                        }
                    }
                }
                if(IPFSHost != "127.0.0.1")
				{
                    dynamic prepRespJson = JObject.Parse("{success: true}");
                    return Json(prepRespJson);
                } else
				{
                    dynamic prepRespJson = JObject.Parse("{success: false}");
                    return Json(prepRespJson);
                }
            }
            catch (Exception ex)
            {
                dynamic prepBadRespJson = JObject.Parse("{success: false}");
                return Json(prepBadRespJson);
            }


        }


        public static async Task<string> GetPublicIPAddress()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    string url = "https://httpbin.org/ip";
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        var ipInfo = Newtonsoft.Json.Linq.JObject.Parse(json);
                        return ipInfo["origin"].ToString();
                    }
                    return "Unable to get public IP";
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }
        }

        private long PingMasternode(string ipAddress)
        {
            try
            {
				System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
				System.Net.NetworkInformation.PingReply reply = ping.Send(ipAddress, 1000); // 1 second timeout
                if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                {
                    return reply.RoundtripTime;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions or unreachable hosts
                return 999; // Return max value if ping fails
            }
            return 999; // Return max value if ping fails
        }

        private bool CheckHttpsAvailability(string url)
        {
            try
            {
                string fullUrl = url + "/ipfs/Qmd76KSvQn51VpsputPNGgdpAQsd73E5ZRxqjhtBsrGS6b/index.html";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullUrl);
                request.Timeout = 5000; 
                request.Method = "GET"; 
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (Exception ex)
            {
                return false; 
            }
        }


        [HttpGet]
        public JsonResult DeleteRegisteredMasternode(string id)
        {
            try
            {
                using (var conn = new SqliteConnection("Data Source=basex.db"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "DELETE from masternodeprivatekeys WHERE Id = @Id";
                        cmd.Parameters.AddWithValue("Id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                dynamic prepBadRespJson = JObject.Parse("{success: false}");
                return Json(prepBadRespJson);
            }

            dynamic prepRespJson = JObject.Parse("{success: true}");
            return Json(prepRespJson);
        }

        public class SignMessageModel
        {
            public string Message { get; set; }
            public string PrivateKey { get; set; }
        }


        [HttpPost]
        public JsonResult SignMessage([FromBody] SignMessageModel model)
        {
            string message = model.Message;
            string privateKey = model.PrivateKey;
            try
            {
                //Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";

                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"signmessagewithprivkey\", \"params\": [\"" + privateKey + "\", \"" + message + "\"] }";
                System.Diagnostics.Debug.WriteLine("SignMessage: " + jsonstring);
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                WebResponse webResponse = webRequest.GetResponse();

                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                dynamic prepRespJson = JObject.Parse(getResp);
                return Json(prepRespJson);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }
        }

        [HttpPost]
        public JsonResult SubmitMasternodeVote(string voteData)
        {
            Console.WriteLine("SubmitMasternodeVote:: " + voteData);
            bool skipToNextVd = false;
            try
            {

                var voteDataList = JsonConvert.DeserializeObject<List<dynamic>>(voteData);
                foreach (var vd in voteDataList)
                {

                    HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                    webRequest.Credentials = new NetworkCredential(_userName, _password);
                    webRequest.ContentType = "application/json-rpc";
                    webRequest.Method = "POST";
                    webRequest.Timeout = 5000;
                    string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [";
                    jsonstring += "\"getcurrentvotes\",";
                    jsonstring += $"\"{vd.parentHash}\"";
                    jsonstring += "]}";

                    byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                    webRequest.ContentLength = byteArray.Length;
                    Stream dataStream = webRequest.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();

                    WebResponse webResponse = webRequest.GetResponse();
                    StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                    string getResp = sr.ReadToEnd();
                    Debug.WriteLine("getCurrentVotes: ", getResp);

                    JObject obj = JObject.Parse(getResp);

                    string CollateralHash = vd.vinMasternode.ToString();
                    string Signal = vd.voteOutcome.ToString();
                    string Govhash = vd.parentHash.ToString();
                    string VoteTime = vd.time.ToString();

                    var voteOutcome = "yes";
                    switch (vd.voteOutcome.ToString())
                    {
                        case "2": voteOutcome = "no"; break;
                        case "3": voteOutcome = "abstain"; break;
                    }

                    foreach (var x in obj)
                    {
                        string name = x.Key;
                        JToken value = x.Value;

                        if (name == "result")
                        {
                            JObject mn = JObject.Parse(x.Value.ToString());
                            foreach (JProperty y in (JToken)mn)
                            {
                                string key = y.Name;
                                JToken mnResult = y.Value;

                                var res = mnResult.ToString().Split(":");
                                string mnVin = res[0].Split("-")[0];
                                string voteTime = res[1];
                                string voteSignal = res[2];

                                if (vd.vinMasternode == mnVin)
                                {
                                    long voteEpoch = DateTimeOffset.Now.ToUnixTimeSeconds();
                                    long startEpoch = long.Parse(voteTime);
                                    long elasped = voteEpoch - startEpoch;

                                    if (voteSignal == voteOutcome)
                                    {
                                        skipToNextVd = true;
                                        var resp = new { Success = false, Message = "You have already cast your vote as '" + voteOutcome + "'" };
      
                                    }

                                    if (elasped < (60 * 60))
                                    {
                                        var resp = new { Success = false, Message = "You are voting too quickly. Please only vote once per proposal or record per hour" };
                                        return Json(resp);
                                    }
                                }

                                Debug.WriteLine("MNVin: " + mnVin + "; voteTime: " + voteTime);
                            }
                        }
                    }


                    if (skipToNextVd)
                    {
                        continue;
                    }

                    try
                    {
                        //Prepare call
                        HttpWebRequest webRequest1 = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                        webRequest1.Credentials = new NetworkCredential(_userName, _password);
                        webRequest1.ContentType = "application/json-rpc";
                        webRequest1.Method = "POST";
                        webRequest1.Timeout = 5000;

                        jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"voteraw\", \"params\": [";
                        jsonstring += $"\"{vd.vinMasternode}\",";
                        jsonstring += $"{vd.collateralIndex},";
                        jsonstring += $"\"{vd.parentHash}\",";
                        jsonstring += $"\"funding\",";
                        jsonstring += $"\"{voteOutcome}\",";
                        jsonstring += $"{vd.time},";
                        jsonstring += $"\"{vd.signature}\"";
                        jsonstring += "]}";

                        // serialize json for the request
                        byteArray = Encoding.UTF8.GetBytes(jsonstring);
                        webRequest1.ContentLength = byteArray.Length;
                        dataStream = webRequest1.GetRequestStream();
                        dataStream.Write(byteArray, 0, byteArray.Length);
                        dataStream.Close();

                        webResponse = webRequest1.GetResponse();
                        sr = new StreamReader(webResponse.GetResponseStream());
                        getResp = sr.ReadToEnd();
                        Debug.WriteLine("submit voteRaw Response: ", getResp);
                    }
                    catch (Exception ex)
                    {
                        var rep = new { Success = false, Error = ex.ToString() };
                        Console.WriteLine("SubmitMasternodeVote::Failed Voting ");
                        return Json(rep);
                    }
                }
                var prepRespJson = new { Success = true };
                return Json(prepRespJson);
            }
            catch (Exception ex)
            {
                //This Error Message maybe not be true, but for most cases it is true.
                var prepRespJson = new { Success = false, Message = "This record or proposal is outside of the valid vote date range and will be deleted soon. For records, you must vote before next superblock." };
                return Json(prepRespJson);
            }
        }

        [HttpGet]
        public JsonResult GetMasternodePrivKey()
        {
            try
            {
                List<AddUpdateMasternode> masterNodes = new List<AddUpdateMasternode>();

                using (var conn = new SqliteConnection("Data Source=basex.db"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT * FROM masternodeprivatekeys";

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                AddUpdateMasternode mn = new AddUpdateMasternode();
                                mn.Id = rdr.GetInt16(rdr.GetOrdinal("Id"));
                                mn.CollateralHash = rdr.GetString(rdr.GetOrdinal("collateralHash"));
                                mn.CollateralIndex = rdr.GetInt16(rdr.GetOrdinal("collateralIndex"));
                                mn.PrivateKey = !rdr.IsDBNull(rdr.GetOrdinal("EncryptedPrivateKey")) ? rdr.GetString(rdr.GetOrdinal("EncryptedPrivateKey")) : string.Empty;
                                masterNodes.Add(mn);
                            }
                        }
                    }
                }
                return Json(masterNodes);
            }
            catch (Exception ex)
            {
                dynamic prepBadRespJson = JObject.Parse("{success: false}");
                return Json(prepBadRespJson);
            }
        }

        [HttpPost]
        public JsonResult StoreEncryptedPrivateKeys([FromBody]AddUpdateMasternode request)
        {
            try
            {

                using (var conn = new SqliteConnection("Data Source=basex.db"))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "UPDATE masternodeprivatekeys SET EncryptedPrivateKey = @EncryptedPrivateKey WHERE Id = @Id";
                        cmd.Parameters.AddWithValue("Id", request.Id);
                        cmd.Parameters.AddWithValue("EncryptedPrivateKey", request.PrivateKey);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                dynamic prepBadRespJson = JObject.Parse("{success: false}");
                return Json(prepBadRespJson);
            }

            dynamic prepRespJson = JObject.Parse("{success: true}");
            return Json(prepRespJson);
        }
    
        [HttpGet]
        public JsonResult GetMyMasternodes()
        {
            try
            {
                List<AddUpdateMasternode> masterNodes = new List<AddUpdateMasternode>();

                using (var conn = new SqliteConnection("Data Source=basex.db"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT * FROM masternodeprivatekeys";


                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                AddUpdateMasternode mn = new AddUpdateMasternode();
                                mn.Id = rdr.GetInt16(rdr.GetOrdinal("Id"));
                                mn.CollateralHash = rdr.GetString(rdr.GetOrdinal("collateralHash"));
                                mn.CollateralIndex = rdr.GetInt16(rdr.GetOrdinal("collateralIndex"));
                                mn.Name = rdr.GetString(rdr.GetOrdinal("masternodeName"));
                                masterNodes.Add(mn);
                            }
                        }
                    }
                }
                return Json(masterNodes);

            }
            catch (Exception ex)
            {
                dynamic prepBadRespJson = JObject.Parse("{success: false}");
                return Json(prepBadRespJson);
            }
        }

        [HttpPost]
        public JsonResult AddUpdateMasternodeRegistration([FromBody]AddUpdateMasternode request)
        {
            try
            {

                using (var conn = new SqliteConnection("Data Source=basex.db"))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "INSERT INTO masternodeprivatekeys (collateralIndex, masternodeName, collateralHash) VALUES (@CollateralIndex, @MasternodeName, @CollateralHash)";
                        cmd.Parameters.AddWithValue("CollateralHash", request.CollateralHash);
                        cmd.Parameters.AddWithValue("CollateralIndex", request.CollateralIndex);
                        cmd.Parameters.AddWithValue("MasternodeName", request.Name);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                dynamic prepBadRespJson = JObject.Parse("{success: false}");
                return Json(prepBadRespJson);
            }

            dynamic prepRespJson = JObject.Parse("{success: true}");
            return Json(prepRespJson);
        }

        public class AddUpdateMasternode
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string CollateralHash { get; set; }
            public string PrivateKey { get; set; }
            public bool Notify { get; set; }
            public int CollateralIndex { get; set; }

        }
    }
}
