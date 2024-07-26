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
using Microsoft.IdentityModel.Tokens;
using MySqlX.XDevAPI.Common;
using Microsoft.DotNet.MSIdentity.Shared;
using System.Data.Common;
using Microsoft.AspNetCore.SignalR.Protocol;
using PuppeteerSharp.Media;
using System.Security.Policy;
using AngleSharp.Dom;
using HistWeb.Utilties;
using Renci.SshNet;
using System.Net.Sockets;
using System.Net.Security;
using System.Data;
using Humanizer;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Org.BouncyCastle.Utilities.Net;
using Org.BouncyCastle.Utilities;
using MySqlX.XDevAPI;
using System.Security.Cryptography.X509Certificates;

namespace HistWeb.Controllers
{
    [Area("Masternode")]
    public class MasternodeController : Controller
    {
        private string _rpcServerUrl = "http://" + ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;
        private string _userName = ApplicationSettings.HistoriaRPCUserName;
        private string _password = ApplicationSettings.HistoriaRPCPassword;
        private IConfiguration _configuration = null;

        private static bool isRunning = false;
        private static readonly object lockObject = new object();
        private readonly DatabaseHelper _StepHelper;

        public MasternodeController(IConfiguration iConfig)
        {
            _configuration = iConfig;
            _rpcServerUrl = "http://" + ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;
            _userName = ApplicationSettings.HistoriaRPCUserName;
            _password = ApplicationSettings.HistoriaRPCPassword;
            string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
            _StepHelper = new DatabaseHelper(connectionString);
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
                byte[] byteArray1 = Encoding.UTF8.GetBytes(jsonstring);
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
                jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"getsuperblockbudget\", \"params\": [" + nextsuperblock + "] }";
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
                string totalPassingBudget = "0";
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
        public JsonResult GetIPFSObjectSize()
        {
            string block = "";

            try
            {
                // Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"spork\", \"params\": [\"show\"] }";
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                var Response = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);

                JObject jsonResponse = JObject.Parse(getResp);
                string size = "";
                if (jsonResponse.ContainsKey("result"))
                {
                    var result = jsonResponse["result"];
                    if (result != null)
                    {
                        size = result["SPORK_102_IPFS_OBJECT_SIZE"].ToString();

                    }
                }
                 
                //return Response["SPORK_102_IPFS_OBJECT_SIZE"]?.Value<string>() ?? -1;
                var prepRespJson1 = new { Success = true, size = size };
                return Json(prepRespJson1);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, };
                return Json(prepRespJson1);
            }
        }


        [HttpPost]
        public JsonResult UnlockWallet([FromBody] PassphraseModel model)
        {
            lockWallet();
            string passphrase = model.Passphrase;
            string time = model.Time;
            
            try
            {
                Logger.Instance.Log("UnlockWallet", "Attempting to Unlock Wallet", "WALLET");
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                webRequest.Timeout = 5000;
                string jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"walletpassphrase\", \"params\": [\"{passphrase}\", {time}] }}";
                Console.WriteLine(jsonstring);
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                var prepRespJson = new { success = true };
                return Json(prepRespJson);
            }
            catch (Exception ex)
            {
                var prepRespJson = new { success = false, error = "Invalid Passphrase or Wallet is Not Encrypted (Please encrypt the wallet first)" };
                return Json(prepRespJson);
            }
        }

