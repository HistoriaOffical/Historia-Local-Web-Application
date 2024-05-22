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
using Ganss.Xss;
using OpenGraphNet;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using System.Net.Http;
using System.Net.Http.Headers;

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

		public List<ProposalModel> LoadAllRecords()
		{
			List<ProposalModel> records = new List<ProposalModel>();

			try
			{
				string rpcServerUrl = "http://" + ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;

				HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(rpcServerUrl);
				webRequest.Credentials = new NetworkCredential(_userName, _password);
				webRequest.ContentType = "application/json-rpc";
				webRequest.Method = "POST";
				webRequest.Timeout = 5000;
				string jsonstring = String.Format("{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [\"list\"] }}");

				// serialize json for the request
				byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
				webRequest.ContentLength = byteArray.Length;
				Stream dataStream = webRequest.GetRequestStream();
				dataStream.Write(byteArray, 0, byteArray.Length);
				dataStream.Close();
				WebResponse webResponse = webRequest.GetResponse();
				StreamReader sr = new StreamReader(webResponse.GetResponseStream());
				string getResp = sr.ReadToEnd();
				if (!String.IsNullOrEmpty(getResp))
				{
					dynamic response = Newtonsoft.Json.JsonConvert.DeserializeObject(getResp);
					foreach (var record in response.result)
					{
						ProposalModel pm = new ProposalModel();

						//Get Datastring Info
						var ds = record.Value.DataString;
						string p1 = ds;
						dynamic proposal1 = JObject.Parse(p1);

						string hostname = _ipfsUrl;
						pm.DataString = record.Value.DataString;
						pm.Hostname = hostname;
						pm.IPFSUrl = _ipfsUrl;

						pm.IPFSWebPort = _ipfsWebPort;
						pm.Hash = record.Value.Hash;
						pm.ProposalName = HttpUtility.HtmlEncode(proposal1.summary.name.ToString());
						pm.ProposalSummary = HttpUtility.HtmlEncode(proposal1.summary.description.ToString());

						pm.ProposalDescriptionUrl = proposal1.ipfscid.ToString();
						pm.PaymentAddress = proposal1.payment_address.ToString();
						pm.YesCount = long.Parse(record.Value.YesCount.ToString());
						pm.NoCount = long.Parse(record.Value.NoCount.ToString());
						pm.AbstainCount = long.Parse(record.Value.AbstainCount.ToString());
						pm.CachedLocked = bool.Parse(record.Value.fCachedLocked.ToString());

						pm.CachedFunding = bool.Parse(record.Value.fCachedFunding.ToString());
						pm.PaymentAmount = decimal.Parse(proposal1.payment_amount.ToString());
						pm.PaymentDate = UnixTimeStampToDateTime(double.Parse(proposal1.start_epoch.ToString())).ToString("MMM dd, yyyy");
						DateTime EndDateTemp = UnixTimeStampToDateTime(double.Parse(proposal1.end_epoch.ToString()));
						pm.PaymentEndDate = EndDateTemp.AddDays(-2);
						pm.ProposalDate = pm.PaymentDate;

						pm.PermLocked = bool.Parse(record.Value.fPermLocked.ToString());

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
						pm.ParentIPFSCID = string.IsNullOrEmpty(proposal1.ipfspid.ToString()) ? "" : proposal1.ipfspid.ToString();
						if (!string.IsNullOrEmpty(pm.ParentIPFSCID))
						{

							if (string.IsNullOrEmpty(proposal1.ipfscidtype?.ToString()))
							{
								pm.cidtype = "0";
							}
							else
							{
								pm.cidtype = proposal1.ipfscidtype.ToString();
							}

							if (pm.PermLocked)
							{
								pm.IsUpdate = "0";
							}
							else
							{
								pm.IsUpdate = "1";
							}
						}
						else
						{
							if (string.IsNullOrEmpty(proposal1.ipfscidtype?.ToString()))
							{
								pm.cidtype = "0";
							}
							else
							{
								pm.cidtype = proposal1.ipfscidtype.ToString();
							}
							if (pm.PermLocked)
							{
								pm.IsUpdate = "0";
							}
							else
							{
								pm.IsUpdate = "1";
							}
						}
						records.Add(pm);
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogCritical("ProposalDescription Error: " + ex.ToString());
			}

			//sort descending based on DataString.name
			List<ProposalModel> sortedRecords = records.OrderByDescending(o => ((dynamic)JObject.Parse(o.DataString)).name).ToList();
			return sortedRecords;
		}

		public JsonResult GetVersionsDetails(string phash, int level)
		{
			string IPFSHash = "";
			string ParentIPFSCid = "", url = "";
			int pid = 0;
			List<PreviousVersionsModel> previousVersions = new List<PreviousVersionsModel>();
			try
			{
				//Get first parent hash based on Record View information.
				string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
				using (var conn = new SqliteConnection(connectionString))
				{
					using (var cmd = conn.CreateCommand())
					{
						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT ParentIPFSCID, ipfscid, id FROM items WHERE proposalhash = @phash";
						cmd.Parameters.AddWithValue("phash", phash);

						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							if (rdr.Read())
							{
								ParentIPFSCid = !rdr.IsDBNull(rdr.GetOrdinal("ParentIPFSCID")) ? rdr.GetString(rdr.GetOrdinal("ParentIPFSCID")) : string.Empty;
								pid = !rdr.IsDBNull(rdr.GetOrdinal("id")) ? rdr.GetInt32(rdr.GetOrdinal("id")) : 0;
								url = !rdr.IsDBNull(rdr.GetOrdinal("ipfscid")) ? rdr.GetString(rdr.GetOrdinal("ipfscid")) : string.Empty;

							}
						}
						conn.Close();
					}
				}
				List<int> numbersList = new List<int>();

				if (!string.IsNullOrEmpty(ParentIPFSCid))
				{
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT proposalid FROM proposalmatrix where url = @ParentIPFSCid or ParentIPFS = @ParentIPFSCid or url = @url or ParentIPFS = @url";
							cmd.Parameters.AddWithValue("ParentIPFSCid", ParentIPFSCid);
							cmd.Parameters.AddWithValue("url", url);
							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									numbersList.Add(!rdr.IsDBNull(rdr.GetOrdinal("proposalid")) ? rdr.GetInt32(rdr.GetOrdinal("proposalid")) : 0);
								}
							}
							conn.Close();
						}
					}
				}
				else
				{
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT url FROM proposalmatrix where proposalmatrixid = @pid";
							cmd.Parameters.AddWithValue("pid", pid);
							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									url = !rdr.IsDBNull(rdr.GetOrdinal("url")) ? rdr.GetString(rdr.GetOrdinal("url")) : string.Empty;
								}
							}
							conn.Close();
						}
					}

					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT proposalid FROM proposalmatrix where url = @url or ParentIPFS = @url";
							cmd.Parameters.AddWithValue("url", url);
							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									numbersList.Add(!rdr.IsDBNull(rdr.GetOrdinal("proposalid")) ? rdr.GetInt32(rdr.GetOrdinal("proposalid")) : 0);
								}
							}
							conn.Close();
						}
					}
				}
				foreach (int id in numbersList)
				{
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT * FROM items WHERE id = @id AND (cidtype IS NULL OR cidtype = 0)";

							cmd.Parameters.AddWithValue("id", id); // Use the current id from the list

							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									PreviousVersionsModel previousVersion = new PreviousVersionsModel();
									var hash = rdr.GetString(rdr.GetOrdinal("proposalhash"));
									previousVersion.Id = !rdr.IsDBNull(rdr.GetOrdinal("Id")) ? rdr.GetInt32(rdr.GetOrdinal("Id")) : 0;
									previousVersion.IPFSHash = !rdr.IsDBNull(rdr.GetOrdinal("ParentIPFSCID")) ? rdr.GetString(rdr.GetOrdinal("ipfscid")) : rdr.GetString(rdr.GetOrdinal("ipfscid"));
									previousVersion.ParentIPFSCid = rdr.GetString(rdr.GetOrdinal("ParentIPFSCid"));
									previousVersion.Name = rdr.GetString(rdr.GetOrdinal("Name"));
									previousVersion.Summary = rdr.GetString(rdr.GetOrdinal("Summary"));
									double dateAdded = double.Parse(rdr.GetString(rdr.GetOrdinal("dateadded")));
									previousVersion.DateAdded = UnixTimeStampToDateTime(dateAdded).ToString("MMM dd, yyyy HH:mm:ss");
									previousVersion.ProposalHash = rdr.GetString(rdr.GetOrdinal("proposalhash"));
									if (phash == previousVersion.ProposalHash)
									{
										previousVersion.Summary = previousVersion.Summary + " <b>(Current document)</b>";
									}
									previousVersion.cidtype = rdr.GetString(rdr.GetOrdinal("cidtype")); ;

									previousVersions.Add(previousVersion);  //Add to list
								}
							}
							conn.Close();
						}
					}
				}

				if (previousVersions.Count > 1)
				{
					//Dedup.
					List<PreviousVersionsModel> AllVersions = new List<PreviousVersionsModel>();
					AllVersions = previousVersions.GroupBy(x => x.IPFSHash).Select(x => x.Last()).ToList();
					AllVersions = AllVersions.GroupBy(x => x.DateAdded).Select(x => x.Last()).ToList();
					//Sort
					if (AllVersions != null && AllVersions.Count > 0)
						AllVersions = AllVersions.OrderBy(p => p.DateAdded).ToList();
					return Json(JsonConvert.SerializeObject(AllVersions));
				}
				else
				{
					dynamic prepJson = JObject.Parse("{success: false}");
					return Json(prepJson);
				}
			}
			catch (Exception ex)
			{
				dynamic prepBadRespJson = JObject.Parse("{success: false}");
				return Json(prepBadRespJson);
			}

		}

		public JsonResult GetEvidenceDetails(string phash, int level)
		{
			string IPFSHash = "";
			string ParentIPFSCid = "", url = "";
			int pid = 0;
			List<PreviousVersionsModel> previousVersions = new List<PreviousVersionsModel>();
			try
			{
				//Get first parent hash based on Record View information.
				string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
				using (var conn = new SqliteConnection(connectionString))
				{
					using (var cmd = conn.CreateCommand())
					{
						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT ParentIPFSCID, ipfscid, id FROM items WHERE proposalhash = @phash";
						cmd.Parameters.AddWithValue("phash", phash);

						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							if (rdr.Read())
							{
								ParentIPFSCid = !rdr.IsDBNull(rdr.GetOrdinal("ParentIPFSCID")) ? rdr.GetString(rdr.GetOrdinal("ParentIPFSCID")) : string.Empty;
								pid = !rdr.IsDBNull(rdr.GetOrdinal("id")) ? rdr.GetInt32(rdr.GetOrdinal("id")) : 0;
								url = !rdr.IsDBNull(rdr.GetOrdinal("ipfscid")) ? rdr.GetString(rdr.GetOrdinal("ipfscid")) : string.Empty;

							}
						}
						conn.Close();
					}
				}
				List<int> numbersList = new List<int>();

				if (!string.IsNullOrEmpty(ParentIPFSCid))
				{
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT proposalid FROM proposalmatrix where url = @ParentIPFSCid or ParentIPFS = @ParentIPFSCid or url = @url or ParentIPFS = @url";
							cmd.Parameters.AddWithValue("ParentIPFSCid", ParentIPFSCid);
							cmd.Parameters.AddWithValue("url", url);
							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									numbersList.Add(!rdr.IsDBNull(rdr.GetOrdinal("proposalid")) ? rdr.GetInt32(rdr.GetOrdinal("proposalid")) : 0);
								}
							}
							conn.Close();
						}
					}
				}
				else
				{
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT url FROM proposalmatrix where proposalmatrixid = @pid";
							cmd.Parameters.AddWithValue("pid", pid);
							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									url = !rdr.IsDBNull(rdr.GetOrdinal("url")) ? rdr.GetString(rdr.GetOrdinal("url")) : string.Empty;
								}
							}
							conn.Close();
						}
					}

					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT proposalid FROM proposalmatrix where url = @url or ParentIPFS = @url";
							cmd.Parameters.AddWithValue("url", url);
							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									numbersList.Add(!rdr.IsDBNull(rdr.GetOrdinal("proposalid")) ? rdr.GetInt32(rdr.GetOrdinal("proposalid")) : 0);
								}
							}
							conn.Close();
						}
					}
				}
				foreach (int id in numbersList)
				{

					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT * FROM items WHERE id = @id AND (cidtype = 1 OR cidtype IS NULL OR cidtype = 0)";

							cmd.Parameters.AddWithValue("id", id); // Use the current id from the list

							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									PreviousVersionsModel previousVersion = new PreviousVersionsModel();
									var hash = rdr.GetString(rdr.GetOrdinal("proposalhash"));
									previousVersion.Id = !rdr.IsDBNull(rdr.GetOrdinal("Id")) ? rdr.GetInt32(rdr.GetOrdinal("Id")) : 0;
									previousVersion.IPFSHash = !rdr.IsDBNull(rdr.GetOrdinal("ParentIPFSCID")) ? rdr.GetString(rdr.GetOrdinal("ipfscid")) : rdr.GetString(rdr.GetOrdinal("ipfscid"));
									previousVersion.ParentIPFSCid = rdr.GetString(rdr.GetOrdinal("ParentIPFSCid"));
									previousVersion.Name = rdr.GetString(rdr.GetOrdinal("Name"));
									previousVersion.Summary = rdr.GetString(rdr.GetOrdinal("Summary"));
									double dateAdded = double.Parse(rdr.GetString(rdr.GetOrdinal("dateadded")));
									previousVersion.DateAdded = UnixTimeStampToDateTime(dateAdded).ToString("MMM dd, yyyy HH:mm:ss");
									previousVersion.ProposalHash = rdr.GetString(rdr.GetOrdinal("proposalhash"));
									if (phash == previousVersion.ProposalHash)
									{
										previousVersion.Summary = previousVersion.Summary + " <b>(Current document)</b>";
									}
									previousVersion.cidtype = rdr.GetString(rdr.GetOrdinal("cidtype")); ;

									previousVersions.Add(previousVersion);  //Add to list
								}
							}
							conn.Close();
						}
					}
				}

				if (previousVersions.Count > 1)
				{
					//Dedup.
					List<PreviousVersionsModel> AllVersions = new List<PreviousVersionsModel>();
					AllVersions = previousVersions.GroupBy(x => x.IPFSHash).Select(x => x.Last()).ToList();
					AllVersions = AllVersions.GroupBy(x => x.DateAdded).Select(x => x.Last()).ToList();
					//Sort
					if (AllVersions != null && AllVersions.Count > 0)
						AllVersions = AllVersions.OrderBy(p => p.DateAdded).ToList();
					return Json(JsonConvert.SerializeObject(AllVersions));
				}
				else
				{
					dynamic prepJson = JObject.Parse("{success: false}");
					return Json(prepJson);
				}
			}
			catch (Exception ex)
			{
				dynamic prepBadRespJson = JObject.Parse("{success: false}");
				return Json(prepBadRespJson);
			}


		}

		public JsonResult GetTopicsDetails(string phash, int level)
		{
			string IPFSHash = "";
			string ParentIPFSCid = "", url = "";
			int pid = 0;
			List<PreviousVersionsModel> previousVersions = new List<PreviousVersionsModel>();
			try
			{
				//Get first parent hash based on Record View information.
				string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
				using (var conn = new SqliteConnection(connectionString))
				{
					using (var cmd = conn.CreateCommand())
					{
						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT ParentIPFSCID, ipfscid, id FROM items WHERE proposalhash = @phash";
						cmd.Parameters.AddWithValue("phash", phash);

						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							if (rdr.Read())
							{
								ParentIPFSCid = !rdr.IsDBNull(rdr.GetOrdinal("ParentIPFSCID")) ? rdr.GetString(rdr.GetOrdinal("ParentIPFSCID")) : string.Empty;
								pid = !rdr.IsDBNull(rdr.GetOrdinal("id")) ? rdr.GetInt32(rdr.GetOrdinal("id")) : 0;
								url = !rdr.IsDBNull(rdr.GetOrdinal("ipfscid")) ? rdr.GetString(rdr.GetOrdinal("ipfscid")) : string.Empty;

							}
						}
						conn.Close();
					}
				}
				List<int> numbersList = new List<int>();

				if (!string.IsNullOrEmpty(ParentIPFSCid))
				{
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT proposalid FROM proposalmatrix where url = @ParentIPFSCid or ParentIPFS = @ParentIPFSCid or url = @url or ParentIPFS = @url";
							cmd.Parameters.AddWithValue("ParentIPFSCid", ParentIPFSCid);
							cmd.Parameters.AddWithValue("url", url);
							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									numbersList.Add(!rdr.IsDBNull(rdr.GetOrdinal("proposalid")) ? rdr.GetInt32(rdr.GetOrdinal("proposalid")) : 0);
								}
							}
							conn.Close();
						}
					}
				}
				else
				{

					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT url FROM proposalmatrix where proposalmatrixid = @pid";
							cmd.Parameters.AddWithValue("pid", pid);
							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									url = !rdr.IsDBNull(rdr.GetOrdinal("url")) ? rdr.GetString(rdr.GetOrdinal("url")) : string.Empty;
								}
							}
							conn.Close();
						}
					}

					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT proposalid FROM proposalmatrix where url = @url or ParentIPFS = @url";
							cmd.Parameters.AddWithValue("url", url);
							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									numbersList.Add(!rdr.IsDBNull(rdr.GetOrdinal("proposalid")) ? rdr.GetInt32(rdr.GetOrdinal("proposalid")) : 0);
								}
							}
							conn.Close();
						}
					}
				}
				foreach (int id in numbersList)
				{
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT * FROM items WHERE id = @id AND (cidtype = 2 OR cidtype IS NULL OR cidtype = 0)";

							cmd.Parameters.AddWithValue("id", id); // Use the current id from the list

							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									PreviousVersionsModel previousVersion = new PreviousVersionsModel();
									var hash = rdr.GetString(rdr.GetOrdinal("proposalhash"));
									previousVersion.Id = !rdr.IsDBNull(rdr.GetOrdinal("Id")) ? rdr.GetInt32(rdr.GetOrdinal("Id")) : 0;
									previousVersion.IPFSHash = !rdr.IsDBNull(rdr.GetOrdinal("ParentIPFSCID")) ? rdr.GetString(rdr.GetOrdinal("ipfscid")) : rdr.GetString(rdr.GetOrdinal("ipfscid"));
									previousVersion.ParentIPFSCid = rdr.GetString(rdr.GetOrdinal("ParentIPFSCid"));
									previousVersion.Name = rdr.GetString(rdr.GetOrdinal("Name"));
									previousVersion.Summary = rdr.GetString(rdr.GetOrdinal("Summary"));
									double dateAdded = double.Parse(rdr.GetString(rdr.GetOrdinal("dateadded")));
									previousVersion.DateAdded = UnixTimeStampToDateTime(dateAdded).ToString("MMM dd, yyyy HH:mm:ss");
									previousVersion.ProposalHash = rdr.GetString(rdr.GetOrdinal("proposalhash"));
									if (phash == previousVersion.ProposalHash)
									{
										previousVersion.Summary = previousVersion.Summary + " <b>(Current document)</b>";
									}
									previousVersion.cidtype = rdr.GetString(rdr.GetOrdinal("cidtype")); ;

									previousVersions.Add(previousVersion);  //Add to list
								}
							}
							conn.Close();
						}
					}
				}

				if (previousVersions.Count > 1)
				{
					//Dedup.
					List<PreviousVersionsModel> AllVersions = new List<PreviousVersionsModel>();
					AllVersions = previousVersions.GroupBy(x => x.IPFSHash).Select(x => x.Last()).ToList();
					AllVersions = AllVersions.GroupBy(x => x.DateAdded).Select(x => x.Last()).ToList();
					//Sort
					if (AllVersions != null && AllVersions.Count > 0)
						AllVersions = AllVersions.OrderBy(p => p.DateAdded).ToList();
					return Json(JsonConvert.SerializeObject(AllVersions));
				}
				else
				{
					dynamic prepJson = JObject.Parse("{success: false}");
					return Json(prepJson);
				}
			}
			catch (Exception ex)
			{
				dynamic prepBadRespJson = JObject.Parse("{success: false}");
				return Json(prepBadRespJson);
			}


		}

		public JsonResult GetVersions(string phash, int level)
		{
			List<ProposalModel> AllRecords = LoadAllRecords();
			string IPFSHash = "";
			string ParentIPFSCid = "", url = "";
			List<PreviousVersionsModel> previousVersions = new List<PreviousVersionsModel>();

			foreach (var record in AllRecords)
			{

					//Get current record from All Records
				if (record.Hash == phash)
				{
					PreviousVersionsModel previousVersion = new PreviousVersionsModel();
					previousVersion.IPFSHash = record.ParentIPFSCID;
					if (string.IsNullOrEmpty(previousVersion.IPFSHash))
					{
						IPFSHash = record.ProposalDescriptionUrl;
					}
					else
					{
						IPFSHash = previousVersion.IPFSHash;
					}

					previousVersion.Name = record.ProposalName;
					previousVersion.Summary = record.ProposalSummary;
					previousVersion.DateAdded = record.ProposalDate;
					previousVersion.ProposalHash = record.Hash;
					previousVersion.cidtype = record.cidtype;
					previousVersion.ParentIPFSCid = record.ParentIPFSCID;
					previousVersions.Add(previousVersion);  //Add to list
				}
	
			}

			foreach (var record in AllRecords)
			{
				if (record.cidtype == "0")
				{
					//Find other records that ParentIPFSCID == URL
					if (record.ProposalDescriptionUrl == IPFSHash)
					{
						PreviousVersionsModel previousVersion = new PreviousVersionsModel();
						previousVersion.IPFSHash = record.ParentIPFSCID;
						previousVersion.Name = record.ProposalName;
						previousVersion.Summary = record.ProposalSummary;
						previousVersion.DateAdded = record.ProposalDate;
						previousVersion.ProposalHash = record.Hash;
						previousVersion.cidtype = record.cidtype;
						previousVersion.ParentIPFSCid = record.ParentIPFSCID;
						previousVersions.Add(previousVersion);  //Add to list
					}
				}
			}

			foreach (var record in AllRecords)
			{
				if (record.cidtype == "0")
				{
					//Find other records that ParentIPFSCID == URL
					if (record.ParentIPFSCID == IPFSHash)
					{
						PreviousVersionsModel previousVersion = new PreviousVersionsModel();
						previousVersion.IPFSHash = record.ParentIPFSCID;
						previousVersion.Name = record.ProposalName;
						previousVersion.Summary = record.ProposalSummary;
						previousVersion.DateAdded = record.ProposalDate; 
						previousVersion.ProposalHash = record.Hash;
						previousVersion.cidtype = record.cidtype;
						previousVersion.ParentIPFSCid = record.ParentIPFSCID;
						previousVersions.Add(previousVersion);  //Add to list
					}
				}
			}

			if (previousVersions.Count > 0)
			{
				//Dedup.
				List<PreviousVersionsModel> AllVersions = new List<PreviousVersionsModel>();
				AllVersions = previousVersions.GroupBy(x => x.ProposalHash).Select(x => x.Last()).ToList();
				AllVersions = AllVersions.GroupBy(x => x.DateAdded).Select(x => x.Last()).ToList();
				//Sort
				if (AllVersions != null && AllVersions.Count > 0)
					AllVersions = AllVersions.OrderBy(p => p.DateAdded).ToList();

				return Json(JsonConvert.SerializeObject(AllVersions));
			}

			//return Json(JsonConvert.SerializeObject(previousVersions));
			dynamic prepBadRespJson1 = JObject.Parse("{success: false}");
			return Json(prepBadRespJson1);

		}
		public JsonResult GetEvidence(string phash, int level)
		{
			List<ProposalModel> AllRecords = LoadAllRecords();
			string IPFSHash = "";
			string ParentIPFSCid = "", url = "";
			List<PreviousVersionsModel> previousVersions = new List<PreviousVersionsModel>();

			foreach (var record in AllRecords)
			{

					//Get current record from All Records
				if (record.Hash == phash)
				{
					PreviousVersionsModel previousVersion = new PreviousVersionsModel();
					previousVersion.IPFSHash = record.ParentIPFSCID;
					if (string.IsNullOrEmpty(previousVersion.IPFSHash))
					{
						IPFSHash = record.ProposalDescriptionUrl;
					}
					else
					{
						IPFSHash = previousVersion.IPFSHash;
					}

					previousVersion.Name = record.ProposalName;
					previousVersion.Summary = record.ProposalSummary;
					previousVersion.DateAdded = record.ProposalDate;
					previousVersion.ProposalHash = record.Hash;
					previousVersion.cidtype = record.cidtype;
					previousVersion.ParentIPFSCid = record.ParentIPFSCID;
					previousVersions.Add(previousVersion);  //Add to list
				}

			}

			foreach (var record in AllRecords)
			{
				if (record.cidtype == "1")
				{
					//Find other records that ParentIPFSCID == URL
					if (record.ProposalDescriptionUrl == IPFSHash)
					{
						PreviousVersionsModel previousVersion = new PreviousVersionsModel();
						previousVersion.IPFSHash = record.ParentIPFSCID;
						previousVersion.Name = record.ProposalName;
						previousVersion.Summary = record.ProposalSummary;
						previousVersion.DateAdded = record.ProposalDate;
						previousVersion.ProposalHash = record.Hash;
						previousVersion.cidtype = record.cidtype;
						previousVersion.ParentIPFSCid = record.ParentIPFSCID;
						previousVersions.Add(previousVersion);  //Add to list
					}
				}
			}

			foreach (var record in AllRecords)
			{
				if (record.cidtype == "1")
				{
					//Find other records that ParentIPFSCID == URL
					if (record.ParentIPFSCID == IPFSHash)
					{
						PreviousVersionsModel previousVersion = new PreviousVersionsModel();
						previousVersion.IPFSHash = record.ParentIPFSCID;
						previousVersion.Name = record.ProposalName;
						previousVersion.Summary = record.ProposalSummary;
						previousVersion.DateAdded = record.ProposalDate;
						previousVersion.ProposalHash = record.Hash;
						previousVersion.cidtype = record.cidtype;
						previousVersion.ParentIPFSCid = record.ParentIPFSCID;
						previousVersions.Add(previousVersion);  //Add to list
					}
				}
			}

			if (previousVersions.Count > 0)
			{
				//Dedup.
				List<PreviousVersionsModel> AllVersions = new List<PreviousVersionsModel>();
				AllVersions = previousVersions.GroupBy(x => x.ProposalHash).Select(x => x.Last()).ToList();
				AllVersions = AllVersions.GroupBy(x => x.DateAdded).Select(x => x.Last()).ToList();
				//Sort
				if (AllVersions != null && AllVersions.Count > 0)
					AllVersions = AllVersions.OrderBy(p => p.DateAdded).ToList();

				return Json(JsonConvert.SerializeObject(AllVersions));
			}

			//return Json(JsonConvert.SerializeObject(previousVersions));
			dynamic prepBadRespJson1 = JObject.Parse("{success: false}");
			return Json(prepBadRespJson1);

		}
		public JsonResult GetTopics(string phash, int level)
		{
			List<ProposalModel> AllRecords = LoadAllRecords();
			string IPFSHash = "";
			string ParentIPFSCid = "", url = "";
			List<PreviousVersionsModel> previousVersions = new List<PreviousVersionsModel>();

			foreach (var record in AllRecords)
			{

					//Get current record from All Records
					if (record.Hash == phash)
					{
						PreviousVersionsModel previousVersion = new PreviousVersionsModel();
						previousVersion.IPFSHash = record.ParentIPFSCID;
						if (string.IsNullOrEmpty(previousVersion.IPFSHash))
						{
							IPFSHash = record.ProposalDescriptionUrl;
						}
						else
						{
							IPFSHash = previousVersion.IPFSHash;
						}

						previousVersion.Name = record.ProposalName;
						previousVersion.Summary = record.ProposalSummary;
						previousVersion.DateAdded = record.ProposalDate; 
						previousVersion.ProposalHash = record.Hash;
						previousVersion.cidtype = record.cidtype;
						previousVersion.ParentIPFSCid = record.ParentIPFSCID;
						previousVersions.Add(previousVersion);  //Add to list
					}
				
			}

			foreach (var record in AllRecords)
			{
				if (record.cidtype == "2")
				{
					//Find other records that ParentIPFSCID == URL
					if (record.ProposalDescriptionUrl == IPFSHash)
					{
						PreviousVersionsModel previousVersion = new PreviousVersionsModel();
						previousVersion.IPFSHash = record.ParentIPFSCID;
						previousVersion.Name = record.ProposalName;
						previousVersion.Summary = record.ProposalSummary;
						previousVersion.DateAdded = record.ProposalDate;
						previousVersion.ProposalHash = record.Hash;
						previousVersion.cidtype = record.cidtype;
						previousVersion.ParentIPFSCid = record.ParentIPFSCID;
						previousVersions.Add(previousVersion);  //Add to list
					}
				}
			}

			foreach (var record in AllRecords)
			{
				if (record.cidtype == "2")
				{
					//Find other records that ParentIPFSCID == URL
					if (record.ParentIPFSCID == IPFSHash)
					{
						PreviousVersionsModel previousVersion = new PreviousVersionsModel();
						previousVersion.IPFSHash = record.ParentIPFSCID;
						previousVersion.Name = record.ProposalName;
						previousVersion.DateAdded = record.ProposalDate;
						previousVersion.ProposalHash = record.Hash;
						previousVersion.cidtype = record.cidtype;
						previousVersion.ParentIPFSCid = record.ParentIPFSCID;
						previousVersions.Add(previousVersion);  //Add to list
					}
				}
			}

			if (previousVersions.Count > 0)
			{
				//Dedup.
				List<PreviousVersionsModel> AllVersions = new List<PreviousVersionsModel>();
				AllVersions = previousVersions.GroupBy(x => x.ProposalHash).Select(x => x.Last()).ToList();
				AllVersions = AllVersions.GroupBy(x => x.DateAdded).Select(x => x.Last()).ToList();
				//Sort
				if (AllVersions != null && AllVersions.Count > 0)
					AllVersions = AllVersions.OrderBy(p => p.DateAdded).ToList();

				return Json(JsonConvert.SerializeObject(AllVersions));
			}

			//return Json(JsonConvert.SerializeObject(previousVersions));
			dynamic prepBadRespJson1 = JObject.Parse("{success: false}");
			return Json(prepBadRespJson1);

		}



		public int OG(string url, string ipfscid)
		{
			var sanitizer = new HtmlSanitizer();
			try
			{

				string desc = "", imageurl = "", type = "", title = "", urltmp = "", sitename = "";
				int PURL = 0, id = 0;
				string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
				using (var conn = new SqliteConnection(connectionString))
				{
					using (var cmd = conn.CreateCommand())
					{

						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT description, imageurl, type, title, sitename, url, id FROM oglinks where url = @url LIMIT 1";
						cmd.Parameters.AddWithValue("@url", url);
						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							if (rdr.Read())
							{
								desc = rdr.GetString(rdr.GetOrdinal("description"));
								imageurl = rdr.GetString(rdr.GetOrdinal("imageurl"));
								type = rdr.GetString(rdr.GetOrdinal("type"));
								title = rdr.GetString(rdr.GetOrdinal("title"));
								urltmp = rdr.GetString(rdr.GetOrdinal("url"));
								sitename = rdr.GetString(rdr.GetOrdinal("sitename"));
								id = rdr.GetInt32(rdr.GetOrdinal("id"));
								PURL = 1;
							}
						}
					}
				}

				if (PURL == 0)
				{
					OpenGraph graph = OpenGraph.ParseUrl(url);
					if (graph.Metadata.Count != 0)
					{
						if (graph.Metadata.ContainsKey("og:image"))
						{
							imageurl = graph.Metadata["og:image"].First().Value;
						}
						if (graph.Metadata.ContainsKey("og:type"))
						{
							type = graph.Type;
						}
						if (graph.Metadata.ContainsKey("og:title"))
						{
							title = graph.Title;
						}
						if (graph.Metadata.ContainsKey("og:url"))
						{
							urltmp = graph.Metadata["og:url"].First().Value;
						}
						if (graph.Metadata.ContainsKey("og:site_name"))
						{
							sitename = graph.Metadata["og:site_name"].First().Value;
						}

						if (graph.Metadata.ContainsKey("description"))
						{
							desc = graph.Metadata["description"].First().Value;
						}
						else if (graph.Metadata.ContainsKey("twitter:description"))
						{
							desc = graph.Metadata["twitter:description"].First().Value;
						}
						else if (graph.Metadata.ContainsKey("og:description"))
						{
							desc = graph.Metadata["og:description"].First().Value;
						}

						if (urltmp != "")
						{
							using (var conn = new SqliteConnection(connectionString))
							{
								using (var cmd = conn.CreateCommand())
								{
									conn.Open();
									cmd.CommandType = System.Data.CommandType.Text;
									cmd.CommandText = "INSERT OR IGNORE INTO oglinks (url, description, imageurl, type, title, sitename, ipfscid) VALUES(@url, @desc, @imageurl, @type, @title, @sitename);  SELECT LAST_INSERT_ID() as InsertedId";
									cmd.Parameters.AddWithValue("@url", urltmp);
									cmd.Parameters.AddWithValue("@desc", sanitizer.Sanitize(desc));
									cmd.Parameters.AddWithValue("@imageurl", imageurl);
									cmd.Parameters.AddWithValue("@type", type);
									cmd.Parameters.AddWithValue("@title", sanitizer.Sanitize(title));
									cmd.Parameters.AddWithValue("@sitename", sitename);
									cmd.Parameters.AddWithValue("@ipfscid", ipfscid);
									cmd.ExecuteNonQuery();
									id = Convert.ToInt32(cmd.ExecuteScalar());
								}
							}
						}
					}
					else
					{
						return 0;
					}
				}

				return id;

			}
			catch (Exception ex)
			{

				return 0;
			}
		}

		public static async Task<string> CallAPI(string url)
		{
			string data = "";
			using (HttpClient client = new HttpClient())
			{
				client.BaseAddress = new Uri(url);
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				try
				{
					HttpResponseMessage response = client.GetAsync(url).Result;
					if (response.IsSuccessStatusCode)
					{
						string result = await response.Content.ReadAsStringAsync();
						data = JsonConvert.DeserializeObject(result).ToString();
						return data;
					}
					else
					{
						throw new Exception($"Failed to call API. StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);

				}

				return data;
			}
		}

		public class OGA
		{
			public string isArchive { get; set; }
			public string OGUrl { get; set; }

		}

		public async Task<IActionResult> ProposalDetails(string hash, string sort, string cid, string url)
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
				pm.ParentIPFSCID = string.IsNullOrEmpty(proposal1.ipfspid.ToString()) ? "" : proposal1.ipfspid.ToString();

				if (!string.IsNullOrEmpty(pm.ParentIPFSCID))
				{
					if (string.IsNullOrEmpty(proposal1.ipfscidtype?.ToString()))
					{
						pm.cidtype = "0";
					}
					else
					{
						pm.cidtype = proposal1.ipfscidtype.ToString();
					}
					if (pm.PermLocked)
					{
						pm.IsUpdate = "0";
					}
					else
					{
						pm.IsUpdate = "1";
					}
				}
				else
				{
					pm.cidtype = "1";
					if (pm.PermLocked)
					{
						pm.IsUpdate = "0";
					}
					else
					{
						pm.IsUpdate = "1";
					}
				}

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


				if (pm.Type == "4")
				{
					try {
						string jsonString = CallAPI("https://historia.network/home/rogai?ipfs=" + proposal1.ipfscid.ToString()).GetAwaiter().GetResult();
						OGA data = JsonConvert.DeserializeObject<OGA>(jsonString);
						if (data.isArchive == "1")
						{
							pm.Type = "5";
							pm.orgUrl = data.OGUrl;
						}
					} catch (Exception ex)
					{

					}

				}

				int Oid = OG(pm.orgUrl, proposal1.ipfscid.ToString());


				string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
				using (var conn = new SqliteConnection(connectionString))
				{
					using (var cmd = conn.CreateCommand())
					{
						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT * FROM oglinks where Id = @Oid";
						cmd.Parameters.AddWithValue("@Oid", Oid);
						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							if (rdr.Read())
							{
								pm.oglinksid = rdr.GetInt32(rdr.GetOrdinal("Id"));
								pm.oglinksimageurl = rdr.GetString(rdr.GetOrdinal("imageurl"));
								pm.oglinkstitle = rdr.GetString(rdr.GetOrdinal("title"));
								pm.oglinksurl = rdr.GetString(rdr.GetOrdinal("url"));
								pm.oglinkssitename = rdr.GetString(rdr.GetOrdinal("sitename"));
								pm.oglinksdescription = rdr.GetString(rdr.GetOrdinal("description"));
							}
						}
					}
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
