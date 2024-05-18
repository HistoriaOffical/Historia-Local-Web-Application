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
using SQLitePCL;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;



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
		public JsonResult ImportHistoriaClientRecords(string value)
		{
			ToggleDeepSearch();
			try
			{
				string rpcServerUrl = "http://" + ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;
				HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(rpcServerUrl);
				webRequest.Credentials = new NetworkCredential(_userName, _password);
				webRequest.ContentType = "application/json-rpc";
				webRequest.Method = "POST";
				webRequest.Timeout = 5000;
				string jsonstring = "";
				jsonstring = String.Format("{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [\"list\", \"all\", \"all\"] }}");

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

						//Get Datastring Info
						var ds = record.Value.DataString;
						string p1 = ds;
						dynamic proposal1 = JObject.Parse(p1);
						string hash = "";
						hash = record.Value.Hash;
						string hostname = _ipfsUrl;
						string cidtype;
						if (proposal1.ipfscidtype != null && !string.IsNullOrEmpty(proposal1.ipfscidtype.ToString()))
						{
							cidtype = proposal1.ipfscidtype.ToString();
						} else
						{
							cidtype = "0";
						}

						string payment_amount =  proposal1.payment_amount.ToString();
						string payment_address =  proposal1.payment_address.ToString();
						string dateadded = proposal1.start_epoch.ToString();
						GetHtmlSourceAsync(HttpUtility.HtmlEncode(proposal1.summary.name.ToString()), HttpUtility.HtmlEncode(proposal1.summary.description.ToString()), "https://" + hostname + "/ipfs/" + proposal1.ipfscid.ToString() + "/index.html", proposal1.ipfscid.ToString(), hash, proposal1.ipfspid.ToString(), cidtype, payment_amount, payment_address, dateadded);

					}
				}

				var rep = new { Success = true, toggle = GetDeepSearch() };
				return Json(rep);
			}
			catch (Exception ex)
			{
				_logger.LogCritical("ProposalDescription Error: " + ex.ToString());
				var rep = new { Success = false, toggle = 0 };
				return Json(rep);
			}
		}

		private int ToggleDeepSearch()
		{
			int toggleValue = 0;
			string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
			using (var conn = new SqliteConnection(connectionString))
			{
				try
				{
					using (var cmd = conn.CreateCommand())
					{
						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "UPDATE basexConfiguration SET DeepSearch = NOT DeepSearch WHERE Id = 1";
						cmd.ExecuteNonQuery();
					}

					using (var cmd = conn.CreateCommand())
					{

						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT DeepSearch FROM basexConfiguration where Id = 1";
						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							if (rdr.Read())
							{
								toggleValue = rdr.GetInt32(rdr.GetOrdinal("DeepSearch"));
							}
						}
					}


					return toggleValue;
				}
				catch (Exception ex)
				{
					return toggleValue;
				}
			}
		}

		[HttpGet]
		public JsonResult ToggleIpfsApiValue()
		{
			int toggleValue = 0;
			string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
			using (var conn = new SqliteConnection(connectionString))
			{
				try
				{
					using (var cmd = conn.CreateCommand())
					{
						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "UPDATE basexConfiguration SET IpfsApi = NOT IpfsApi WHERE Id = 1";
						cmd.ExecuteNonQuery();
					}

					using (var cmd = conn.CreateCommand())
					{

						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT IpfsApi FROM basexConfiguration where Id = 1";
						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							if (rdr.Read())
							{
								toggleValue = rdr.GetInt32(rdr.GetOrdinal("IpfsApi"));
							}
						}
					}


					var rep = new { Success = true, value = toggleValue };
					return Json(rep);
				}
				catch (Exception ex)
				{
					var rep = new { Success = false, value = toggleValue };
					return Json(rep);
				}
			}
		}

		[HttpGet]
		public JsonResult ToggleDeepValue()
		{
			int toggleValue = 0;
			string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
			using (var conn = new SqliteConnection(connectionString))
			{
				try
				{
					using (var cmd = conn.CreateCommand())
					{
						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "UPDATE basexConfiguration SET DeepSearch = NOT DeepSearch WHERE Id = 1";
						cmd.ExecuteNonQuery();
					}

					using (var cmd = conn.CreateCommand())
					{

						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT DeepSearch FROM basexConfiguration where Id = 1";
						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							if (rdr.Read())
							{
								toggleValue = rdr.GetInt32(rdr.GetOrdinal("DeepSearch"));
							}
						}
					}


					var rep = new { Success = true, value = toggleValue };
					return Json(rep);
				}
				catch (Exception ex)
				{
					var rep = new { Success = false, value = toggleValue };
					return Json(rep);
				}
			}
		}

		[HttpGet]
		public JsonResult GetDeepValue()
		{
			int toggleValue = 0;
			string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
			using (var conn = new SqliteConnection(connectionString))
			{
				try
				{
					using (var cmd = conn.CreateCommand())
					{

						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT DeepSearch FROM basexConfiguration where Id = 1";
						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							if (rdr.Read())
							{
								toggleValue = rdr.GetInt32(rdr.GetOrdinal("DeepSearch"));
							}
						}
					}


					var rep = new { Success = true, value = toggleValue };
					return Json(rep);
				}
				catch (Exception ex)
				{
					var rep = new { Success = false, value = 0 };
					return Json(rep);
				}
			}
		}

		[HttpGet]
		public JsonResult GetIpfsApiValue()
		{
			int toggleValue = 0;
			string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
			using (var conn = new SqliteConnection(connectionString))
			{
				try
				{
					using (var cmd = conn.CreateCommand())
					{

						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT IpfsApi FROM basexConfiguration where Id = 1";
						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							if (rdr.Read())
							{
								toggleValue = rdr.GetInt32(rdr.GetOrdinal("IpfsApi"));
							}
						}
					}


					var rep = new { Success = true, value = toggleValue };
					return Json(rep);
				}
				catch (Exception ex)
				{
					var rep = new { Success = false, value = 0 };
					return Json(rep);
				}
			}
		}
		
		private static int GetDeepSearch()
		{
			int toggleValue = 0;
			string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
			using (var conn = new SqliteConnection(connectionString))
			{
				try
				{
					using (var cmd = conn.CreateCommand())
					{

						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT DeepSearch FROM basexConfiguration where Id = 1";
						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							if (rdr.Read())
							{
								toggleValue = rdr.GetInt32(rdr.GetOrdinal("DeepSearch"));
							}
						}
					}


					return toggleValue;
				}
				catch (Exception ex)
				{
					return toggleValue;
				}
			}
		}


		private static Dictionary<string, string> ReadHistoriaConfig(string filePath)
		{
			var config = new Dictionary<string, string>();

			using (StreamReader reader = new StreamReader(filePath))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					// Ignore comments and empty lines
					if (!string.IsNullOrEmpty(line) && !line.Trim().StartsWith("#"))
					{
						string[] parts = line.Split('=');
						if (parts.Length == 2)
						{
							string key = parts[0].Trim();
							string value = parts[1].Trim();
							config[key] = value;  // Add or update dictionary entry
						}
					}
				}
			}

			return config;
		}



		static string[] GetCommandsBasedOnOS()
		{
			string ipfsDir = GetWorkingDirectoryBasedOnOS();
			string ipfsExecutable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ipfs.exe" : "./ipfs";
			string ipfsPath = System.IO.Path.Combine(ipfsDir, ipfsExecutable);

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return new string[]
				{
					$"{ipfsPath} init",
					$"{ipfsPath} bootstrap add /ip4/202.182.119.4/tcp/4001/ipfs/QmVjkn7yEqb3LTLCpnndHgzczPAPAxxpJ25mNwuuaBtFJD",
					$"{ipfsPath} bootstrap add /ip4/149.28.22.65/tcp/4001/ipfs/QmZkRv4qfXvtHot37STR8rJxKg5cDKFnkF5EMh2oP6iBVU",
					$"{ipfsPath} bootstrap add /ip4/149.28.247.81/tcp/4001/ipfs/QmcvrQ8LpuMqtjktwXRb7Mm6JMCqVdGz6K7VyQynvWRopH",
					$"{ipfsPath} bootstrap add /ip4/45.32.194.49/tcp/4001/ipfs/QmZXbb5gRMrpBVe79d8hxPjMFJYDDo9kxFZvdb7b2UYamj",
					$"{ipfsPath} bootstrap add /ip4/45.76.236.45/tcp/4001/ipfs/QmeW8VxxZjhZnjvZmyBqk7TkRxrRgm6aJ1r7JQ51ownAwy",
					$"{ipfsPath} bootstrap add /ip4/209.250.233.69/tcp/4001/ipfs/Qma946d7VCm8v2ny5S2wE7sMFKg9ZqBXkkZbZVVxjJViyu",
					$"{ipfsPath} config --json Datastore.StorageMax \"50GB\"",
					$"{ipfsPath} config --json Gateway.HTTPHeaders.Access-Control-Allow-Headers \"[\\\"X-Requested-With\\\", \\\"Access-Control-Expose-Headers\\\", \\\"Range\\\", \\\"Authorization\\\"]\"",
					$"{ipfsPath} config --json Gateway.HTTPHeaders.Access-Control-Allow-Methods \"[\\\"POST\\\", \\\"GET\\\"]\"",
					$"{ipfsPath} config --json Gateway.HTTPHeaders.Access-Control-Allow-Origin \"[\\\"*\\\"]\"",
					$"{ipfsPath} config --json Gateway.HTTPHeaders.Access-Control-Expose-Headers \"[\\\"Location\\\", \\\"Ipfs-Hash\\\"]\"",
					$"{ipfsPath} config --json Gateway.HTTPHeaders.X-Special-Header \"[\\\"Access-Control-Expose-Headers: Ipfs-Hash\\\"]\"",
					$"{ipfsPath} config --json Gateway.NoFetch \"false\"",
					$"{ipfsPath} config --json Swarm.ConnMgr.HighWater \"500\"",
					$"{ipfsPath} config --json Swarm.ConnMgr.LowWater \"200\"",
					$"{ipfsPath} config --json Datastore.StorageMax \"50GB\"",
					$"{ipfsPath} config --json Gateway.Writable true",
				};
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{

				string scriptPath = "/Applications/Historia-Qt.app/Contents/Resources/ipfs/startipfs.sh";
				string createScriptCommand = $"/bin/bash -c 'printf \"#!/bin/bash\\n/Applications/Historia-Qt.app/Contents/Resources/ipfs/ipfs daemon\\nexec bash\\n\" > {scriptPath} && chmod +x {scriptPath}'";

				// Run the command to create the script
				Process createScriptProcess = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "/bin/bash",
						Arguments = $"-c \"{createScriptCommand.Replace("\"", "\\\"")}\"",
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true
					}
				};

				createScriptProcess.Start();
				createScriptProcess.WaitForExit();
				Console.WriteLine("SCRIPT: " + createScriptCommand);
				if (createScriptProcess.ExitCode != 0)
				{
					string error = createScriptProcess.StandardError.ReadToEnd();
					Console.WriteLine("Error creating script: " + error);
				}

				return new string[]
				{
					$"{ipfsPath} init -p server",
					$"{ipfsPath} bootstrap add /ip4/202.182.119.4/tcp/4001/ipfs/QmVjkn7yEqb3LTLCpnndHgzczPAPAxxpJ25mNwuuaBtFJD",
					$"{ipfsPath} bootstrap add /ip4/149.28.22.65/tcp/4001/ipfs/QmZkRv4qfXvtHot37STR8rJxKg5cDKFnkF5EMh2oP6iBVU",
					$"{ipfsPath} bootstrap add /ip4/149.28.247.81/tcp/4001/ipfs/QmcvrQ8LpuMqtjktwXRb7Mm6JMCqVdGz6K7VyQynvWRopH",
					$"{ipfsPath} bootstrap add /ip4/45.32.194.49/tcp/4001/ipfs/QmZXbb5gRMrpBVe79d8hxPjMFJYDDo9kxFZvdb7b2UYamj",
					$"{ipfsPath} bootstrap add /ip4/45.76.236.45/tcp/4001/ipfs/QmeW8VxxZjhZnjvZmyBqk7TkRxrRgm6aJ1r7JQ51ownAwy",
					$"{ipfsPath} bootstrap add /ip4/209.250.233.69/tcp/4001/ipfs/Qma946d7VCm8v2ny5S2wE7sMFKg9ZqBXkkZbZVVxjJViyu",
					$"{ipfsPath} config --json Datastore.StorageMax '\"50GB\"'",
					$"{ipfsPath} config --json Gateway.HTTPHeaders.Access-Control-Allow-Headers '[\\\"X-Requested-With\\\", \\\"Access-Control-Expose-Headers\\\", \\\"Range\\\", \\\"Authorization\\\"]'",

					$"{ipfsPath} config --json Gateway.HTTPHeaders.Access-Control-Allow-Methods '[\\\"POST\\\",\\\"GET\\\"]'",
					$"{ipfsPath} config --json Gateway.HTTPHeaders.Access-Control-Allow-Origin '[\\\"*\\\"]'",
					$"{ipfsPath} config --json Gateway.HTTPHeaders.Access-Control-Expose-Headers '[\\\"Location\\\", \\\"Ipfs-Hash\\\"]'",
					$"{ipfsPath} config --json Gateway.HTTPHeaders.X-Special-Header '[\\\"Access-Control-Expose-Headers: Ipfs-Hash\\\"]'",
					$"{ipfsPath} config --json Gateway.NoFetch false",
					$"{ipfsPath} config --json Swarm.ConnMgr.HighWater '500'",
					$"{ipfsPath} config --json Swarm.ConnMgr.LowWater '200'",
					$"{ipfsPath} config --json Gateway.Writable true",
				};
			} 
			else
			{
				throw new InvalidOperationException("Unsupported operating system");
			}
		}

		static string GetShellName()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return "cmd.exe";
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return "/bin/zsh";
			}
			else
			{
				return "/bin/bash";
			}
		}

		static string GetShellArguments(string command)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return $"/c {command}";
			}
			else
			{
				return $"-c \"{command}\"";
			}
		}

		private static string GetWorkingDirectoryBasedOnOS()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return @"C:\Program Files\HistoriaCore\ipfs";
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return @"/path/to/ipfs/unix";
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return @"/Applications/Historia-Qt.app/Contents/Resources/ipfs";
			}
			else
			{
				throw new NotSupportedException("Unsupported operating system");
			}
		}

		[HttpGet]
		public async Task<JsonResult> InitializeHLWA()
		{
			int initialized = 0;

			string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
			using (var conn = new SqliteConnection(connectionString))
			{
				try
				{
					conn.Open();
					using (var cmd = conn.CreateCommand())
					{
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT InitializedHLWA FROM basexConfiguration WHERE Id = 1";
						using (SqliteDataReader rdr = await cmd.ExecuteReaderAsync())
						{
							if (rdr.Read())
							{
								initialized = rdr.GetInt32(rdr.GetOrdinal("InitializedHLWA"));
								Console.WriteLine("InitializeHLWA::initialized:" + initialized);
							}
						}
					}

					if (initialized == 0)
					{
						string[] commandsToRun = GetCommandsBasedOnOS();
						string workingDirectory = GetWorkingDirectoryBasedOnOS();
						Console.WriteLine("InitializeHLWA::workingDirectory:" + workingDirectory);
						foreach (var command in commandsToRun)
						{
							var processStartInfo = new ProcessStartInfo
							{
								FileName = GetShellName(),
								Arguments = GetShellArguments(command),
								WorkingDirectory = workingDirectory,
								RedirectStandardOutput = true,
								RedirectStandardError = true,
								UseShellExecute = false,
								CreateNoWindow = true
							};

							var tcs = new TaskCompletionSource<bool>();
							using (var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true })
							{
								process.OutputDataReceived += (sender, e) =>
								{
									if (e.Data != null)
									{
										Console.WriteLine($"Output: {e.Data}");
									}
								};
								process.ErrorDataReceived += (sender, e) =>
								{
									if (e.Data != null)
									{
										Console.WriteLine($"ERROR: {e.Data}");
									}
								};

								process.Exited += (sender, e) => tcs.SetResult(true);

								Console.WriteLine($"Starting process: {processStartInfo.FileName} {processStartInfo.Arguments}");

								bool processStarted = process.Start();
								if (processStarted)
								{
									Console.WriteLine("Process started successfully.");
									process.BeginOutputReadLine();
									process.BeginErrorReadLine();

									await tcs.Task;  // Wait for the process to exit
								}
								else
								{
									Console.WriteLine("Failed to start process.");
								}
							}
						}

						initialized = 1;
						string configFilePath = GetConfigFilePath();

						var settings = ReadHistoriaConfig(configFilePath);
						Console.WriteLine("InitializeHLWA::ReadHistoriaConfig:" + configFilePath);
						ApplicationSettings.IPFSHost = "127.0.0.1"; // User must select a default server
						ApplicationSettings.IPFSPort = 443; // User must select a default server
						ApplicationSettings.IPFSApiHost = "127.0.0.1";
						ApplicationSettings.IPFSApiPort = 5001;
						ApplicationSettings.HistoriaClientIPAddress = "127.0.0.1";
						ApplicationSettings.HistoriaRPCPort = int.Parse(settings["rpcport"]);
						ApplicationSettings.HistoriaRPCUserName = settings["rpcuser"];
						ApplicationSettings.HistoriaRPCPassword = settings["rpcpassword"];
						ApplicationSettings.SaveConfig();
						Console.WriteLine("InitializeHLWA SAVED:");
						using (var cmd = conn.CreateCommand())
						{
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "UPDATE basexConfiguration SET InitializedHLWA = 1 WHERE Id = 1";
							await cmd.ExecuteNonQueryAsync();
						}
					}

					var rep = new { Success = true, value = initialized };
					Console.WriteLine("InitializeHLWA::success:" + initialized);
					return Json(rep);
				}
				catch (Exception ex)
				{
					var rep = new { Success = false, value = 0 };
					Console.WriteLine("InitializeHLWA FAILED:" + ex);
					return Json(rep);
				}
				finally
				{
					await conn.CloseAsync();
				}
			}
		}

		static string GetConfigFilePath()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				return Path.Combine(appDataPath, "HistoriaCore", "historia.conf");
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "historia.conf");
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				return Path.Combine(homePath, "Library", "Application Support", "HistoriaCore", "historia.conf");
			}
			else
			{
				throw new PlatformNotSupportedException("Unsupported OS");
			}
		}

		[DllImport("sqlite3.dll", EntryPoint = "sqlite3_load_extension")]
		private static extern int LoadExtension(sqlite3 db, string fileName, string procName, out string errMsg);

		[HttpGet]
		public JsonResult LoadRecords(string recordType, int pageIndex, string query, int numberToLoad = 5)
		{

			int? dbRecordType = null;
			switch (recordType)
			{
				case "proposals": dbRecordType = 1; break;
				case "records": dbRecordType = 4; break;
				case "tree": dbRecordType = 4; break;

			}
			if (recordType == "tree")
			{
				recordType = "records";
			}
			int toggle = GetDeepSearch();
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
					jsonstring = String.Format("{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [\"list\", \"all\", \"{0}\", \"{1}\", \"{2}\"] }}", "all", (pageIndex * 5) + 1, (pageIndex + 1) * 5);
				}
				else
				{
					jsonstring = String.Format("{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [\"list\", \"all\", \"{0}\", \"{1}\", \"{2}\"] }}", recordType, (pageIndex * 5) + 1, (pageIndex + 1) * numberToLoad);
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
						var en = new System.Globalization.CultureInfo("en-US");

						if (!string.IsNullOrEmpty(query) && toggle == 0)
						{

							// This is terribly ugly code. This was required as there were weird values that were not being found, even if the string value was exactly the same. Something to do with unicode values, but haven't solved it completely yet.
							//This code needs to be refactored, but it works currently.
							int compareLinguisticName = String.Compare(query, pm.ProposalName, en, System.Globalization.CompareOptions.IgnoreCase);
							int compareOrdinalName = String.Compare(query, pm.ProposalName, StringComparison.OrdinalIgnoreCase);
							int compareLinguisticSummary = String.Compare(query, pm.ProposalSummary, en, System.Globalization.CompareOptions.IgnoreCase);
							int compareOrdinalSummary = String.Compare(query, pm.ProposalSummary, StringComparison.OrdinalIgnoreCase);

							if (compareLinguisticName == 0)
							{
								goto run;
							}

							if (compareOrdinalName == 0)
							{
								goto run;
							}

							if (compareLinguisticSummary == 0)
							{
								goto run;
							}

							if (compareOrdinalSummary == 0)
							{
								goto run;
							}

							if (!pm.ProposalName.Contains(query, StringComparison.OrdinalIgnoreCase) || !pm.ProposalSummary.Contains(query, StringComparison.OrdinalIgnoreCase))
							{
								continue;
							}
						}
						else if (!string.IsNullOrEmpty(query) && toggle == 1)
						{

							try
							{
								string connectionString1 = $"Data Source={ApplicationSettings.DatabasePath}";
								using (var conn = new SqliteConnection(connectionString1))
								{

									using (var cmd = conn.CreateCommand())
									{
										conn.Open();
										cmd.CommandType = System.Data.CommandType.Text;
										cmd.CommandText = "SELECT ipfscid FROM items_fts WHERE ipfscid = @ipfscid AND (name MATCH @query OR summary MATCH @query OR html MATCH @query)";
										cmd.Parameters.AddWithValue("@ipfscid", proposal1.ipfscid.ToString());
										cmd.Parameters.AddWithValue("@query", query);
										using (SqliteDataReader rdr = cmd.ExecuteReader())
										{
											if (rdr.Read())
											{
												goto run;
											}
											else
											{
												continue;
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

					run:

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
							string jsonString = CallAPI("https://historia.network/home/rogai?ipfs=" + proposal1.ipfscid.ToString()).GetAwaiter().GetResult();
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
									string connectionString1 = $"Data Source={ApplicationSettings.DatabasePath}";
									using (var conn = new SqliteConnection(connectionString1))
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
													pm.oglinksurl = "https://" + hostname + "/ipfs/" + proposal1.ipfscid.ToString() + "/index.html";
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
						string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
						using (var conn = new SqliteConnection(connectionString))
						{
							using (var cmd = conn.CreateCommand())
							{
								conn.Open();
								cmd.CommandType = System.Data.CommandType.Text;
								cmd.CommandText = "SELECT id FROM items where proposalhash = @proposalhash";
								cmd.Parameters.AddWithValue("@proposalhash", pm.Hash);
								using (SqliteDataReader rdr = cmd.ExecuteReader())
								{
									if (rdr.Read())
									{
										pm.Id = rdr.GetInt32(rdr.GetOrdinal("id"));
									}
								}
							}
						}
						
						if (toggle == 1)
						{
							GetHtmlSourceAsync(pm.ProposalName, pm.ProposalSummary, pm.ProposalDescriptionUrlRazor, proposal1.ipfscid.ToString(), pm.Hash, pm.ParentIPFSCID, pm.cidtype, pm.PaymentAmount.ToString(), pm.PaymentAddress, proposal1.start_epoch.ToString());
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


		

		public static async Task<bool> GetHtmlSourceAsync(string Name, string Summary, string url, string ipfscid, string proposalhash, string ParentIPFSCID, string cidtype, string paymentAmount, string paymentAddress, string dateadded)
		{
			int toggle = GetDeepSearch();
			if (toggle == 0)
			{
				return false;
			}
			int count = 0;
			try
			{
				string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
				using (var conn = new SqliteConnection(connectionString))
				{
					using (var cmd = conn.CreateCommand())
					{

						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT COUNT(*) as count FROM items WHERE ipfscid = @ipfscid";
						cmd.Parameters.AddWithValue("@ipfscid", ipfscid);
						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							while (rdr.Read())
							{
								count = rdr.GetInt32(rdr.GetOrdinal("count")); ;
							}
						}
						if (count > 0)
						{
							return true;
						}
					}
				}
			}
			catch (Exception ex)
			{
				return false;
			}
			using (HttpClient client = new HttpClient())
			{
				using (HttpResponseMessage response = await client.GetAsync(url))
				{
					using (HttpContent content = response.Content)
					{
						string html = await content.ReadAsStringAsync();
						try
						{
							string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
							using (var conn = new SqliteConnection(connectionString))
							{
								using (var cmd = conn.CreateCommand())
								{
									conn.Open();
									cmd.CommandType = System.Data.CommandType.Text;
									cmd.CommandText = "INSERT OR IGNORE INTO items (Name, Summary, html, ipfscid, proposalhash, ParentIPFSCID, cidtype, PaymentAddress, PaymentAmount, dateadded, imported) VALUES (@Name, @Summary, @html, @ipfscid, @proposalhash, @ParentIPFSCID, @cidtype, @PaymentAddress, @PaymentAmount, @dateadded, 1)";
									cmd.Parameters.AddWithValue("Name", Name);
									cmd.Parameters.AddWithValue("Summary", Summary);
									cmd.Parameters.AddWithValue("html", html);
									cmd.Parameters.AddWithValue("ipfscid", ipfscid);
									cmd.Parameters.AddWithValue("proposalhash", proposalhash);
									cmd.Parameters.AddWithValue("ParentIPFSCID", ParentIPFSCID);
									cmd.Parameters.AddWithValue("cidtype", cidtype);
									cmd.Parameters.AddWithValue("PaymentAddress", paymentAddress);
									cmd.Parameters.AddWithValue("PaymentAmount", paymentAmount);
									cmd.Parameters.AddWithValue("dateadded", dateadded);
									cmd.ExecuteNonQuery();

								}
							}

						}
						catch (Exception ex)
						{
							Console.WriteLine(ex.Message);
							return false;
						}
					}
					return true;
				}

			}

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
							try
							{
								using (var conn = new SqliteConnection(connectionString))
								{
									using (var cmd = conn.CreateCommand())
									{
										//INSERT INTO followers(userid, followerid) VALUES(@userid, @followerid)";
										conn.Open();
										cmd.CommandType = System.Data.CommandType.Text;
										cmd.CommandText = "INSERT OR IGNORE INTO oglinks (url, description, imageurl, type, title, sitename, ipfscid) VALUES(@url, @desc, @imageurl, @type, @title, @sitename, @ipfscid);  SELECT last_insert_rowid();";
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
							catch (Exception ex)
							{
								return 0;
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

		public IActionResult Tokenomics()
		{
			return View();
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
		public IActionResult SaveMasternodeSettings([FromBody] SettingsParams settings)
		{

			ApplicationSettings.IPFSHost = settings.IPFSHost; 
			ApplicationSettings.IPFSPort = settings.IPFSPort;
			ApplicationSettings.SaveConfig();
			return Json(new { success = true });
		}

		[HttpPost]
		public IActionResult SaveCoreSettings([FromBody] SettingsParams settings)
		{
			ApplicationSettings.HistoriaClientIPAddress = settings.HistoriaClientIPAddress;
			ApplicationSettings.HistoriaRPCPort = settings.HistoriaRPCPort;
			ApplicationSettings.HistoriaRPCUserName = settings.HistoriaRPCUserName;
			ApplicationSettings.HistoriaRPCPassword = settings.HistoriaRPCPassword;
			ApplicationSettings.SaveConfig();
			return Json(new { success = true });
		}

		[HttpPost]
		public IActionResult SaveIpfsApiSettings([FromBody] SettingsParams settings)
		{
			ApplicationSettings.IPFSApiHost = settings.IPFSApiHost;
			ApplicationSettings.IPFSApiPort = settings.IPFSApiPort;
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

		[HttpGet]
		public async Task<IActionResult> TestIPFSAPIDefault()
		{
			try
			{

				Ipfs.Http.IpfsClient client = new Ipfs.Http.IpfsClient($"http://{ApplicationSettings.IPFSApiHost}:{ApplicationSettings.IPFSApiPort}");

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