        [HttpGet]
        public JsonResult IsUnlockWallet()
        {
            try
            {
                Logger.Instance.Log("IsUnlockWallet", "Checking to see if Wallet is unlocked", "WALLET");
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                webRequest.Timeout = 5000;
                string jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"getwalletinfo\", \"params\": [] }}";
                Console.WriteLine(jsonstring);
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);
                string unlocked_until = jsonResponse["result"]["unlocked_until"].ToString();
                if (unlocked_until == "0")
                {
                    Logger.Instance.Log("IsUnlockWallet", "Wallet is not unlocked or it is not encrypted", "WALLET");
                    var prepRespJson = new { success = false };
                    return Json(prepRespJson);
                }
                else
                {
                    //Logger.Instance.Log("IsUnlockWallet", "Wallet is unlocked", "VOTINGNODE");
                    var prepRespJson = new { success = true };
                    return Json(prepRespJson);
                }


            }
            catch (Exception ex)
            {
                Logger.Instance.Log("IsUnlockWallet", "Can't connect to Historia Core Wallet", "WALLET");
                var prepRespJson = new { success = false, error = "Something went wrong." };
                return Json(prepRespJson);
            }
        }
        private bool IsWalletUnlocked()
        {
            Logger.Instance.Log("IsWalletUnlocked", "Verify wallet is unlocked", "WALLET");
            try
            {

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                webRequest.Timeout = 5000;
                string jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"getwalletinfo\", \"params\": [] }}";
                Console.WriteLine(jsonstring);
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);
                string unlocked_until = jsonResponse["result"]["unlocked_until"].ToString();
                if (unlocked_until == "0")
                {
                    Logger.Instance.Log("IsWalletUnlocked", "Wallet is not unlocked or it is not encrypted", "WALLET");
                    return false;
                }
                else
                {
                    Logger.Instance.Log("IsWalletUnlocked", "Wallet is unlocked", "WALLET");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("IsWalletUnlocked", "Can't connect to Historia Core Wallet", "WALLET");
                return false;
            }
        }


        [HttpPost]
        public JsonResult RevokeNode([FromBody] CollateralHashModel model)
        {
            if (!IsWalletUnlocked())
            {
                var prepRespJson = new { success = false, error = "Wallet is Not Unlocked" };
                return Json(prepRespJson);
            }
            string blsprivkey = "", ProTXHash = "", feeSourceAddress = "";
            string collateralHash = model.CollateralHash;
            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT ProTXHash, blsprivkey,  feeSourceAddress from masternodeprivatekeys WHERE collateralhash = @collateralHash";
                        cmd.Parameters.AddWithValue("collateralHash", collateralHash);
                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                blsprivkey = rdr.GetString(rdr.GetOrdinal("blsprivkey"));
                                ProTXHash = rdr.GetString(rdr.GetOrdinal("ProTXHash"));
                                feeSourceAddress = rdr.GetString(rdr.GetOrdinal("feeSourceAddress"));
                            }
                        }
                    }
                }

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                webRequest.Timeout = 5000;

                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"protx\", \"params\": [\"revoke\", \"" + ProTXHash + "\", \"" + blsprivkey + "\", \"0\", \"" + feeSourceAddress + "\"] }";
                Console.WriteLine(jsonstring);
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "DELETE from masternodeprivatekeys WHERE collateralhash = @collateralHash";
                        cmd.Parameters.AddWithValue("collateralHash", collateralHash);
                        cmd.ExecuteNonQuery();
                    }
                }

                lockWallet();
                var prepRespJson = new { success = true };
                return Json(prepRespJson);

            }
            catch (Exception ex)
            {
                lockWallet();
                var prepRespJson = new { success = false, error = "Invalid Passphrase or Wallet is Not Encrypted (Please encrypt the wallet first)" };
                return Json(prepRespJson);

            }
        }


        [HttpGet]
        public JsonResult NodeQueue(string filter)
        {
            int cnt = 0;
            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = $@"SELECT count(*) as cnt from masternodesetupqueue WHERE nodeType = @nodeType";
                        cmd.Parameters.AddWithValue("nodeType", filter);
                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                cnt = rdr.GetInt32(rdr.GetOrdinal("cnt"));
                            }
                        }
                    }
                }
                if (cnt == 0)
                {
                    var prepRespJson = new { success = false, error = "" };
                    return Json(prepRespJson);
                }
                else
                {
                    var prepRespJson = new { success = true, error = "" };
                    return Json(prepRespJson);
                }

            }
            catch (Exception ex)
            {
                var prepRespJson = new { success = false, error = "Something Went Wrong" };
                return Json(prepRespJson);

            }
        }


        [HttpGet]
        public JsonResult DeleteNodeQueue()
        {
            int cnt = 0;
            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "DELETE from masternodesetupqueue";
                        cmd.ExecuteNonQuery();
                    }
                }
                if (cnt == 0)
                {
                    var prepRespJson = new { success = false, error = "" };
                    return Json(prepRespJson);
                }
                else
                {
                    var prepRespJson = new { success = true, error = "" };
                    return Json(prepRespJson);
                }

            }
            catch (Exception ex)
            {
                var prepRespJson = new { success = false, error = "Something Went Wrong" };
                return Json(prepRespJson);

            }
        }


        [HttpPost]
        public JsonResult RestartMasterNode([FromBody] CollateralHashModel model)
        {
            if (!IsWalletUnlocked())
            {
                var prepRespJson = new { success = false, error = "Wallet is Not Unlocked" };
                return Json(prepRespJson);
            }


            string blsprivkey = "", ProTXHash = "", feeSourceAddress = "", masternodeName = "";
            string collateralHash = model.CollateralHash;

            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT ProTXHash, blsprivkey,  feeSourceAddress, masternodeName from masternodeprivatekeys WHERE collateralhash = @collateralHash";
                        cmd.Parameters.AddWithValue("collateralHash", collateralHash);
                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                blsprivkey = rdr.GetString(rdr.GetOrdinal("blsprivkey"));
                                ProTXHash = rdr.GetString(rdr.GetOrdinal("ProTXHash"));
                                feeSourceAddress = rdr.GetString(rdr.GetOrdinal("feeSourceAddress"));
                                masternodeName = rdr.GetString(rdr.GetOrdinal("masternodeName"));
                            }
                        }
                    }
                }
                string ipAndPort = GetIpAddress(masternodeName);

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                webRequest.Timeout = 5000;

                string EmptyPlaceHolder = ""; // This is needed for command format.

                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"protx\", \"params\": [\"update_service\", \"" + ProTXHash + "\", \"" + ipAndPort + ":10101" + "\", \"" + blsprivkey + "\", \"" + EmptyPlaceHolder + "\", \"" + feeSourceAddress + "\"]   }";

                Console.WriteLine(jsonstring);
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                lockWallet();
                var prepRespJson = new { success = true };
                return Json(prepRespJson);

            }
            catch (Exception ex)
            {
                lockWallet();
                var prepRespJson = new { success = false, error = "Invalid Passphrase or Wallet is Not Encrypted (Please encrypt the wallet first)" };
                return Json(prepRespJson);

            }
        }

        private void lockWallet()
        {
            Logger.Instance.Log("lockWallet", "Locked Wallet", "WALLET");
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                webRequest.Timeout = 5000;
                string jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"walletlock\", \"params\": [] }}";

                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

            }
            catch (Exception ex)
            {
                var prepRespJson = new { success = false, error = "Something went wrong" };
            }
        }

        public bool GenNodeAddresses(string nodeType)
        {

            if (!IsWalletUnlocked())
            {
                return false;
            }
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try
            {
                Logger.Instance.Log("GenNodeAddresses", "Generating Required addresses", $"{nodeType}");
                string collateralAddr = GetNewAddress();
                string ownerKeyAddr = GetNewAddress();
                string votingKeyAddr = GetNewAddress();
                string payoutAddress = GetNewAddress();
                string feeSourceAddress = GetNewAddress();
                var (blsprivkey, blspublickey) = blsgenerate();
                Logger.Instance.Log(
                    "GenNodeAddresses",
                    $"Collateral Address: {collateralAddr}, Owner Key Address: {ownerKeyAddr}, Voting Key Address: {votingKeyAddr}, Payout Address: {payoutAddress}, Fee Source Address: {feeSourceAddress}, BLS Public Key: {blspublickey}, BLS Private Key: {blsprivkey}", $"{nodeType}" );

                if (!String.IsNullOrEmpty(collateralAddr) && !String.IsNullOrEmpty(ownerKeyAddr) && !String.IsNullOrEmpty(collateralAddr) && !String.IsNullOrEmpty(votingKeyAddr) && !String.IsNullOrEmpty(payoutAddress) && !String.IsNullOrEmpty(feeSourceAddress) && !String.IsNullOrEmpty(blsprivkey) && !String.IsNullOrEmpty(blspublickey))
                {
                    string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                    using (var conn = new SqliteConnection(connectionString))
                    {
                        conn.Open();

                        using (var cmd = conn.CreateCommand())
                        {
                            conn.Open();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = @"UPDATE masternodesetupqueue 
                                SET collateralAddr = @collateralAddr, 
                                    ownerKeyAddr = @ownerKeyAddr, 
                                    votingKeyAddr = @votingKeyAddr, 
                                    payoutAddress = @payoutAddress, 
                                    feeSourceAddress = @feeSourceAddress, 
                                    blsprivkey = @blsprivkey, 
                                    blspublickey = @blspublickey";

                            cmd.Parameters.AddWithValue("@collateralAddr", collateralAddr);
                            cmd.Parameters.AddWithValue("@ownerKeyAddr", ownerKeyAddr);
                            cmd.Parameters.AddWithValue("@votingKeyAddr", votingKeyAddr);
                            cmd.Parameters.AddWithValue("@payoutAddress", payoutAddress);
                            cmd.Parameters.AddWithValue("@feeSourceAddress", feeSourceAddress);
                            cmd.Parameters.AddWithValue("@blsprivkey", blsprivkey);
                            cmd.Parameters.AddWithValue("@blspublickey", blspublickey);
                            cmd.ExecuteNonQuery();
                            Logger.Instance.Log("GenNodeAddresses", "Saving to masternodesetupqueue table", $"{nodeType}");
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("GenNodeAddresses", "Something failed. Is Historia Core Wallet Running?", $"{nodeType}");
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool SendCollateralForVotingNode()
        {
            if (!IsWalletUnlocked())
            {
                return false;
            }
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            string txid = "";
            string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
            Logger.Instance.Log("SendCollateralForVotingNode", "Attempting to send collateral for node", "VOTINGNODE");
            try
            {
                string collateralAddr = "", feeSourceAddr = "";
                int nodeType = 0;
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT collateralAddr, feeSourceAddress, nodeType from masternodesetupqueue";
                        cmd.ExecuteNonQuery();

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                collateralAddr = rdr.GetString(rdr.GetOrdinal("collateralAddr"));
                                feeSourceAddr = rdr.GetString(rdr.GetOrdinal("feeSourceAddress"));
                                nodeType = rdr.GetInt32(rdr.GetOrdinal("nodeType"));
                            }
                        }
                    }
                }


                // Send collateral
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"sendtoaddress\", \"params\": [\"{collateralAddr}\", \"{nodeType}\"] }}";
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);
                txid = jsonResponse["result"].ToString();
                Logger.Instance.Log("SendCollateralForVotingNode", $"Sent {nodeType} HTA to your address {collateralAddr} with TXID: {txid}", "VOTINGNODE");

                // Send some for fee source
                webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"sendtoaddress\", \"params\": [\"{feeSourceAddr}\", 1] }}";
                byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                webResponse = webRequest.GetResponse();
                sr = new StreamReader(webResponse.GetResponseStream());
                getResp = sr.ReadToEnd();
                Logger.Instance.Log("SendCollateralForVotingNode", $"Sent 1 HTA to your address {feeSourceAddr}", "VOTINGNODE");

            }
            catch (Exception ex)
            {
                Logger.Instance.Log("SendCollateralForVotingNode", "Something failed. Is Historia Core Wallet Running?", "VOTINGNODE");
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
            }


            string collateralIndex = GetCollateralIndex(txid);
            if(string.IsNullOrEmpty(collateralIndex))
            {
                Logger.Instance.Log("SendCollateralForVotingNode", $"Failed to getcollateralIndex", "VOTINGNODE");
                return false;
            }
            try
            {
                if (!string.IsNullOrEmpty(txid))
                {

                    using (var conn = new SqliteConnection(connectionString))
                    {
                        conn.Open();

                        using (var cmd = conn.CreateCommand())
                        {
                            conn.Open();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = @"UPDATE masternodesetupqueue 
                                SET collateralHash = @collateralHash, collateralIndex = @collateralIndex";

                            cmd.Parameters.AddWithValue("@collateralHash", txid);
                            cmd.Parameters.AddWithValue("@collateralIndex", collateralIndex);
                            cmd.ExecuteNonQuery();
                            Logger.Instance.Log("SendCollateralForVotingNode", $"Saving to collateralHash TXID: {txid} and collateralIndex:{collateralIndex} to masternodesetupqueue table", "VOTINGNODE");
                        }
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("SendCollateralForVotingNode", $"Failed to update masternodesetupqueue table", "VOTINGNODE");
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool SendCollateralForMasternode()
        {
            if (!IsWalletUnlocked())
            {
                return false;
            }
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            string txid = "";
            string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
            Logger.Instance.Log("SendCollateralForMasternode", "Attempting to send collateral for node", "MASTERNODE");
            try
            {
                string collateralAddr = "", feeSourceAddr = "";
                int nodeType = 0;
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT collateralAddr, feeSourceAddress, nodeType from masternodesetupqueue";
                        cmd.ExecuteNonQuery();

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                collateralAddr = rdr.GetString(rdr.GetOrdinal("collateralAddr"));
                                feeSourceAddr = rdr.GetString(rdr.GetOrdinal("feeSourceAddress"));
                                nodeType = rdr.GetInt32(rdr.GetOrdinal("nodeType"));
                            }
                        }
                    }
                }


                // Send collateral
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"sendtoaddress\", \"params\": [\"{collateralAddr}\", \"{nodeType}\"] }}";
                Logger.Instance.Log("SendCollateralForMasternode", "Command to send collateral for node" + jsonstring, "MASTERNODE");
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);
                txid = jsonResponse["result"].ToString();
                Logger.Instance.Log("SendCollateralForMasternode", $"Sent {nodeType} HTA to your address {collateralAddr} with TXID: {txid}", "MASTERNODE");

                // Send some for fee source
                webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"sendtoaddress\", \"params\": [\"{feeSourceAddr}\", 1] }}";
                Logger.Instance.Log("SendCollateralForMasternode", "Command to send collateral fee for node" + jsonstring, "MASTERNODE");
                byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                webResponse = webRequest.GetResponse();
                sr = new StreamReader(webResponse.GetResponseStream());
                getResp = sr.ReadToEnd();
                Logger.Instance.Log("SendCollateralForMasternode", $"Sent 1 HTA to your address {feeSourceAddr}", "MASTERNODE");
                Logger.Instance.Log("SendCollateralForMasternode", "Completed sending collateral for node", "MASTERNODE");

            }
            catch (Exception ex)
            {
                Logger.Instance.Log("SendCollateralForMasternode", "Something failed. Is Historia Core Wallet Running?", "MASTERNODE");
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
            }


            string collateralIndex = GetCollateralIndex(txid);
            if (string.IsNullOrEmpty(collateralIndex))
            {
                Logger.Instance.Log("SendCollateralForVotingNode", $"Failed to getcollateralIndex", "VOTINGNODE");
                return false;
            }

            try
            {
                if (!string.IsNullOrEmpty(txid))
                {

                    using (var conn = new SqliteConnection(connectionString))
                    {
                        conn.Open();

                        using (var cmd = conn.CreateCommand())
                        {
                            conn.Open();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = @"UPDATE masternodesetupqueue 
                                SET collateralHash = @collateralHash, collateralIndex = @collateralIndex";

                            cmd.Parameters.AddWithValue("@collateralHash", txid);
                            cmd.Parameters.AddWithValue("@collateralIndex", collateralIndex);
                            cmd.ExecuteNonQuery();
                            Logger.Instance.Log("SendCollateralForMasternode", $"Saving to collateralHash TXID: {txid} and collateralIndex:{collateralIndex} to masternodesetupqueue table", "MASTERNODE");
                        }
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("SendCollateralForMasternode", $"Failed to update masternodesetupqueue table", "MASTERNODE");
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool WaitOneBlock(string nodeType)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }

            try
            {
                Logger.Instance.Log("WaitOneBlock", "Waiting on one block to pass", nodeType);
                int block0 = Int32.Parse(GetBlock());
                int block1 = 0;

                while (block1 <= block0)
                {
                    block1 = Int32.Parse(GetBlock());
                    Logger.Instance.Log("WaitOneBlock", $"Current Block: {block0} Waiting for Next Block: {block0 + 1}", nodeType);
                    System.Threading.Thread.Sleep(15000);
                }
                Logger.Instance.Log("WaitOneBlock", $"One block has passed.", nodeType);

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("WaitOneBlock", "Something failed. Is Historia Core Wallet Running?", nodeType);
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool ProTX_register_prepare_votingnode()
        {
            if (!IsWalletUnlocked())
            {
                return false;
            }
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            Logger.Instance.Log("ProTX register_prepare", "Attempting to run register_prepare command to start Voting node registration process", "VOTINGNODE");

            string tx = "", collateralAddress = "", signMessage = "";
            string collateralhash = "", collateralIndex = "", collateralAddr = "", ownerKeyAddr = "", blsPubKey = "", votingKeyAddr = "", payoutAddress = "", feeSourceAddress = "";
            int nodeType = 0;
            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT collateralHash, collateralIndex, collateralAddr, ownerKeyAddr, votingKeyAddr, payoutAddress, feeSourceAddress, blspublickey, nodeType from masternodesetupqueue";

                        cmd.ExecuteNonQuery();

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                collateralhash = rdr.GetString(rdr.GetOrdinal("collateralHash"));
                                collateralIndex = rdr.GetString(rdr.GetOrdinal("collateralIndex"));
                                collateralAddr = rdr.GetString(rdr.GetOrdinal("collateralAddr"));
                                ownerKeyAddr = rdr.GetString(rdr.GetOrdinal("ownerKeyAddr"));
                                blsPubKey = rdr.GetString(rdr.GetOrdinal("blspublickey"));
                                votingKeyAddr = rdr.GetString(rdr.GetOrdinal("votingKeyAddr"));
                                payoutAddress = rdr.GetString(rdr.GetOrdinal("payoutAddress"));
                                feeSourceAddress = rdr.GetString(rdr.GetOrdinal("feeSourceAddress"));
                                nodeType = rdr.GetInt32(rdr.GetOrdinal("nodeType"));

                            }
                        }
                    }
                }

                string randomIdentity = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", 30).Select(s => s[new Random().Next(s.Length)]).ToArray());
                // Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"protx\", \"params\": [\"register_prepare\", \"" + collateralhash + "\",   \"" + collateralIndex + "\",  \"VOTER\", \"" + ownerKeyAddr + "\", \"" + blsPubKey + "\",  \"" + votingKeyAddr + "\",  \"0\", \"" + payoutAddress + "\", \"0\", \"" + randomIdentity + "\", \"" + feeSourceAddress + "\"] }";
                Logger.Instance.Log("ProTX register_prepare", jsonstring, "VOTINGNODE");

                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);
                tx = jsonResponse["result"]["tx"].ToString();
                signMessage = jsonResponse["result"]["signMessage"].ToString(); ;

                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = @"UPDATE masternodesetupqueue 
                        SET register_prepare_TXID = @register_prepare_TXID, register_prepare_SignMessage = @signMessage, randomIdentity = @randomIdentity";
                        cmd.Parameters.AddWithValue("@register_prepare_TXID", tx);
                        cmd.Parameters.AddWithValue("@signMessage", signMessage);
                        cmd.Parameters.AddWithValue("@randomIdentity", randomIdentity);

                        cmd.ExecuteNonQuery();
                        Logger.Instance.Log("ProTX register_prepare", $"UPDATE masternodesetupqueue register_prepare_TXID:{tx}, signmessage:{signMessage}, randomIdentity:{randomIdentity}", "VOTINGNODE");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("ProTX register_prepare", $"Something went wrong. Is Historia Core Wallet running? Exception: "+ ex.Message, "VOTINGNODE");
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool ProTX_register_submit_votingnode()
        {
            if (!IsWalletUnlocked())
            {
                return false;
            }
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            Logger.Instance.Log("ProTX_register_submit", "Attempting to run register_submit command to continue Voting node registration process", "VOTINGNODE");

            string tx = "", collateralAddress = "", signMessage = "";
            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT collateralAddr, register_prepare_TXID, register_prepare_SignMessage from masternodesetupqueue";

                        cmd.ExecuteNonQuery();

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                collateralAddress = rdr.GetString(rdr.GetOrdinal("collateralAddr"));
                                tx = rdr.GetString(rdr.GetOrdinal("register_prepare_TXID"));
                                signMessage = rdr.GetString(rdr.GetOrdinal("register_prepare_SignMessage"));

                            }
                        }
                    }
                }

                string txid = "";
                string signature = SignMessage(collateralAddress, signMessage);
                Logger.Instance.Log("ProTX_register_submit", $"Generate Signature from collateraladdress:{collateralAddress} and signed message:{signMessage}, signature:{signature}", "VOTINGNODE");

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"protx\", \"params\": [\"register_submit\", \"" + tx + "\",   \"" + signature + "\"] }";
                Logger.Instance.Log("ProTX_register_submit", jsonstring, "VOTINGNODE");
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);
                txid = jsonResponse["result"].ToString();


                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = @"UPDATE masternodesetupqueue 
                        SET register_submit_TXID = @register_submit_TXID, register_prepare_SignMessage = @signMessage";
                        cmd.Parameters.AddWithValue("@register_submit_TXID", tx);
                        cmd.Parameters.AddWithValue("@signMessage", signMessage);
                        cmd.ExecuteNonQuery();
                        Logger.Instance.Log("ProTX_register_submit", $"UPDATE masternodesetupqueue: register_submit_TXID:{tx}, register_prepare_SignMessage:{signMessage}, ", "VOTINGNODE");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("ProTX_register_submit", "Something failed. Is Historia Core Wallet Running? Exception:" + ex.Message, "VOTINGNODE");
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool ProTX_register_prepare_masternode()
        {
            if (!IsWalletUnlocked())
            {
                return false;
            }
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            Logger.Instance.Log("ProTX register_prepare", "Attempting to run register_prepare command to start Masternode registration process", "MASTERNODE");

            string tx = "", collateralAddress = "", signMessage = "", MNIpAddr = "";
            string collateralhash = "", collateralIndex = "", collateralAddr = "", ownerKeyAddr = "", blsPubKey = "", votingKeyAddr = "", payoutAddress = "", feeSourceAddress = "", MNDNS = "", MNIPFS = "";
            int nodeType = 0;
            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT collateralHash, collateralIndex, collateralAddr, ownerKeyAddr, votingKeyAddr, payoutAddress, feeSourceAddress, blspublickey, nodeType, MN_DNS, MN_ipfs, sshAddr from masternodesetupqueue";

                        cmd.ExecuteNonQuery();

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                collateralhash = rdr.GetString(rdr.GetOrdinal("collateralHash"));
                                collateralIndex = rdr.GetString(rdr.GetOrdinal("collateralIndex"));
                                collateralAddr = rdr.GetString(rdr.GetOrdinal("collateralAddr"));
                                ownerKeyAddr = rdr.GetString(rdr.GetOrdinal("ownerKeyAddr"));
                                blsPubKey = rdr.GetString(rdr.GetOrdinal("blspublickey"));
                                votingKeyAddr = rdr.GetString(rdr.GetOrdinal("votingKeyAddr"));
                                payoutAddress = rdr.GetString(rdr.GetOrdinal("payoutAddress"));
                                feeSourceAddress = rdr.GetString(rdr.GetOrdinal("feeSourceAddress"));
                                nodeType = rdr.GetInt32(rdr.GetOrdinal("nodeType"));
                                MNDNS = rdr.GetString(rdr.GetOrdinal("MN_DNS"));
                                MNIPFS = rdr.GetString(rdr.GetOrdinal("MN_ipfs"));
                                MNIpAddr = rdr.GetString(rdr.GetOrdinal("sshAddr"));

                            }
                        }
                    }
                }
                MNIpAddr = MNIpAddr + ":10101";
                // Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"protx\", \"params\": [\"register_prepare\", \"" + collateralhash + "\",   \"" + collateralIndex + "\",  \""+ MNIpAddr + "\", \"" + ownerKeyAddr + "\", \"" + blsPubKey + "\",  \"" + votingKeyAddr + "\",  \"0\", \"" + payoutAddress + "\", \"" + MNIPFS + "\", \"" + MNDNS + "\", \"" + feeSourceAddress + "\"] }";
                Logger.Instance.Log("ProTX register_prepare", jsonstring, "MASTERNODE");

                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);
                tx = jsonResponse["result"]["tx"].ToString();
                signMessage = jsonResponse["result"]["signMessage"].ToString(); ;

                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = @"UPDATE masternodesetupqueue 
                        SET register_prepare_TXID = @register_prepare_TXID, register_prepare_SignMessage = @signMessage";
                        cmd.Parameters.AddWithValue("@register_prepare_TXID", tx);
                        cmd.Parameters.AddWithValue("@signMessage", signMessage);
                        cmd.ExecuteNonQuery();
                        Logger.Instance.Log("ProTX register_prepare", $"UPDATE masternodesetupqueue register_prepare_TXID:{tx}, signmessage:{signMessage}", "MASTERNODE");
                    }
                }
                Logger.Instance.Log("ProTX register_prepare", "Succuessful sent register_prepare command to start Masternode registration process", "MASTERNODE");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("ProTX register_prepare", $"Something went wrong. Is Historia Core Wallet running?", "MASTERNODE");
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool ProTX_register_submit_masternode()
        {
            if (!IsWalletUnlocked())
            {
                return false;
            }
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            Logger.Instance.Log("ProTX_register_submit", "Attempting to run register_submit command to continue masternode registration process", "MASTERNODE");

            string tx = "", collateralAddress = "", signMessage = "";
 
            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT collateralAddr, register_prepare_TXID, register_prepare_SignMessage from masternodesetupqueue";

                        cmd.ExecuteNonQuery();

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                collateralAddress = rdr.GetString(rdr.GetOrdinal("collateralAddr"));
                                tx = rdr.GetString(rdr.GetOrdinal("register_prepare_TXID"));
                                signMessage = rdr.GetString(rdr.GetOrdinal("register_prepare_SignMessage"));

                            }
                        }
                    }
                }

                string txid = "";
                string signature = SignMessage(collateralAddress, signMessage);
                Logger.Instance.Log("ProTX_register_submit", $"Generate Signature from collateraladdress:{collateralAddress} and signed message:{signMessage}, signature:{signature}", "MASTERNODE");

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"protx\", \"params\": [\"register_submit\", \"" + tx + "\",   \"" + signature + "\"] }";
                Logger.Instance.Log("ProTX_register_submit", jsonstring, "MASTERNODE");
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);
                txid = jsonResponse["result"].ToString();


                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = @"UPDATE masternodesetupqueue 
                        SET register_submit_TXID = @register_submit_TXID, register_prepare_SignMessage = @signMessage";
                        cmd.Parameters.AddWithValue("@register_submit_TXID", tx);
                        cmd.Parameters.AddWithValue("@signMessage", signMessage);
                        cmd.ExecuteNonQuery();
                        Logger.Instance.Log("ProTX_register_submit", $"UPDATE masternodesetupqueue: register_submit_TXID:{tx}, register_prepare_SignMessage:{signMessage}, ", "MASTERNODE");
                    }
                }
                Logger.Instance.Log("ProTX_register_submit", "Completed register_submit command", "MASTERNODE");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("ProTX_register_submit", "Something failed. Is Historia Core Wallet Running?", "MASTERNODE");
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool SaveMasternodePrivKey(string nodeType)
        {
            if (!IsWalletUnlocked())
            {
                return false;
            }
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }

            Logger.Instance.Log("SaveMasternodePrivKey", "Attempting to save voting key and setup HLWA voting privileges", nodeType);
            string votingKeyAddr = "", feeSourceAddress = "", collateralHash = "", collateralIndex = "", randomIdentity = "", blspublickey = "", blsprivkey = "";
   
            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT votingKeyAddr, feeSourceAddress, collateralHash, collateralIndex, randomIdentity, blsprivkey,  blspublickey, MN_DNS from masternodesetupqueue";

                        cmd.ExecuteNonQuery();

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                votingKeyAddr = rdr.GetString(rdr.GetOrdinal("votingKeyAddr"));
                                collateralHash = rdr.GetString(rdr.GetOrdinal("collateralHash"));
                                collateralIndex = rdr.GetString(rdr.GetOrdinal("collateralIndex"));
                                if(nodeType == "MASTERNODE") {
                                    randomIdentity = rdr.GetString(rdr.GetOrdinal("MN_DNS")); // Not actually random
                                } else
                                {
                                    randomIdentity = rdr.GetString(rdr.GetOrdinal("randomIdentity"));

                                }
                                blsprivkey = rdr.GetString(rdr.GetOrdinal("blsprivkey"));
                                blspublickey = rdr.GetString(rdr.GetOrdinal("blspublickey"));
                                feeSourceAddress = rdr.GetString(rdr.GetOrdinal("feeSourceAddress"));

                            }
                        }
                    }
                }

                string masternodeprivkey = getMasternodePrivKey(votingKeyAddr);
                Logger.Instance.Log("SaveMasternodePrivKey", $"Get Voting Node Voting Key to setup voting rights: REDACTED", nodeType);
                MasternodeModel masternode = new MasternodeModel();
                masternode = masternodelistFilterByJson(collateralHash, collateralIndex);
                if (!String.IsNullOrEmpty(masternodeprivkey))
                {
                    using (var conn = new SqliteConnection(connectionString))
                    {
                        conn.Open();

                        using (var cmd = conn.CreateCommand())
                        {
                            conn.Open();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = @"INSERT OR IGNORE INTO masternodeprivatekeys 
                                            (collateralIndex, masternodeName, collateralHash, EncryptedPrivateKey, blsprivkey, blspublickey, ProTXHash, feeSourceAddress) 
                                            VALUES 
                                            (@CollateralIndex, @MasternodeName, @CollateralHash, @EncryptedPrivateKey, @blsprivkey, @blspublickey, @ProTXHash, @feeSourceAddress)";
                            cmd.Parameters.AddWithValue("@CollateralHash", collateralHash);
                            cmd.Parameters.AddWithValue("@CollateralIndex", collateralIndex);
                            cmd.Parameters.AddWithValue("@MasternodeName", randomIdentity);
                            cmd.Parameters.AddWithValue("@EncryptedPrivateKey", masternodeprivkey);
                            cmd.Parameters.AddWithValue("@blsprivkey", blsprivkey);
                            cmd.Parameters.AddWithValue("@blspublickey", blspublickey);
                            cmd.Parameters.AddWithValue("@ProTXHash", masternode.ProTxHash);
                            cmd.Parameters.AddWithValue("@feeSourceAddress", feeSourceAddress);
                            cmd.ExecuteNonQuery();
                            Logger.Instance.Log("SaveMasternodePrivKey", "Completed Setup of Voting Key for voting rights", nodeType);
                        }
                    }
                }
                lockWallet();
                Logger.Instance.Log("SaveMasternodePrivKey", "Locked wallet after completion of voting node setup.", nodeType);


                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = @"DELETE FROM masternodesetupqueue";
                        cmd.ExecuteNonQuery();
                        Logger.Instance.Log("SaveMasternodePrivKey", "Deleted entree from masternodesetupqueue", nodeType);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("SaveMasternodePrivKey", "Something failed. Is Historia Core Wallet Running? Exception:" + ex.Message, nodeType);
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        [HttpPost]
        public JsonResult SetupVotingNodeQueue()
        {
            int cnt = 0;
            Logger.Instance.Log("SetupVotingNodeQueue", "Attempting to Add Node to Setup Queue", "VOTINGNODE");
            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = @"SELECT count(*) as cnt FROM masternodesetupqueue";
                        cmd.ExecuteNonQuery();

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                cnt = rdr.GetInt32(rdr.GetOrdinal("cnt"));
                            }
                        }
                    }
                }
                if (cnt == 0)
                {
                    using (var conn = new SqliteConnection(connectionString))
                    {
                        conn.Open();

                        using (var cmd = conn.CreateCommand())
                        {
                            conn.Open();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = @"INSERT INTO masternodesetupqueue 
                                                (nodeType, queue_step) 
                                                VALUES 
                                                (@nodeType, @queue_step)";
                            cmd.Parameters.AddWithValue("@nodeType", 100);
                            cmd.Parameters.AddWithValue("@queue_step", 0);
                            cmd.ExecuteNonQuery();
                            Logger.Instance.Log("SetupVotingNodeQueue", "Successfully Added Node to Setup Queue", "VOTINGNODE");
                        }
                    }
                }

                var prepRespJson1 = new { Success = true, message = "" };
                return Json(prepRespJson1);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }

        }

        [HttpPost]
        public JsonResult SetupMasternodeQueue([FromBody] SshConnectionInfo connectionInfo)
        {
            int cnt = 0;
            Logger.Instance.Log("SetupMasternodeQueue", "Attempting to Add Node to Setup Queue", "MASTERNODE");
            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = @"SELECT count(*) as cnt FROM masternodesetupqueue";
                        cmd.ExecuteNonQuery();

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                cnt = rdr.GetInt32(rdr.GetOrdinal("cnt"));
                            }
                        }
                    }
                }

                if (cnt == 0)
                {
                    using (var conn = new SqliteConnection(connectionString))
                    {
                        conn.Open();

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = @"INSERT INTO masternodesetupqueue 
                                        (nodeType, queue_step, sshAddr, sshUsername, sshPassword, sshPort, MN_DNS) 
                                        VALUES 
                                        (@nodeType, @queue_step, @sshAddr, @sshUsername, @sshPassword, @sshPort, @MN_DNS)";
                            cmd.Parameters.AddWithValue("@nodeType", 5000);  
                            cmd.Parameters.AddWithValue("@queue_step", 0);
                            cmd.Parameters.AddWithValue("@sshAddr", connectionInfo.IpAddress);
                            cmd.Parameters.AddWithValue("@sshUsername", connectionInfo.Username);
                            cmd.Parameters.AddWithValue("@sshPassword", connectionInfo.Password);
                            cmd.Parameters.AddWithValue("@sshPort", connectionInfo.Port);
                            cmd.Parameters.AddWithValue("@MN_DNS", connectionInfo.DNS);
                            cmd.ExecuteNonQuery();
                            Logger.Instance.Log("SetupMasternodeQueue", "Successfully Added Node to Setup Queue", "MASTERNODE");
                        }
                    }
                }

                var prepRespJson1 = new { Success = true, message = "" };
                return Json(prepRespJson1);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }
        }

        [HttpGet]
        public JsonResult GetLogEntries(string filter)
        {
            List<LogEntry> logEntries = new List<LogEntry>();
            string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    if (filter == "DIAGNOSIS")
                    {
                        cmd.CommandText = "SELECT Timestamp, LogType, LogSource, Log FROM logs WHERE logtype = @logtype ORDER BY Timestamp ASC";
                    } else
                    {
                        cmd.CommandText = "SELECT Timestamp, LogType, LogSource, Log FROM logs WHERE logtype = @logtype OR logtype = \"WALLET\" ORDER BY Timestamp ASC";
                    }
                    cmd.Parameters.AddWithValue("@logtype", filter);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logEntries.Add(new LogEntry
                            {
                                Timestamp = reader["Timestamp"].ToString(),
                                LogType = reader["LogType"].ToString(),
                                LogSource = reader["LogSource"].ToString(),
                                Log = reader["Log"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(logEntries);
        }

        [HttpGet]
        public JsonResult DeleteLogEntries(string filter)
        {
            try { 
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "DELETE FROM logs WHERE logtype = @logtype or logtype = @wallet";
                        cmd.Parameters.AddWithValue("@logtype", filter);
                        cmd.Parameters.AddWithValue("@wallet", "WALLET");
                        cmd.ExecuteNonQuery();
                    }
                }

                var prepRespJson1 = new { Success = true };
                return Json(prepRespJson1);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false };
                return Json(prepRespJson1);
            }
        }
        
        [HttpGet]
        public JsonResult GetVotingNodeSetupFlag()
        {
            try {              
                string alert = "";
                int cnt = 0;
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT count(alert) as cnt from masternodeprivatekeys WHERE alert = 1";

                        cmd.ExecuteNonQuery();

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                cnt = rdr.GetInt32(rdr.GetOrdinal("cnt"));
                            }
                        }
                    }
                }

                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "UPDATE masternodeprivatekeys SET alert = 0 WHERE alert = 1";
                        cmd.ExecuteNonQuery();
                    }
                }

                if (cnt > 0)
                {
                    var prepRespJson1 = new { Success = true, message = "" };
                    return Json(prepRespJson1);
                } else
                {
                    var prepRespJson1 = new { Success = false, message = "" };
                    return Json(prepRespJson1);
                }

            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }
        }

        [HttpGet]
        public JsonResult GetMasternodeSetupFlag()
        {
            try
            {
                string alert = "";
                int cnt = 0;
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT count(alert) as cnt from masternodeprivatekeys WHERE alert = 2";

                        cmd.ExecuteNonQuery();

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                cnt = rdr.GetInt32(rdr.GetOrdinal("cnt"));
                            }
                        }
                    }
                }

                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "UPDATE masternodeprivatekeys SET alert = 0 WHERE alert = 2";
                        cmd.ExecuteNonQuery();
                    }
                }

                if (cnt > 0)
                {
                    var prepRespJson1 = new { Success = true, message = "" };
                    return Json(prepRespJson1);
                }
                else
                {
                    var prepRespJson1 = new { Success = false, message = "" };
                    return Json(prepRespJson1);
                }

            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }
        }


        [HttpPost]
        public JsonResult ImportMyMasterNodes()
        {
            try
            {
                MasternodeModel masternode = new MasternodeModel();
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"masternode\", \"params\": [\"outputs\"] }";
                System.Diagnostics.Debug.WriteLine("GetMasterNodes: " + jsonstring);
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                // Debugging step: Print the raw response
                System.Diagnostics.Debug.WriteLine("Raw response: " + getResp);

                // Attempt to parse the response
                var jsonResponse = JObject.Parse(getResp);
                var result = jsonResponse["result"].ToObject<Dictionary<string, string>>();

                // Create a list to store MasternodeModel objects
                List<MasternodeModel> masternodes = new List<MasternodeModel>();

                foreach (var kvp in result)
                {
                    string CollateralTXID = kvp.Key;
                    string Collateralindex = kvp.Value;
                    int count = 0;
                    try
                    {

                        string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                        using (var conn = new SqliteConnection(connectionString))
                        {
                            conn.Open();
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandType = System.Data.CommandType.Text;
                                cmd.CommandText = "SELECT COUNT(*) as cnt from masternodeprivatekeys WHERE collateralHash = @collateralHash";
                                cmd.Parameters.AddWithValue("collateralHash", CollateralTXID);
                                cmd.ExecuteNonQuery();

                                using (SqliteDataReader rdr = cmd.ExecuteReader())
                                {
                                    if (rdr.Read())
                                    {
                                        count = rdr.GetInt32(rdr.GetOrdinal("cnt"));
                                    }
                                }
                            }
                        }

                        if (count == 0)
                        {

                            masternode = masternodelistFilterByJson(CollateralTXID, Collateralindex);

                            string masternodeprivkey = getMasternodePrivKey(masternode.VotingAddress);

                            if (!String.IsNullOrEmpty(masternodeprivkey))
                            {
                                using (var conn = new SqliteConnection(connectionString))
                                {
                                    conn.Open();

                                    using (var cmd = conn.CreateCommand())
                                    {
                                        conn.Open();
                                        cmd.CommandType = System.Data.CommandType.Text;
                                        cmd.CommandText = @"INSERT OR IGNORE INTO masternodeprivatekeys 
                                                (ProTXHash, collateralIndex, masternodeName, collateralHash, EncryptedPrivateKey) 
                                                VALUES 
                                                (@ProTXHash, @CollateralIndex, @MasternodeName, @CollateralHash, @EncryptedPrivateKey)";
                                        cmd.Parameters.AddWithValue("ProTXHash", masternode.ProTxHash);
                                        cmd.Parameters.AddWithValue("CollateralHash", CollateralTXID);
                                        cmd.Parameters.AddWithValue("CollateralIndex", Collateralindex);
                                        cmd.Parameters.AddWithValue("MasternodeName", masternode.Identity);
                                        cmd.Parameters.AddWithValue("EncryptedPrivateKey", masternodeprivkey);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                masternodes.Add(masternode);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing key {CollateralTXID}: {ex.Message}");
                    }
                }
                lockWallet();
                var json1 = JsonConvert.SerializeObject(masternodes);
                return Json(json1);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }
        }
        

        //-------------------------------------------
        //SshClient Masternode functions
        //-------------------------------------------

        public void UpdateFirewallRules(SshClient client)
        {
            Logger.Instance.Log("UpdateFirewallRules", $"Setting up firewall rules", "MASTERNODE");
            var commands = new[]
            {
                "sudo ufw allow 10101/tcp",
                "sudo ufw allow 4001/tcp",
                "sudo ufw allow 443/tcp",
                "sudo ufw allow 80/tcp"
            };

            foreach (var command in commands)
            {
                RunCommand(client, command);
            }
            Logger.Instance.Log("UpdateFirewallRules", $"Completed setting up firewall rules", "MASTERNODE");
        }

        [HttpPost]
        public async Task<JsonResult> CheckPorts([FromBody] CheckMasternode info)
        {
            int[] Ports = { 10101, 4001, 443, 80 };
            var results = new List<string>();
            bool allPortsOpen = true;

            foreach (var port in Ports)
            {
                using (var tcpClient = new TcpClient())
                {
                    try
                    {
                        await tcpClient.ConnectAsync(info.ipaddress, port);
                        Logger.Instance.Log("CheckFirewallPorts", $"Port {port} is open on {info.ipaddress}", "DIAGNOSIS");
                    }
                    catch (SocketException ex)
                    {
                        Logger.Instance.Log("CheckFirewallPorts", $"Port {port} is not open on {info.ipaddress}", "DIAGNOSIS");
                        results.Add($"Port {port} is not open on {info.ipaddress}.");
                        allPortsOpen = false;
                    }
                }
            }

            foreach (var result in results)
            {
                Logger.Instance.Log("CheckFirewallPorts", result, "DIAGNOSIS");
            }

            if (allPortsOpen)
            {
                return Json(new { success = true, message = "All specified ports are open." });
            }
            else
            {
                return Json(new { success = false, message = "Some ports are not open.", details = results });
            }
        }


        [HttpPost]
        public async Task<JsonResult> CheckWebServer([FromBody] CheckMasternode info)
        {
            try
            {
                using (var handler = new HttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    var response = await client.GetAsync("http://" + info.ipaddress);
                    if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                    {
                        Logger.Instance.Log("CheckWebServer", $"Web server is running. URL {info.ipaddress} is accessible.", "DIAGNOSIS");
                        return Json(new { success = true, message = $"Web server is running. URL {info.ipaddress} is accessible." });
                    }
                    else
                    {
                        Logger.Instance.Log("CheckWebServer", $"Web server is not running. URL {info.ipaddress} returned status code {response.StatusCode}.", "DIAGNOSIS");
                        return Json(new { success = false, message = $"Web server is not running. URL {info.ipaddress} returned status code {response.StatusCode}." });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("CheckWebServer", $"Error checking web server: {ex.Message}", "DIAGNOSIS");
                return Json(new { success = false, message = $"Error checking web server: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> CheckSslCertificate([FromBody] CheckMasternode info)
        {
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    {
                        if (errors == SslPolicyErrors.None)
                        {
                            Logger.Instance.Log("CheckSslCertificate", "SSL certificate is valid.", "DIAGNOSIS");
                            return true;
                        }

                        Logger.Instance.Log("CheckSslCertificate", $"SSL certificate validation failed: {errors}", "DIAGNOSIS");
                        return false;
                    };

                    using (var client = new HttpClient(handler))
                    {
                        var response = await client.GetAsync("https://" + info.dnsName);
                        return Json(new { success = response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound, message = response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound ? "SSL certificate is valid." : "SSL certificate validation failed." });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("CheckSslCertificate", $"Error checking SSL certificate: {ex.Message}", "DIAGNOSIS");
                return Json(new { success = false, message = $"Error checking SSL certificate: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> CheckWebIPFSServer([FromBody] CheckMasternode info)
        {
            string ipfsPath = "/ipfs/Qmd76KSvQn51VpsputPNGgdpAQsd73E5ZRxqjhtBsrGS6b/index.html";

            try
            {
                using (var handler = new HttpClientHandler())
                {
                    // Ignore SSL certificate errors
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                    using (var client = new HttpClient(handler))
                    {
                        var response = await client.GetAsync("https://" + info.dnsName + ipfsPath);
                        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                        {
                            Logger.Instance.Log("CheckWebIPFSServer", $"IPFS is running and connected to web server. URL {info.dnsName + ipfsPath} is accessible.", "DIAGNOSIS");
                            return Json(new { success = true, message = $"Web server is running. URL {info.dnsName + ipfsPath} is accessible." });
                        }
                        else
                        {
                            Logger.Instance.Log("CheckWebIPFSServer", $"IPFS is not connected to Web server. URL {info.dnsName + ipfsPath} returned status code {response.StatusCode}.", "DIAGNOSIS");
                            return Json(new { success = false, message = $"Web server is not running. URL {info.dnsName + ipfsPath} returned status code {response.StatusCode}." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("CheckWebIPFSServer", $"Error checking web server: {ex.Message}", "DIAGNOSIS");
                return Json(new { success = false, message = $"Error checking web server: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult CheckDNS([FromBody] CheckMasternode info)
        {
            Logger.Instance.Log("CheckDNS", $"Verifying DNS {info.dnsName} for IP {info.ipaddress}", "DIAGNOSIS");

            try
            {
                var hostAddresses = Dns.GetHostAddresses(info.dnsName);
                foreach (var address in hostAddresses)
                {
                    if (address.ToString() == info.ipaddress)
                    {
                        Logger.Instance.Log("CheckDNS", $"DNS {info.dnsName} resolves to IP {info.ipaddress}", "DIAGNOSIS");
                        return Json(new { success = true, message = "DNS resolves correctly." });
                    }
                }
                Logger.Instance.Log("CheckDNS", $"DNS {info.dnsName} does not resolve to IP {info.ipaddress}", "DIAGNOSIS");
                return Json(new { success = false, message = "DNS does not resolve to the provided IP." });
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("CheckDNS", $"Error verifying DNS {info.dnsName}. Exception: {ex.Message}", "DIAGNOSIS");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> CheckHistoria([FromBody] CheckMasternode info)
        {
            try
            {
                SshClient client = null;
                var sshDetails = new SshConnectionInfo
                {
                    IpAddress = info.ipaddress,
                    Username = info.username,
                    Password = info.password,
                    Port = info.port,
                    DNS = info.dnsName
                };
                if (sshDetails != null)
                {
                    client = new SshClient(sshDetails.IpAddress, sshDetails.Port, sshDetails.Username, sshDetails.Password);
                    client.Connect();
                }

                if (sshDetails == null)
                {
                    Logger.Instance.Log("CheckHistoria", "SSH details are null.", "DIAGNOSIS");
                    return Json(new { success = false, message = "SSH details are null." });
                }
                Logger.Instance.Log("CheckHistoria", "Checking if Historia is running on Linux host.", "DIAGNOSIS");

                var checkCommand = "~/.historiacore/historia-cli getinfo";
                var result = RunCommandWithOutput(client, checkCommand).Trim();

                client.Disconnect();

                if (result.Contains("error: couldn't connect to server: unknown (code -1)"))
                {
                    Logger.Instance.Log("CheckHistoria", "Historia is not running.", "DIAGNOSIS");
                    return Json(new { success = false, message = "Historia is not running." });
                }
                else
                {
                    Logger.Instance.Log("CheckHistoria", "Historia is running.", "DIAGNOSIS");
                    Logger.Instance.Log("CheckHistoria", $"Historia getinfo output: {result}", "DIAGNOSIS");
                    return Json(new { success = true, message = "Historia is running." });
                }
                
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("CheckHistoria", $"Error: {ex.Message}", "DIAGNOSIS");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> CheckIPFS([FromBody] CheckMasternode info)
        {
            try
            {
                SshClient client = null;
                var sshDetails = new SshConnectionInfo
                {
                    IpAddress = info.ipaddress,
                    Username = info.username,
                    Password = info.password,
                    Port = info.port,
                    DNS = info.dnsName
                };
                if (sshDetails != null)
                {
                    client = new SshClient(sshDetails.IpAddress, sshDetails.Port, sshDetails.Username, sshDetails.Password);
                    client.Connect();
                }

                if (sshDetails == null)
                {
                    Logger.Instance.Log("CheckIPFS", "SSH details are null.", "DIAGNOSIS");
                    return Json(new { success = false, message = "SSH details are null." });
                }


                Logger.Instance.Log("IsIpfsRunning", "Checking if IPFS is running on this host (VPS).", "DIAGNOSIS");

                var checkCommand = "ipfs id -f \"<id>\"";
                var result = RunCommandWithOutput(client, checkCommand).Trim();

                client.Disconnect();

                if (result.Contains("Qm"))
                {
                    Logger.Instance.Log("IsIpfsRunning", "IPFS is running on this host.", "DIAGNOSIS");
                    return Json(new { success = true, message = "IPFS is running on this host." });
                }
                else
                {
                    Logger.Instance.Log("IsIpfsRunning", "IPFS is not running on this host.", "DIAGNOSIS");
                    return Json(new { success = false, message = "IPFS is not running on this host." });
                }
                
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("CheckIPFS", $"Error: {ex.Message}", "DIAGNOSIS");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> CheckPortsInternal([FromBody] CheckMasternode info)
        {
            //THIS NEEDS FIXING BUT WE CAN ASSUME IF PORTS ARE OPEN EXTERNALLY THEY ARE OPEN INTERNALLY
            //This would be useful to see if ports were open internally, but not open externally
            //Could alert user to incorrect public firewall permissions 
            int[] Ports = { 10101, 4001, 443, 80 };
            try
            {
                SshClient client = null;
                var sshDetails = new SshConnectionInfo
                {
                    IpAddress = info.ipaddress,
                    Username = info.username,
                    Password = info.password,
                    Port = info.port,
                    DNS = info.dnsName
                };
                if (sshDetails != null)
                {
                    client = new SshClient(sshDetails.IpAddress, sshDetails.Port, sshDetails.Username, sshDetails.Password);
                    client.Connect();
                }

                if (sshDetails == null)
                {
                    Logger.Instance.Log("CheckPortsInternal", "SSH details are null.", "DIAGNOSIS");
                    return Json(new { success = false, message = "SSH details are null." });
                }

                if (!client.IsConnected)
                {
                    Logger.Instance.Log("CheckPortsInternal", "SSH connection failed.", "DIAGNOSIS");
                    return Json(new { success = false, message = "SSH connection failed." });
                }

                bool allPortsOpen = true;

                foreach (var port in Ports)
                {
                    var ufwCommand = $"echo \"{info.password}\" | sudo ufw status | grep '{port}/tcp'";
                    var netstatCommand = $"echo \"{info.password}\" | sudo netstat -tuln | grep ':{port} '";

                    var ufwResult = RunCommandWithOutput(client, ufwCommand).Trim();
                    var netstatResult = RunCommandWithOutput(client, netstatCommand).Trim();

                    if (string.IsNullOrEmpty(ufwResult) || string.IsNullOrEmpty(netstatResult))
                    {
                        Logger.Instance.Log("CheckPortsInternal", $"Port {port} is not open on {info.ipaddress}.", "DIAGNOSIS");
                        allPortsOpen = false;
                    }
                    else
                    {
                        Logger.Instance.Log("CheckPortsInternal", $"Port {port} is open on {info.ipaddress}.", "DIAGNOSIS");
                    }
                }

                client.Disconnect();
                return Json(new { success = allPortsOpen, message = allPortsOpen ? "All specified ports are open." : "Some ports are not open." });
                
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("CheckPortsInternal", $"Error: {ex.Message}", "MASTERNODE");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> CheckHardDriveSpace([FromBody] CheckMasternode info)
        {
            try
            {
                SshClient client = null;
                var sshDetails = new SshConnectionInfo
                {
                    IpAddress = info.ipaddress,
                    Username = info.username,
                    Password = info.password,
                    Port = info.port,
                    DNS = info.dnsName
                };
                if (sshDetails != null)
                {
                    client = new SshClient(sshDetails.IpAddress, sshDetails.Port, sshDetails.Username, sshDetails.Password);
                    client.Connect();
                }

                if (sshDetails == null)
                {
                    Logger.Instance.Log("CheckHardDriveSpace", "SSH details are null.", "DIAGNOSIS");
                    return Json(new { success = false, message = "SSH details are null." });
                }
                var command = "df -h --output=avail / | tail -n1";
                var result = RunCommandWithOutput(client, command).Trim();
                Logger.Instance.Log("CheckHardDriveSpace", $"Disk space available: {result}", "DIAGNOSIS");

                // Assuming result is in a human-readable format like "20G" or "500M"
                if (string.IsNullOrEmpty(result) || !IsSpaceSufficient(result))
                {
                    Logger.Instance.Log("CheckHardDriveSpace", "Hard drive space is insufficient.", "DIAGNOSIS");
                    client.Disconnect();
                    return Json(new { success = false, message = "Hard drive space is insufficient." });
                }

                Logger.Instance.Log("CheckHardDriveSpace", "Hard drive space is sufficient.", "DIAGNOSIS");
                client.Disconnect();
                return Json(new { success = true, message = "Hard drive space is sufficient." });
                
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("CheckHardDriveSpace", $"Error: {ex.Message}", "DIAGNOSIS");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private bool IsSpaceSufficient(string availableSpace)
        {
            // Convert the available space to a numeric value for comparison
            // For simplicity, let's assume we need at least 1G available
            if (availableSpace.EndsWith("G"))
            {
                if (double.TryParse(availableSpace.TrimEnd('G'), out double spaceInG))
                {
                    return spaceInG >= 1;
                }
            }
            else if (availableSpace.EndsWith("M"))
            {
                if (double.TryParse(availableSpace.TrimEnd('M'), out double spaceInM))
                {
                    return spaceInM >= 1024; // 1024M = 1G
                }
            }

            return false;
        }

        [HttpPost]
        public async Task<JsonResult> CheckHistoriaConfig([FromBody] CheckMasternode info)
        {
            try
            {
                SshClient client = null;
                var sshDetails = new SshConnectionInfo
                {
                    IpAddress = info.ipaddress,
                    Username = info.username,
                    Password = info.password,
                    Port = info.port,
                    DNS = info.dnsName
                };
                if (sshDetails != null)
                {
                    client = new SshClient(sshDetails.IpAddress, sshDetails.Port, sshDetails.Username, sshDetails.Password);
                    client.Connect();
                }

                if (sshDetails == null)
                {
                    Logger.Instance.Log("CheckHistoriaConfig", "SSH details are null.", "DIAGNOSIS");
                    return Json(new { success = false, message = "SSH details are null." });
                }

                var command = "cat ~/.historiacore/historia.conf";
                var configFileContent = RunCommandWithOutput(client, command).Trim();

                Logger.Instance.Log("CheckHistoriaConfig", $"Config file content: {configFileContent}", "DIAGNOSIS");

                // Check for required configuration settings
                bool isConfigValid = ValidateConfig(configFileContent);

                if (!isConfigValid)
                {
                    Logger.Instance.Log("CheckHistoriaConfig", "Historia config is invalid.", "DIAGNOSIS");
                    client.Disconnect();
                    return Json(new { success = false, message = "Historia config is invalid." });
                }

                Logger.Instance.Log("CheckHistoriaConfig", "Historia config is valid.", "DIAGNOSIS");
                client.Disconnect();
                return Json(new { success = true, message = "Historia config is valid." });
                
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("CheckHistoriaConfig", $"Error: {ex.Message}", "DIAGNOSIS");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private bool ValidateConfig(string configContent)
        {
            string[] requiredSettings =
            {
                "rpcuser=",
                "rpcpassword=",
                "rpcallowip=127.0.0.1",
                "listen=1",
                "server=1",
                "daemon=1",
                "masternodedns=",
                "masternode=1",
                "masternodeblsprivkey=",
                "masternodecollateral=5000",
                "externalip="
            };

            foreach (var setting in requiredSettings)
            {
                if (!configContent.Contains(setting))
                {
                    Logger.Instance.Log("ValidateConfig", $"Missing setting: {setting}", "MASTERNODE");
                    return false;
                }
            }

            return true;
        }
        public bool CheckMemory(SshClient client, string nodeType)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try { 
                using (var command = client.CreateCommand("free -m | awk '/^Mem:/{print $2}'"))
                {
                    var result = command.Execute();
                    if (int.TryParse(result.Trim(), out int totalMemory))
                    {
                        return totalMemory >= 1900;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("CheckMemory", "Something failed. Exception:" + ex.Message, nodeType);
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool InstallDependencies(SshClient client, string nodeType)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try
            {
                Logger.Instance.Log("InstallDependencies", "Running OS Update commands...", "MASTERNODE");
                RunCommand(client, "sudo apt update && sudo apt upgrade -y");
                RunCommand(client, "sudo apt install -y python3 virtualenv git unzip pv golang-go snapd ufw");
                Logger.Instance.Log("InstallDependencies", "Update all patches and installed dependencies executed successfully.", "MASTERNODE");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("InstallDependencies", "Something failed.", nodeType);
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }

        }

        public bool InstallHistoria(SshClient client, string nodeType)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try
            {
                Logger.Instance.Log("InstallHistoria", "Attempting Historia installation.", "MASTERNODE");
                Logger.Instance.Log("InstallHistoria", "Checking if Historia is already installed on this host (VPS).", "MASTERNODE");

                // Check if Historia is already installed
                var checkCommand = "if [ -x ~/.historiacore/historiad ]; then echo 'Installed'; else echo 'Not Installed'; fi";
                var result = RunCommandWithOutput(client, checkCommand).Trim();

                if (result == "Installed")
                {
                    Logger.Instance.Log("InstallHistoria", "Historia is already installed on this host. Skipping installation.", "MASTERNODE");
                    return true;
                }

                Logger.Instance.Log("InstallHistoria", "Historia is not installed. Proceeding with installation.", "MASTERNODE");

                RunCommand(client, "cd /tmp && wget https://github.com/HistoriaOffical/historia/releases/download/0.17.3.0/historiacore-0.17.3-x86_64-linux-gnu.tar.gz");
                RunCommand(client, "mkdir -p ~/.historiacore");
                RunCommand(client, "mkdir -p /tmp/historiacore");
                RunCommand(client, "tar xfvz /tmp/historiacore-0.17.3-x86_64-linux-gnu.tar.gz -C /tmp/historiacore");
                RunCommand(client, "cp /tmp/historiacore/historiacore-0.17.3/bin/historia-cli ~/.historiacore");
                RunCommand(client, "cp /tmp/historiacore/historiacore-0.17.3/bin/historiad ~/.historiacore");
                RunCommand(client, "chmod 755 ~/.historiacore/historia*");
                //RunCommand(client, "sudo rm -rf /tmp/historiacore-0.17.3-x86_64-linux-gnu.tar.gz");
                //RunCommand(client, "sudo rm -rf /tmp/historiacore");
                var rpcuser = Guid.NewGuid().ToString();
                var rpcpassword = Guid.NewGuid().ToString();
                var publicIp = RunCommandWithOutput(client, "curl -s ifconfig.me").Trim();
                var localPublicIp = GetLocalPublicIpAddress();

                var configContent = $@"
#----
rpcuser={rpcuser}
rpcpassword={rpcpassword}
rpcallowip=127.0.0.1
rpcallowip={localPublicIp}
#----
listen=1
server=1
daemon=1
#----
addnode=202.182.119.4:10101
addnode=149.28.22.65:10101
addnode=149.28.247.81:10101
addnode=45.32.194.49:10101
addnode=45.76.236.45:10101
addnode=209.250.233.69:10101
addnode=104.156.233.45:10101
#----
";
                RunCommand(client, $"echo \"{configContent}\" > ~/.historiacore/historia.conf");

                RunCommand(client, "~/.historiacore/historiad");

                Logger.Instance.Log("InstallHistoria", "Starting historia daemon. Please wait...", "MASTERNODE");
                System.Threading.Thread.Sleep(10000);

                RunCommand(client, "cd ~/.historiacore && git clone https://github.com/HistoriaOffical/sentinel.git");
                RunCommand(client, "cd ~/.historiacore/sentinel && virtualenv venv");
                RunCommand(client, "cd ~/.historiacore/sentinel && venv/bin/pip install -r requirements.txt");
                RunCommand(client, "cd ~/.historiacore/sentinel && venv/bin/python bin/sentinel.py");

                Logger.Instance.Log("InstallHistoria", "Historia is now installed and running.", "MASTERNODE");
                Logger.Instance.Log("InstallHistoria", "Historia installation completed.", "MASTERNODE");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("InstallHistoria", "Something failed. Exception:" + ex.Message, nodeType);
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool CheckHistoriaStatus(SshClient client)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try
            {
                Logger.Instance.Log("CheckHistoriaStatus", "Checking if Historia is running on Linux host.", "MASTERNODE");

                var checkCommand = "~/.historiacore/historia-cli getinfo";
                var result = RunCommandWithOutput(client, checkCommand).Trim();

                if (result.Contains("error: couldn't connect to server: unknown (code -1)"))
                {
                    Logger.Instance.Log("CheckHistoriaStatus", "Historia is not running.", "MASTERNODE");
                    return false;
                }
                else
                {
                    Logger.Instance.Log("CheckHistoriaStatus", "Historia is running.", "MASTERNODE");
                    Logger.Instance.Log("CheckHistoriaStatus", $"Historia getinfo output: {result}", "MASTERNODE");
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("CheckHistoriaStatus", "Something failed.", "MASTERNODE");
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool UpdateHistoriaConf(SshClient client, string domainName, string blsSecret, string ExternalIp)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try
            {
                Logger.Instance.Log("UpdateHistoriaConf", "Attempting To Update Historia Configuration", "MASTERNODE");

                Logger.Instance.Log("UpdateHistoriaConf", "Updating Historia.conf Configuration file", "MASTERNODE");
                const string confFile = "$HOME/.historiacore/historia.conf";

                var commands = new[]
                {
                        $"echo 'masternode=1' >> {confFile}",
                        $"echo 'masternodeblsprivkey={blsSecret}' >> {confFile}",
                        $"echo 'masternodecollateral=5000' >> {confFile}",
                        $"echo 'masternodedns={domainName}' >> {confFile}",
                        $"echo 'externalip={ExternalIp}' >> {confFile}",

                    };

                foreach (var command in commands)
                {
                    RunCommand(client, command);
                }
                RunCommand(client, "(crontab -l ; echo \"* * * * * cd ~/.historiacore/sentinel && ./venv/bin/python bin/sentinel.py 2>&1 >> sentinel-cron.log\") | crontab -");
                RunCommand(client, "(crontab -l ; echo \"* * * * * pidof historiad || ~/.historiacore/historiad\") | crontab -");
                RunCommand(client, "sudo sh -c '(crontab -l ; echo \"0 0 */4 * * /sbin/reboot\") | crontab -'");


                Logger.Instance.Log("UpdateHistoriaConf", "Configuration file updated successfully", "MASTERNODE");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("UpdateHistoriaConf", "Something failed.", "MASTERNODE");
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool StartHistoria(SshClient client)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try
            {
                Logger.Instance.Log("StartHistoria", "Starting historia daemon.", "MASTERNODE");
                RunCommand(client, "~/.historiacore/historiad");

                Logger.Instance.Log("StartHistoria", "Historia daemon restarted. Please wait...", "MASTERNODE");
                System.Threading.Thread.Sleep(15000);

                Logger.Instance.Log("StartHistoria", "Historia daemon is now running.", "MASTERNODE");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("StartHistoria", "Something failed.", "MASTERNODE");
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }
        public bool StopHistoria(SshClient client)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try
            {
                Logger.Instance.Log("StopHistoria", "Attempting To Stop Historia", "MASTERNODE");
                Logger.Instance.Log("StopHistoria", "Stopping historia daemon.", "MASTERNODE");
                RunCommand(client, "~/.historiacore/historia-cli stop");
                Logger.Instance.Log("StopHistoria", "Waiting for 45 seconds to ensure historia daemon has stopped.", "MASTERNODE");
                System.Threading.Thread.Sleep(45000);
                Logger.Instance.Log("StopHistoria", "Historia daemon stopped.", "MASTERNODE");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("StopHistoria", "Something failed.", "MASTERNODE");
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool InstallIpfs(SshClient client)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try
            {
                Logger.Instance.Log("InstallIpfs", "Attempting IPFS installation.", "MASTERNODE");
                Logger.Instance.Log("InstallIpfs", "Checking if IPFS is already installed on this host (VPS).", "MASTERNODE");

                // Check if IPFS is already installed
                var checkCommand = "if [ -x /usr/local/bin/ipfs ]; then echo 'Installed'; else echo 'Not Installed'; fi";
                var result = RunCommandWithOutput(client, checkCommand).Trim();

                if (result == "Installed")
                {
                    Logger.Instance.Log("InstallIpfs", "IPFS is already installed on this host. Skipping installation.", "MASTERNODE");
                    return true;
                }

                Logger.Instance.Log("InstallIpfs", "IPFS is not installed. Proceeding with installation.", "MASTERNODE");

                RunCommand(client, "wget https://dist.ipfs.io/go-ipfs/v0.4.23/go-ipfs_v0.4.23_linux-amd64.tar.gz");
                RunCommand(client, "tar xvfz go-ipfs_v0.4.23_linux-amd64.tar.gz");
                RunCommand(client, "sudo mv go-ipfs/ipfs /usr/local/bin/ipfs");
                RunCommand(client, "rm -rf go-ipfs/");

                RunCommand(client, "ipfs init -p server");

                var bootstrapNodes = new[]
                {
            "/ip4/202.182.119.4/tcp/4001/ipfs/QmVjkn7yEqb3LTLCpnndHgzczPAPAxxpJ25mNwuuaBtFJD",
            "/ip4/149.28.22.65/tcp/4001/ipfs/QmZkRv4qfXvtHot37STR8rJxKg5cDKFnkF5EMh2oP6iBVU",
            "/ip4/149.28.247.81/tcp/4001/ipfs/QmcvrQ8LpuMqtjktwXRb7Mm6JMCqVdGz6K7VyQynvWRopH",
            "/ip4/45.32.194.49/tcp/4001/ipfs/QmZXbb5gRMrpBVe79d8hxPjMFJYDDo9kxFZvdb7b2UYamj",
            "/ip4/45.76.236.45/tcp/4001/ipfs/QmeW8VxxZjhZnjvZmyBqk7TkRxrRgm6aJ1r7JQ51ownAwy",
            "/ip4/209.250.233.69/tcp/4001/ipfs/Qma946d7VCm8v2ny5S2wE7sMFKg9ZqBXkkZbZVVxjJViyu"
        };

                foreach (var node in bootstrapNodes)
                {
                    RunCommand(client, $"ipfs bootstrap add {node}");
                }

                RunCommand(client, "ipfs config --json Datastore.StorageMax '\"50GB\"'");
                RunCommand(client, "ipfs config --json Gateway.HTTPHeaders.Access-Control-Allow-Headers '[\"X-Requested-With\", \"Access-Control-Expose-Headers\", \"Range\", \"Authorization\"]'");
                RunCommand(client, "ipfs config --json Gateway.HTTPHeaders.Access-Control-Allow-Methods '[\"POST\", \"GET\"]'");
                RunCommand(client, "ipfs config --json Gateway.HTTPHeaders.Access-Control-Allow-Origin '[\"*\"]'");
                RunCommand(client, "ipfs config --json Gateway.HTTPHeaders.Access-Control-Expose-Headers '[\"Location\", \"Ipfs-Hash\"]'");
                RunCommand(client, "ipfs config --json Gateway.NoFetch 'false'");
                RunCommand(client, "ipfs config --json Swarm.ConnMgr.HighWater '50'");
                RunCommand(client, "ipfs config --json Swarm.ConnMgr.LowWater '20'");
                string whoami = RunCommandWithOutput(client, "whoami");

                // Create the service file content
                var serviceContent = $@"
[Unit]
Description=ipfs.service
After=network.target

[Service]
Type=simple
Restart=always
RestartSec=1
StartLimitInterval=0
User={whoami}
ExecStart=/usr/local/bin/ipfs daemon

[Install]
WantedBy=multi-user.target
";

                // Write the content to a temporary file
                var tempFilePath = $"/tmp/ipfs.service";
                RunCommand(client, $"echo \"{serviceContent}\" | sudo tee {tempFilePath}");

                // Move the temporary file to the correct location
                RunCommand(client, $"sudo mv {tempFilePath} /etc/systemd/system/ipfs.service");

                // Reload the systemd daemon and start the service
                RunCommand(client, "sudo systemctl daemon-reload");
                RunCommand(client, "sudo systemctl enable ipfs.service");
                RunCommand(client, "sudo systemctl start ipfs.service");

                Logger.Instance.Log("InstallIpfs", "IPFS is now installed.", "MASTERNODE");
                Logger.Instance.Log("InstallIpfs", "IPFS installation completed.", "MASTERNODE");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("InstallIpfs", $"Something failed: {ex.Message}", "MASTERNODE");
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool GetIPFSId(SshClient client)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try
            {
                Logger.Instance.Log("GetIPFSId", "Attempting to get the IPFS ID", "MASTERNODE");
                var command = "ipfs id -f \"<id>\"";
                var ipfsId = RunCommandWithOutput(client, command).Trim();

                Logger.Instance.Log("GetIPFSId", $"IPFS ID: {ipfsId}", "MASTERNODE");

                System.Threading.Thread.Sleep(10000);

                Logger.Instance.Log("InstallIpfs", "IPFS is now installed.", "MASTERNODE");
                var connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = @"UPDATE masternodesetupqueue SET MN_ipfs = @MN_ipfs";
                        cmd.Parameters.AddWithValue("@MN_ipfs", ipfsId);
                        cmd.ExecuteNonQuery();
                    }
                }
                Logger.Instance.Log("GetIPFSId", "Completed getting the IPFS ID", "MASTERNODE");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("GetIPFSId", $"Something failed: {ex.Message}", "MASTERNODE");
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool IsIpfsRunning(SshClient client)
        {
            Logger.Instance.Log("IsIpfsRunning", "Checking if IPFS is running on this host (VPS).", "MASTERNODE");

            var checkCommand = "ipfs id -f \"<id>\"";
            var result = RunCommandWithOutput(client, checkCommand).Trim();

            if (result.Contains("Qm")) // A valid IPFS ID will be returned if IPFS is running
            {
                Logger.Instance.Log("IsIpfsRunning", "IPFS is running on this host.", "MASTERNODE");
                return true;
            }
            else
            {
                Logger.Instance.Log("IsIpfsRunning", "IPFS is not running on this host.", "MASTERNODE");
                return false;
            }
        }

        public bool InstallNginx(SshClient client, string domainName)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try
            {
                Logger.Instance.Log("InstallNginx", "Attempting Nginx installation completed.", "MASTERNODE");
                Logger.Instance.Log("InstallNginx", "Checking if NGINX is already installed and configured.", "MASTERNODE");

                // Check if NGINX is already installed
                var checkNginxInstalled = "if [ -x /usr/sbin/nginx ]; then echo 'Installed'; else echo 'Not Installed'; fi";
                var nginxInstalled = RunCommandWithOutput(client, checkNginxInstalled).Trim();

                if (nginxInstalled == "Installed")
                {
                    Logger.Instance.Log("InstallNginx", "NGINX is already installed and configured. Skipping installation.", "MASTERNODE");

                    return true;
                }
                else
                {
                    Logger.Instance.Log("InstallNginx", "Installing and configuring NGINX.", "MASTERNODE");

                    RunCommand(client, "sudo apt install -y nginx");
                    Logger.Instance.Log("InstallNginx", "Nginx installation completed.", "MASTERNODE");
                    return true;
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.Log("InstallDependencies", "Something failed.", "MASTERNODE");
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool UpdateNginxConfig(SshClient client, string domainName)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try
            {
                RunCommand(client, "sudo snap install core; sudo snap refresh core");
                RunCommand(client, "sudo snap install --classic certbot");
                RunCommand(client, "sudo ln -s /snap/bin/certbot /usr/bin/certbot");
                RunCommand(client, "sudo systemctl stop nginx");

                string command = $"sudo sed -i 's/server_name _;/server_name {domainName};/' /etc/nginx/sites-available/default"; 
                RunCommand(client, command);
                command = $"sudo sed -i '/location \\/ {{/,/}}/d' /etc/nginx/sites-available/default";
                RunCommand(client, command);

                command = $@"sudo sed -i '/server_name {domainName};/a \# BEGIN IPFS SETTINGS\nlocation / {{\n    proxy_pass http://127.0.0.1:8080;\n    proxy_set_header Host \$host;\n    proxy_cache_bypass \$http_upgrade;\n    proxy_set_header X-Forwarded-For \$remote_addr;\n    allow all;\n}}\n# END IPFS SETTINGS' /etc/nginx/sites-available/default";

                RunCommand(client, command);
                RunCommand(client, "sudo systemctl start nginx");

                RunCommand(client, $"sudo certbot --nginx -d \"{domainName}\" --non-interactive --agree-tos -m your-info@historia.network --redirect");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("UpdateNginxConfig", "Something failed.", "MASTERNODE");
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public bool IsNginxRunning(SshClient client)
        {
            lock (lockObject)
            {
                if (isRunning)
                {
                    return false;
                }
                isRunning = true;
            }
            try
            {
                Logger.Instance.Log("IsNginxRunning", "Checking if NGINX is running on this host (VPS).", "MASTERNODE");

                var checkCommand = "systemctl is-active nginx";
                var result = RunCommandWithOutput(client, checkCommand).Trim();

                if (result == "active")
                {
                    Logger.Instance.Log("IsNginxRunning", "NGINX is running on this host.", "MASTERNODE");
                    return true;
                }
                else
                {
                    Logger.Instance.Log("IsNginxRunning", "NGINX is not running on this host.", "MASTERNODE");
                    return false;
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.Log("InstallDependencies", "Something failed.", "MASTERNODE");
                return false;
            }
            finally
            {
                lock (lockObject)
                {
                    isRunning = false;
                }
            }
        }

        public async Task<SshConnectionInfo> GetSshDetailsAsync()
        {
            SshConnectionInfo sshDetails = null;
            try
            {
 
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var command = connection.CreateCommand();
                    command.CommandText = @"SELECT sshAddr, sshUsername, sshPassword, sshPort, MN_DNS FROM masternodesetupqueue LIMIT 1";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            sshDetails = new SshConnectionInfo
                            {
                                IpAddress = reader.GetString(reader.GetOrdinal("sshAddr")),
                                Username = reader.GetString(reader.GetOrdinal("sshUsername")),
                                Password = reader.GetString(reader.GetOrdinal("sshPassword")),
                                Port = reader.GetInt32(reader.GetOrdinal("sshPort")),
                                DNS = reader.GetString(reader.GetOrdinal("MN_DNS"))
                            };
                        }
                    }
                }
                return sshDetails;
            } catch (Exception ex)
            {
                Logger.Instance.Log("CheckWebServer", $"Error getting SSH settings: {ex.Message}", "MASTERNODE");
                return sshDetails;
            }
        }

        private void RunCommand(SshClient client, string command)
        {
            Logger.Instance.Log("RunCommand", $"Executing command: {command}", "MASTERNODE");
            var cmd = client.CreateCommand(command);
            var result = cmd.Execute();
            if(!string.IsNullOrEmpty(result)) { 
                Logger.Instance.Log("RunCommand", $"Command result: {result}", "MASTERNODE");
            }
            if (cmd.ExitStatus != 0)
            {
                throw new Exception($"Command '{command}' failed with exit status {cmd.ExitStatus}: {cmd.Error}");
            }
        }

        private string RunCommandWithOutput(SshClient client, string commandText)
        {
            using (var command = client.CreateCommand(commandText))
            {
                return command.Execute();
            }
        }

        [HttpPost]
        public JsonResult TestSshConnection([FromBody] SshConnectionInfo connectionInfo)
        {
            Logger.Instance.Log("TestSshConnection", $"Attempting to verify DNS for {connectionInfo.DNS} with IP {connectionInfo.IpAddress}", "MASTERNODE");

            if (!VerifyDns(connectionInfo.DNS, connectionInfo.IpAddress))
            {
                Logger.Instance.Log("TestSshConnection", $"DNS name {connectionInfo.DNS} does not resolve to the provided IP address {connectionInfo.IpAddress}.", "MASTERNODE");
                var dnsErrorResponse = new { Success = false, message = "DNS name does not resolve to the provided IP address." };
                return Json(dnsErrorResponse);
            }

            try
            {
                using (var client = new SshClient(connectionInfo.IpAddress, connectionInfo.Port, connectionInfo.Username, connectionInfo.Password))
                {
                    Logger.Instance.Log("TestSshConnection", "Connecting to SSH client.", "MASTERNODE");
                    client.Connect();
                    if (client.IsConnected)
                    {
                        Logger.Instance.Log("TestSshConnection", "Connected to SSH client.", "MASTERNODE");

                        string osVersion = string.Empty;
                        using (var cmd = client.CreateCommand("lsb_release -a"))
                        {
                            osVersion = cmd.Execute();
                        }

                        if (!osVersion.Contains("Ubuntu 20.04"))
                        {
                            Logger.Instance.Log("TestSshConnection", "The remote host is not running Ubuntu 20.04.", "MASTERNODE");
                            client.Disconnect();
                            var osErrorResponse = new { Success = false, message = "The remote host is not running Ubuntu 20.04." };
                            return Json(osErrorResponse);
                        }

                        string user = string.Empty;
                        using (var cmd = client.CreateCommand("whoami"))
                        {
                            user = cmd.Execute().Trim();
                        }

                        bool hasSudo = false;

                        if (!user.Equals("root", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var sudoCmd = client.CreateCommand("sudo -n true"))
                            {
                                try
                                {
                                    sudoCmd.Execute();
                                    hasSudo = sudoCmd.ExitStatus == 0;
                                }
                                catch (Exception)
                                {
                                    hasSudo = false;
                                }
                            }
                        }

                        client.Disconnect();

                        string message = user.Equals("root", StringComparison.OrdinalIgnoreCase) ?
                                         "Connection successful! User is root. It is not recommended to setup a masternode as the root user." :
                                         hasSudo ?
                                         "Connection successful! User is not root but has sudo access. Hit the Start Masternode Setup button to continue" :
                                         "Connection successful! User is not root and does not have sudo access, which it needs to setup properly";

                        Logger.Instance.Log("TestSshConnection", message, "MASTERNODE");
                        var prepRespJson1 = new { Success = true, message = message };
                        return Json(prepRespJson1);
                    }
                    else
                    {
                        Logger.Instance.Log("TestSshConnection", "Connection to SSH client failed.", "MASTERNODE");
                        var prepRespJson1 = new { Success = false, message = "Connection failed" };
                        return Json(prepRespJson1);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("TestSshConnection", $"Could not connect to {connectionInfo.IpAddress}. Error: {ex.Message}", "MASTERNODE");
                var prepRespJson1 = new { Success = false, message = $"Could Not Connect to {connectionInfo.IpAddress}. Check settings and try again. Error: {ex.Message}" };
                return Json(prepRespJson1);
            }
        }
        private bool VerifyDns(string dnsName, string ipAddress)
        {
            Logger.Instance.Log("VerifyDns", $"Verifying DNS {dnsName} for IP {ipAddress}", "MASTERNODE");

            try
            {
                var hostAddresses = Dns.GetHostAddresses(dnsName);
                foreach (var address in hostAddresses)
                {
                    if (address.ToString() == ipAddress)
                    {
                        Logger.Instance.Log("VerifyDns", $"DNS {dnsName} resolves to IP {ipAddress}", "MASTERNODE");
                        return true;
                    }
                }
                Logger.Instance.Log("VerifyDns", $"DNS {dnsName} does not resolve to IP {ipAddress}", "MASTERNODE");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("VerifyDns", $"Error verifying DNS {dnsName}. Exception: {ex.Message}", "MASTERNODE");
                return false;
            }
        }

        private string GetLocalPublicIpAddress()
        {
            Logger.Instance.Log("GetLocalPublicIpAddress", "Fetching local public IP address", "MASTERNODE");

            try
            {
                using (var client = new WebClient())
                {
                    var ipAddress = client.DownloadString("https://api64.ipify.org").Trim();
                    Logger.Instance.Log("GetLocalPublicIpAddress", $"Local public IP address is {ipAddress}", "MASTERNODE");
                    return ipAddress;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("GetLocalPublicIpAddress", $"Error fetching local public IP address. Exception: {ex.Message}", "MASTERNODE");
                throw;
            }
        }

        [HttpPost]
        public JsonResult CreateUser([FromBody] CreateUserRequest request)
        {
            Logger.Instance.Log("CreateUser", $"Attempting to create user {request.NewUsername} on {request.IpAddress}", "MASTERNODE");

            try
            {
                using (var client = new SshClient(request.IpAddress, request.Port, request.SshUsername, request.SshPassword))
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        Logger.Instance.Log("CreateUser", $"Connected to SSH client on {request.IpAddress}", "MASTERNODE");

                        string createUserCommand = $"sudo useradd -m {request.NewUsername} && echo {request.NewUsername}:{request.NewPassword} | sudo chpasswd";
                        string addSudoAccessCommand = $"echo '{request.NewUsername} ALL=(ALL) NOPASSWD:ALL' | sudo tee /etc/sudoers.d/{request.NewUsername}";

                        using (var cmd = client.CreateCommand(createUserCommand + " && " + addSudoAccessCommand))
                        {
                            cmd.Execute();
                            client.Disconnect();
                            Logger.Instance.Log("CreateUser", $"User {request.NewUsername} created and granted sudo access successfully on {request.IpAddress}", "MASTERNODE");
                            return Json(new { Success = true, message = "New user created and granted sudo access successfully." });
                        }
                    }
                    else
                    {
                        Logger.Instance.Log("CreateUser", $"Connection to SSH client on {request.IpAddress} failed", "MASTERNODE");
                        return Json(new { Success = false, message = "Connection failed." });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("CreateUser", $"Could not create user {request.NewUsername} on {request.IpAddress}. Error: {ex.Message}", "MASTERNODE");
                return Json(new { Success = false, message = $"Could not create user: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult GiveSudoAccess([FromBody] GiveSudoAccessRequest request)
        {
            Logger.Instance.Log("GiveSudoAccess", $"Attempting to grant sudo access to {request.TargetUsername} on {request.IpAddress}", "MASTERNODE");

            try
            {
                using (var client = new SshClient(request.IpAddress, request.Port, request.RootUsername, request.RootPassword))
                {
                    client.Connect();
                    if (client.IsConnected)
                    {
                        Logger.Instance.Log("GiveSudoAccess", $"Connected to SSH client on {request.IpAddress}", "MASTERNODE");

                        // Create the sudoers entry
                        string command = $"echo '{request.TargetUsername} ALL=(ALL) NOPASSWD:ALL' | sudo tee /etc/sudoers.d/{request.TargetUsername}";
                        using (var cmd = client.CreateCommand(command))
                        {
                            cmd.Execute();
                        }

                        // Set correct permissions on the sudoers file
                        string chmodCommand = $"sudo chmod 0440 /etc/sudoers.d/{request.TargetUsername}";
                        using (var cmd = client.CreateCommand(chmodCommand))
                        {
                            cmd.Execute();
                        }

                        client.Disconnect();
                        Logger.Instance.Log("GiveSudoAccess", $"Granted sudo access to {request.TargetUsername} on {request.IpAddress}", "MASTERNODE");
                        return Json(new { Success = true, message = "Sudo access granted successfully." });
                    }
                    else
                    {
                        Logger.Instance.Log("GiveSudoAccess", $"Connection to SSH client on {request.IpAddress} failed", "MASTERNODE");
                        return Json(new { Success = false, message = "Connection failed." });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("GiveSudoAccess", $"Could not grant sudo access to {request.TargetUsername} on {request.IpAddress}. Error: {ex.Message}", "MASTERNODE");
                return Json(new { Success = false, message = $"Could not grant sudo access: {ex.Message}" });
            }
        }


        //-------------------------------------------
        //END SshClient Masternode functions
        //-------------------------------------------

        [HttpPost]
        public JsonResult GetAvaliableBalance()
        {
            MasternodeModel masternode = new MasternodeModel();
            try
            {
                // Prepare the call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"listunspent\", \"params\": [] }";
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                System.Diagnostics.Debug.WriteLine("Raw response: " + getResp);

                double totalAmount = 0;

                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);

                if (jsonResponse.ContainsKey("result"))
                {
                    var result = jsonResponse["result"];
                    if (result != null)
                    {
                        foreach (var item in result)
                        {
                            double amount = item["amount"];
                            totalAmount += amount;
                        }
                    }
                }

                var prepRespJson1 = new { Success = true, amount = totalAmount };

                return Json(prepRespJson1);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
            }
        }

        [HttpGet]
        public JsonResult GetNewAddressGet()
        {
            string address = "";
            try
            {
                // Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"getnewaddress\", \"params\": [] }";
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);

                address = jsonResponse["result"];

                var prepRespJson1 = new { success = true, address = address };

                return Json(prepRespJson1);
            }
            catch (Exception ex)
            {

                var prepRespJson1 = new { success = false, address = address };

                return Json(prepRespJson1);
            }
        }

        private string GetNewAddress()
        {
            try
            {
                // Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"getnewaddress\", \"params\": [] }";
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);

                return jsonResponse["result"];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return "empty";
            }
        }

        private string GetCollateralIndex(string txid)
        {
            string CollateralTXID = "";
            string Collateralindex = "";
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"masternode\", \"params\": [\"outputs\"] }";
                Logger.Instance.Log("GetCollateralIndex", $"{jsonstring}", "VOTINGNODE");
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                System.Diagnostics.Debug.WriteLine("Raw response: " + getResp);
                var jsonResponse = JObject.Parse(getResp);
                var result = jsonResponse["result"].ToObject<Dictionary<string, string>>();

                foreach (var kvp in result)
                {
                    if (txid == kvp.Key)
                    {
                        CollateralTXID = kvp.Key;
                        Collateralindex = kvp.Value;
                    }
                }

                return Collateralindex;


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return Collateralindex;
            }
        }

        private (string, string) blsgenerate()
        {
            string secret = "";
            string publicKey = "";
            try
            {
                // Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"bls\", \"params\": [\"generate\"] }";
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);

                if (jsonResponse.ContainsKey("result"))
                {
                    var result = jsonResponse["result"];
                    if (result != null)
                    {
                        secret = result.secret;
                        publicKey = result.@public;

                    }
                }

                return (secret, publicKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return (secret, publicKey);
            }
        }

        private string GetBlock()
        {
            string block = "";

            try
            {
                // Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"getblockcount\", \"params\": [] }";
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);
                block = jsonResponse["result"].ToString();

                return block;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return block;
            }
        }

        private MasternodeModel masternodelistFilterByJson(string key, string index)
        {
            try
            {
                // Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";

                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"masternodelist\", \"params\": [\"json\", \"" + key + "\"] }";
                System.Diagnostics.Debug.WriteLine("GetMasterNodes: " + jsonstring);

                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                WebResponse webResponse = webRequest.GetResponse();

                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);

                MasternodeModel masternode = new MasternodeModel();

                if (jsonResponse.ContainsKey("result"))
                {
                    var result = jsonResponse["result"];
                    if (result != null)
                    {
                        var mn = result[key + "-" + index];

                        masternode.Status = mn.status;
                        masternode.Payee = mn.payee;
                        masternode.LastSeen = mn.lastpaidtime;
                        masternode.ActiveSeconds = mn.lastpaidtime;
                        masternode.IPAddress = mn.address;
                        masternode.Identity = "https://" + mn.identity;
                        masternode.ProTxHash = mn.proTxHash;
                        masternode.OwnerAddress = mn.owneraddress;
                        masternode.VotingAddress = mn.votingaddress;
                        masternode.CollateralAddress = mn.collateraladdress;
                        masternode.PubKeyOperator = mn.pubkeyoperator;
                        masternode.IpfsPeerId = mn.ipfspeerid;

                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Masternode with the specified key not found.");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Result not found in the response.");
                }

                return masternode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                return new MasternodeModel(); // Return an empty model in case of an exception.
            }
        }

        private string getMasternodePrivKey(string votingAddress)
        {
            try
            {
                // Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";

                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"dumpprivkey\", \"params\": [\"" + votingAddress + "\"] }";
                System.Diagnostics.Debug.WriteLine("GetMasterNodes: " + jsonstring);

                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                WebResponse webResponse = webRequest.GetResponse();

                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();

                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);

                string result = "";

                if (jsonResponse.ContainsKey("result"))
                {
                    result = jsonResponse["result"];
                    if (!String.IsNullOrEmpty(result))
                    {
                        return result;

                    }
                    else
                    {
                        return result;
                    }
                }
                else
                {
                    return result;
                }

            }
            catch (Exception ex)
            {
                string result = "";
                return result;
            }
        }


        [HttpPost]
        public JsonResult GetMasterNodesInfo([FromBody] CollateralHashModel model)
        {
            try
            {
                MasternodeModel mn = new MasternodeModel();
                string collateralHash = model.CollateralHash;
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT * FROM masternodeprivatekeys WHERE collateralHash = @collateralHash";
                        cmd.Parameters.AddWithValue("@collateralHash", collateralHash);
                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                mn.ProTxHash = rdr.GetString(rdr.GetOrdinal("ProTxHash"));
                                mn.CollateralHash = rdr.GetString(rdr.GetOrdinal("collateralHash"));
                                mn.CollateralIndex = rdr.GetInt16(rdr.GetOrdinal("collateralIndex"));
                                mn.Name = !rdr.IsDBNull(rdr.GetOrdinal("masternodeName")) ? rdr.GetString(rdr.GetOrdinal("masternodeName")) : string.Empty;
                                mn.blsprivkey = !rdr.IsDBNull(rdr.GetOrdinal("blsprivkey")) ? rdr.GetString(rdr.GetOrdinal("blsprivkey")) : string.Empty;
                                mn.blspublickey = !rdr.IsDBNull(rdr.GetOrdinal("blspublickey")) ? rdr.GetString(rdr.GetOrdinal("blspublickey")) : string.Empty;
                                mn.feeSourceAddress = !rdr.IsDBNull(rdr.GetOrdinal("feeSourceAddress")) ? rdr.GetString(rdr.GetOrdinal("feeSourceAddress")) : string.Empty;
                            }
                        }
                    }
                }

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";
                string jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"masternode\", \"params\": [\"list\", \"json\", \"{collateralHash}\"] }}";
                System.Diagnostics.Debug.WriteLine("GetVotingNodeReg: " + jsonstring);
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse webResponse = webRequest.GetResponse();
                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                System.Diagnostics.Debug.WriteLine("Raw response: " + getResp);
                var jsonResponse = JObject.Parse(getResp);
                var result = jsonResponse["result"] as JObject;
                if (result != null)
                {
                    foreach (var item in result)
                    {
                        var properties = item.Value as JObject;
                        if (properties != null)
                        {
                            mn.Identity = properties["identity"].ToString();
                            mn.CollateralAddress = properties["collateraladdress"].ToString();
                            mn.OwnerAddress = properties["owneraddress"].ToString();
                            mn.VotingAddress = properties["votingaddress"].ToString();
                            mn.PubKeyOperator = properties["pubkeyoperator"].ToString();
                            mn.IpfsPeerId = properties["ipfspeerid"].ToString();
                            mn.Status = properties["status"].ToString();
                            mn.IPAddress = properties["address"].ToString();
                            mn.lastpaidtime = properties["lastpaidtime"].ToString();
                            mn.lastpaidblock = properties["lastpaidblock"].ToString();
                            mn.Payee = properties["payee"].ToString();
                            break; 
                        }
                    }
                }
                return Json(mn);
            }
            catch (Exception ex)
            {
                var prepRespJson = new { success = false, error = ex.Message };
                return Json(prepRespJson);
            }
        }

        [HttpPost]
        public JsonResult SaveMasternodeFields([FromBody] SaveFieldsModel model)
        {
            string feeSourceAddress = model.feeSourceAddress;
            string blsprivkey = model.blsprivkey;
            string blspublickey = model.blspublickey;
            string collateralHash = model.CollateralHash;
            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "UPDATE masternodeprivatekeys SET feeSourceAddress = @feeSourceAddress, blsprivkey = @blsprivkey, blspublickey = @blspublickey WHERE collateralHash = @collateralHash";
                        cmd.Parameters.AddWithValue("@feeSourceAddress", feeSourceAddress);
                        cmd.Parameters.AddWithValue("@blsprivkey", blsprivkey);
                        cmd.Parameters.AddWithValue("@blspublickey", blspublickey);
                        cmd.Parameters.AddWithValue("@collateralHash", collateralHash);
                        cmd.ExecuteNonQuery();
                    }
                }

                var prepRespJson = new { success = true };
                return Json(prepRespJson);
            }
            catch (Exception ex)
            {
                var prepRespJson = new { success = false, error = ex.Message };
                return Json(prepRespJson);
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

                        // Separate the masternodes into two lists
                        List<MasternodeModel> historiasysNodes = masternodes.Where(mn => mn.Identity.Contains("historiasys.network")).ToList();
                        List<MasternodeModel> otherNodes = masternodes.Where(mn => !mn.Identity.Contains("historiasys.network")).ToList();

                        // Shuffle the otherNodes list
                        Random rng = new Random();
                        int n = otherNodes.Count;
                        while (n > 1)
                        {
                            n--;
                            int k = rng.Next(n + 1);
                            MasternodeModel value = otherNodes[k];
                            otherNodes[k] = otherNodes[n];
                            otherNodes[n] = value;
                        }

                        // Combine the lists with historiasysNodes first
                        List<MasternodeModel> sortedMasternodes = historiasysNodes.Concat(otherNodes).ToList();

                        var responseObj = new
                        {
                            success = true,
                            data = sortedMasternodes
                        };
                        return Json(responseObj);
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
                bool HTTPEnabled = CheckHttpAvailability("http://" + Identity);

                var prepRespJson = new { pingtime = PingTime, identity = Identity, httpsenabled = HTTPSEnabled, httpenabled = HTTPEnabled, success = true };
                return Json(prepRespJson);
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return Json(prepRespJson1);
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

        private bool CheckHttpAvailability(string url)
        {
            // This callback will ignore certificate errors
            ServicePointManager.ServerCertificateValidationCallback =
                new RemoteCertificateValidationCallback(IgnoreCertificateErrors);

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
            finally
            {
                // Reset the callback to its default behavior to avoid affecting other requests
                ServicePointManager.ServerCertificateValidationCallback = null;
            }
        }

        private bool IgnoreCertificateErrors(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        [HttpGet]
        public JsonResult DeleteRegisteredMasternode(string id)
        {
            try
            {
                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
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




        [HttpPost]
        public JsonResult SignMessage([FromBody] SignMessageModel model)
        {
            string message = model.Message;
            string privateKey = model.privateKey;
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

        private string SignMessage(string collateralAddr, string signMessage)
        {
            string signature = "";
            try
            {
                //Prepare call
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                webRequest.Credentials = new NetworkCredential(_userName, _password);
                webRequest.ContentType = "application/json-rpc";
                webRequest.Method = "POST";

                string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"signmessage\", \"params\": [\"" + collateralAddr + "\", \"" + signMessage + "\"] }";
                System.Diagnostics.Debug.WriteLine("SIGNMESSAGE: " + jsonstring);
                byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                webRequest.ContentLength = byteArray.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                WebResponse webResponse = webRequest.GetResponse();

                StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                string getResp = sr.ReadToEnd();
                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(getResp);
                signature = jsonResponse["result"].ToString();

                return signature;
            }
            catch (Exception ex)
            {
                var prepRespJson1 = new { Success = false, Error = ex.ToString() };
                return signature;
            }
        }

        private bool IsMasternodeStatusPoseBanned(string hash)
        {
            string index = "";
            try
            {

                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT collateralIndex from masternodeprivatekeys WHERE collateralHash = @collateralHash";
                        cmd.Parameters.AddWithValue("@collateralHash", hash);

                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                index = rdr.GetString(rdr.GetOrdinal("collateralIndex"));
                            }
                        }


                    }
                }

                MasternodeModel masternode = new MasternodeModel();
                masternode = masternodelistFilterByJson(hash, index);
                if(masternode.Status == "POSE_BANNED")
                {
                    return true;
                } else
                {
                    return false;
                }
                               

            }
            catch (Exception ex)
            {
                return true;
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
                    if (IsMasternodeStatusPoseBanned(CollateralHash))
                    {
                        //Skip Voting of nodes in POSE_BANNED State, as that will fail anyways
                        skipToNextVd = false;
                        continue;
                    }

                    if (skipToNextVd)
                    {
                        skipToNextVd = false;
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
                List<MasternodeModel> masterNodes = new List<MasternodeModel>();

                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
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
                                MasternodeModel mn = new MasternodeModel();
                                mn.Id = rdr.GetInt16(rdr.GetOrdinal("Id"));
                                mn.CollateralHash = rdr.GetString(rdr.GetOrdinal("collateralHash"));
                                mn.CollateralIndex = rdr.GetInt16(rdr.GetOrdinal("collateralIndex"));
                                mn.VotingPrivateKey = !rdr.IsDBNull(rdr.GetOrdinal("EncryptedPrivateKey")) ? rdr.GetString(rdr.GetOrdinal("EncryptedPrivateKey")) : string.Empty;
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
        public JsonResult StoreEncryptedPrivateKeys([FromBody] AddUpdateMasternode request)
        {
            try
            {

                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
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
                List<MasternodeModel> masterNodes = new List<MasternodeModel>();

                string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                using (var conn = new SqliteConnection(connectionString))
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
                                MasternodeModel mn = new MasternodeModel();
                                mn.Id = rdr.GetInt16(rdr.GetOrdinal("Id"));
                                mn.CollateralHash = rdr.GetString(rdr.GetOrdinal("collateralHash"));
                                mn.CollateralIndex = rdr.GetInt16(rdr.GetOrdinal("collateralIndex"));
                                mn.Name = rdr.GetString(rdr.GetOrdinal("masternodeName"));
                                MasternodeModel masternode = new MasternodeModel();
                                masternode = masternodelistFilterByJson(mn.CollateralHash, mn.CollateralIndex.ToString());
                                mn.Status = masternode.Status;
                                mn.IPAddress = GetIpAddress(masternode.Identity);
                                mn.VotingPrivateKey = rdr.IsDBNull(rdr.GetOrdinal("EncryptedPrivateKey")) || string.IsNullOrEmpty(rdr.GetString(rdr.GetOrdinal("EncryptedPrivateKey"))) ? "0" : "1";
                                mn.blsprivkey = rdr.IsDBNull(rdr.GetOrdinal("blsprivkey")) || string.IsNullOrEmpty(rdr.GetString(rdr.GetOrdinal("blsprivkey"))) ? "0" : "1";
                                mn.blspublickey = rdr.IsDBNull(rdr.GetOrdinal("blspublickey")) || string.IsNullOrEmpty(rdr.GetString(rdr.GetOrdinal("blspublickey"))) ? "0" : "1";
                                mn.feeSourceAddress = rdr.IsDBNull(rdr.GetOrdinal("feeSourceAddress")) || string.IsNullOrEmpty(rdr.GetString(rdr.GetOrdinal("feeSourceAddress"))) ? "0" : "1";

                                if (masternode.Status == "VOTER-ENABLED")
                                {
                                    mn.Identity = "VOTER";
                                }
                                else
                                {
                                    mn.Identity = masternode.Identity;
                                }
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

        private static string GetIpAddress(string url)
        {
            try
            {
                // Extract the domain name from the URL
                Uri uri = new Uri(url);
                string domainName = uri.Host;

                // Get the IP addresses associated with the domain name
                System.Net.IPAddress[] addresses = Dns.GetHostAddresses(domainName);
                if (addresses.Length > 0)
                {
                    return addresses[0].ToString();
                }
                else
                {
                    return "No IP address found for the domain.";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    

    }
}
