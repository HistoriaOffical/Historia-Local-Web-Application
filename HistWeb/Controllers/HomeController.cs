using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HistWeb.Models;
using HistWeb.Home.Models;
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
using System.Threading;
using Microsoft.AspNetCore.Http;
using System.Web;
using Newtonsoft.Json;
using Ipfs;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Drawing;
using HistWeb.Helpers;
using Microsoft.AspNetCore.Identity;
using OpenGraphNet;
using Ganss.XSS;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System.Data;
using HistWeb.Home.Views;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;


namespace HistWeb.Controllers
{
	public class HomeController : Controller
	{
		private string _rpcServerUrl = "http://" + ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;
		private string _userName = ApplicationSettings.HistoriaRPCUserName;
		private string _password = ApplicationSettings.HistoriaRPCPassword;
		private string _ipfsUrl = ApplicationSettings.IPFSHost;
		private string _ipfsWebPort = ApplicationSettings.IPFSPort.ToString();
		private readonly IHostingEnvironment _hostingEnvironment;
		private IConfiguration _configuration = null;
		private readonly ILogger _logger;
		private bool _isTestNet = true;

		public HomeController(IHostingEnvironment hostingEnvironment, IConfiguration config, ILoggerFactory loggerFactory, ILogger<HomeController> logger)
		{
			_hostingEnvironment = hostingEnvironment;
			_configuration = config;
			_logger = logger;

			_rpcServerUrl = "http://" + ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;
			_userName = ApplicationSettings.HistoriaRPCUserName;
			_password = ApplicationSettings.HistoriaRPCPassword;

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

		[HttpGet]
		public JsonResult LoadRecords(string recordType, int pageIndex, string query)
		{

			int? dbRecordType = null;
			switch (recordType)
			{
				case "proposals": dbRecordType = 1; break;
				case "records": dbRecordType = 4; break;

			}
			List<ProposalRecordModel> records = new List<ProposalRecordModel>();

			try
			{
				string rpcServerUrl = "http://" + ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;

				HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(rpcServerUrl);
				webRequest.Credentials = new NetworkCredential(_userName, _password);
				webRequest.ContentType = "application/json-rpc";
				webRequest.Method = "POST";
				webRequest.Timeout = 5000;
				string jsonstring = "";
				if (!string.IsNullOrEmpty(query))
				{
					jsonstring = String.Format("{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [\"list\", \"all\", \"{0}\", \"{1}\", \"{2}\"] }}", "all", pageIndex * 10, 10);
				}
				else
				{
					jsonstring = String.Format("{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [\"list\", \"all\", \"{0}\", \"{1}\", \"{2}\"] }}", recordType, pageIndex * 10, 10);
				}

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

						ProposalRecordModel pm = new ProposalRecordModel();

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

						if (!string.IsNullOrEmpty(query))
						{
							if (!pm.ProposalName.Contains(query, StringComparison.OrdinalIgnoreCase) || !pm.ProposalSummary.Contains(query, StringComparison.OrdinalIgnoreCase))
							{
								continue;
							}
						}


						pm.ProposalDescriptionUrl = proposal1.ipfscid.ToString();
						pm.PaymentAddress = proposal1.payment_address.ToString();
						pm.YesCount = long.Parse(record.Value.YesCount.ToString());
						pm.NoCount = long.Parse(record.Value.NoCount.ToString());
						pm.AbstainCount = long.Parse(record.Value.AbstainCount.ToString());
						pm.CachedLocked = bool.Parse(record.Value.fCachedLocked.ToString());

						pm.CachedFunding = bool.Parse(record.Value.fCachedFunding.ToString());
						pm.PaymentAmount = decimal.Parse(proposal1.payment_amount.ToString());
						pm.PaymentDate = UnixTimeStampToDateTime(double.Parse(proposal1.start_epoch.ToString())).ToString("MM/dd/yyyy");
						DateTime EndDateTemp = UnixTimeStampToDateTime(double.Parse(proposal1.end_epoch.ToString()));
						pm.PaymentEndDate = EndDateTemp.AddDays(-2);
						pm.ProposalDate = pm.PaymentDate;

						pm.PermLocked = bool.Parse(record.Value.fPermLocked.ToString());

						pm.ProposalDescriptionUrlRazor = "https://" + hostname + "/ipfs/" + proposal1.ipfscid.ToString() + "/index.html";
						pm.Type = proposal1.type.ToString();
						string OGUrl = "";
						if (pm.Type == "4")
						{
							string jsonString = CallAPI("https://www.historia.network/home/rogai?ipfs=" + proposal1.ipfscid.ToString()).GetAwaiter().GetResult();
							OGA data = JsonConvert.DeserializeObject<OGA>(jsonString);
							if (data.isArchive == "1")
							{
								pm.Type = "5";
								OGUrl = data.OGUrl;
							}
						}

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
							pm.cidtype = "1";
							pm.IsUpdate = "0";
						}
						if (!string.IsNullOrEmpty(OGUrl))
						{
							pm.oglinksid = OG(OGUrl, proposal1.ipfscid.ToString());
							try
							{
								if (pm.oglinksid != 0)
								{
									using (var conn = new SqliteConnection("Data Source=basex.db"))
									{
										using (var cmd = conn.CreateCommand())
										{
											conn.Open();
											cmd.CommandType = System.Data.CommandType.Text;
											cmd.CommandText = "SELECT * FROM oglinks where Id = @Oid";
											cmd.Parameters.AddWithValue("@Oid", pm.oglinksid);
											using (SqliteDataReader rdr = cmd.ExecuteReader())
											{
												if (rdr.Read())
												{
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
							}
							catch (Exception ex)
							{
								Console.WriteLine(ex.Message);
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
			List<ProposalRecordModel> sortedRecords = records.OrderByDescending(o => ((dynamic)JObject.Parse(o.DataString)).name).ToList();

			var rep = new { Success = true, Records = sortedRecords };
			return Json(rep);
		}

		public int OG(string url, string ipfscid)
		{
			var sanitizer = new HtmlSanitizer();
			try
			{

				string desc = "", imageurl = "", type = "", title = "", urltmp = "", sitename = "";
				int PURL = 0, id = 0;
				using (var conn = new SqliteConnection("Data Source=basex.db"))
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
							using (var conn = new SqliteConnection("Data Source=basex.db"))
							{
								using (var cmd = conn.CreateCommand())
								{
									//INSERT INTO followers(userid, followerid) VALUES(@userid, @followerid)";
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


		public IActionResult Index()
		{
			HomeViewModel model = new HomeViewModel();
			if (String.IsNullOrEmpty(ApplicationSettings.IPFSHost))
			{
				return Redirect("/Home/Settings");
			}
			return View(model);
		}


		public IActionResult Test()
		{
			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		public IActionResult Whitepaper()
		{
			return View();
		}

		public IActionResult About()
		{
			return View();
		}

		public IActionResult Wallets()
		{
			return View();
		}

		public IActionResult Team()
		{
			return View();
		}

		public IActionResult Settings()
		{
			SettingsModel model = new SettingsModel();

			model.IPFSHost = ApplicationSettings.IPFSHost;
			model.IPFSPort = ApplicationSettings.IPFSPort;
			model.IPFSApiHost = ApplicationSettings.IPFSApiHost;
			model.IPFSApiPort = ApplicationSettings.IPFSApiPort;
			model.HistoriaClientIPAddress = ApplicationSettings.HistoriaClientIPAddress;
			model.HistoriaRPCPort = ApplicationSettings.HistoriaRPCPort;
			model.HistoriaRPCUserName = ApplicationSettings.HistoriaRPCUserName;
			model.HistoriaRPCPassword = ApplicationSettings.HistoriaRPCPassword;
			return View(model);
		}

		public class SettingsParams
		{
			public string IPFSHost { get; set; }
			public int IPFSPort { get; set; }
			public string IPFSApiHost { get; set; }
			public int IPFSApiPort { get; set; }
			public string HistoriaClientIPAddress { get; set; }
			public int HistoriaRPCPort { get; set; }
			public string HistoriaRPCUserName { get; set; }
			public string HistoriaRPCPassword { get; set; }

		}

		[HttpPost]
		public IActionResult Settings([FromBody] SettingsParams settings)
		{
			ApplicationSettings.IPFSHost = settings.IPFSHost;
			ApplicationSettings.IPFSPort = settings.IPFSPort;
			ApplicationSettings.IPFSApiHost = settings.IPFSApiHost;
			ApplicationSettings.IPFSApiPort = settings.IPFSApiPort;
			ApplicationSettings.HistoriaClientIPAddress = settings.HistoriaClientIPAddress;
			ApplicationSettings.HistoriaRPCPort = settings.HistoriaRPCPort;
			ApplicationSettings.HistoriaRPCUserName = settings.HistoriaRPCUserName;
			ApplicationSettings.HistoriaRPCPassword = settings.HistoriaRPCPassword;
			ApplicationSettings.SaveConfig();

			return Json(new { success = true });
		}

		[HttpPost]
		public async Task<IActionResult> TestIPFSAsync([FromBody] SettingsParams settings)
		{
			try
			{
				Ipfs.Http.IpfsClient client = new Ipfs.Http.IpfsClient($"https://{settings.IPFSHost}:{settings.IPFSPort}");
				Dictionary<string, string> res = await client.VersionAsync();
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				return Json(new { success = false });
			}
		}

		[HttpPost]
		public async Task<IActionResult> TestIPFSAPI([FromBody] SettingsParams settings)
		{
			try
			{

				Ipfs.Http.IpfsClient client = new Ipfs.Http.IpfsClient($"http://{settings.IPFSApiHost}:{settings.IPFSApiPort}");

				string HtmlTest = "<html><body>Test Connection to IPFS API1</body></html>";
				string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", ""));
				Directory.CreateDirectory(tempDirectory);

				var filePath = tempDirectory;
				using (var stream = new FileStream(Path.Combine(filePath, "index.html"), FileMode.Create))
				{
					byte[] info = new UTF8Encoding(true).GetBytes(HtmlTest);
					stream.Write(info, 0, info.Length);
				}

				//Add test file/directory to IPFS API.
				Ipfs.CoreApi.AddFileOptions options = new Ipfs.CoreApi.AddFileOptions() { Pin = true };
				Ipfs.IFileSystemNode ret = await client.FileSystem.AddFileAsync(filePath + "/index.html", options);

				string IpfsCid = ret.Id.Hash.ToString();
				if (!string.IsNullOrEmpty(IpfsCid))
				{
					return Json(new { success = true });
				}
				else
				{
					return Json(new { success = false });
				}

			}
			catch (Exception ex)
			{
				return Json(new { success = false });
			}
		}

		[HttpPost]
		public IActionResult TestHistoriaClient([FromBody] SettingsParams settings)
		{
			try
			{
				string rpcServerUrl = "http://" + settings.HistoriaClientIPAddress + ":" + settings.HistoriaRPCPort;

				HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(rpcServerUrl);
				webRequest.Credentials = new NetworkCredential(settings.HistoriaRPCUserName, settings.HistoriaRPCPassword);
				webRequest.ContentType = "application/json-rpc";
				webRequest.Method = "POST";
				webRequest.Timeout = 5000;
				string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [\"list\"] }";

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
					return Json(new { success = true });
				}
			}
			catch (Exception ex)
			{
				return Json(new { success = false });

			}
			return Json(new { success = false });
		}

		public IActionResult SocialMedia()
		{
			return View();
		}

		public IActionResult HowTo()
		{
			return View();
		}


		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
