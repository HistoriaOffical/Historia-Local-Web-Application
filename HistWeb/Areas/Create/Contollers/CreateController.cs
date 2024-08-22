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
using Ganss.Xss;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using HtmlAgilityPack;
using System.Threading;
using System.Web;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Security.Cryptography;
using Ipfs;

namespace HistWeb.Controllers
{
	[Area("Create")]
	public class CreateController : Controller
	{

		private string _rpcServerUrl = "http://" + ApplicationSettings.HistoriaClientIPAddress + ":" + ApplicationSettings.HistoriaRPCPort;
		private string _userName = ApplicationSettings.HistoriaRPCUserName;
		private string _password = ApplicationSettings.HistoriaRPCPassword;
		private string _ipfsUrl = ApplicationSettings.IPFSHost;
		private string _ipfsApiPort = ApplicationSettings.IPFSApiPort.ToString();
		private string _ipfsWebPort = ApplicationSettings.IPFSPort.ToString();
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
				string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
				using (var conn = new SqliteConnection(connectionString))
				{
					using (var cmd = conn.CreateCommand())
					{
						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT * FROM items WHERE IsDraft == 1";

						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							while (rdr.Read())
							{
								var id = rdr.GetInt32(rdr.GetOrdinal("Id"));
								var Name = !rdr.IsDBNull(rdr.GetOrdinal("Name")) ? rdr.GetString(rdr.GetOrdinal("Name")) : "";
								var Summary = !rdr.IsDBNull(rdr.GetOrdinal("Summary")) ? rdr.GetString(rdr.GetOrdinal("Summary")) : "";
								var Type = !rdr.IsDBNull(rdr.GetOrdinal("Type")) ? rdr.GetString(rdr.GetOrdinal("Type")) : "";
								var cidType = !rdr.IsDBNull(rdr.GetOrdinal("cidtype")) ? rdr.GetInt32(rdr.GetOrdinal("cidtype")).ToString() : "";
								var pid = !rdr.IsDBNull(rdr.GetOrdinal("proposalhash")) ? rdr.GetString(rdr.GetOrdinal("proposalhash")).ToString() : "";
								var ipfspid = !rdr.IsDBNull(rdr.GetOrdinal("ParentIPFSCID")) ? rdr.GetString(rdr.GetOrdinal("ParentIPFSCID")).ToString() : "";
								var item = new { id = id, type = Type, template = "none", draftName = sanitizer.Sanitize(Name), draftSummary = sanitizer.Sanitize(Summary), cidtype = cidType, pid = pid, ipfspid = ipfspid, isdraft = "1" };
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
		public IActionResult CreateEdit(string id, int Type, string Template, string pid)
		{

			return View();
		}

		[HttpGet]
		public IActionResult CreateMedia([FromQuery(Name = "Id")] string Id)
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
					string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
					using (var conn = new SqliteConnection(connectionString))
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
        public IActionResult GetParentInfo(string pid)
        {
            var sanitizer = new HtmlSanitizer();
            string filetype = "";
            Console.WriteLine("Exception GetParentInfo: pid: " + pid);
            try
            {
                if (!string.IsNullOrEmpty(pid))
                {

                    HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                    webRequest.Credentials = new NetworkCredential(_userName, _password);
                    webRequest.ContentType = "application/json-rpc";
                    webRequest.Method = "POST";
                    webRequest.Timeout = 5000;
                    string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [\"get\", \"" + pid + "\"] }";
                    byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                    webRequest.ContentLength = byteArray.Length;
                    Stream dataStream = webRequest.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();

                    WebResponse webResponse = webRequest.GetResponse();
                    StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                    string getResp = sr.ReadToEnd();
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
                    var ParentName = sanitizer.Sanitize(proposal1.summary.name.ToString());
                    var ParentSummary = sanitizer.Sanitize(proposal1.summary.description.ToString());

                    var IpfsPid = proposal1.ipfscid.ToString();

                    Console.WriteLine("Exception GetUploadedMedia: filePath: File not found #2");
                    return Json(new { Success = "true", parentname = ParentName, parentsummary = ParentSummary, ipfs= IpfsPid });
                }
                else
                {
                    // Handle no file uploaded
                    Console.WriteLine("Exception GetUploadedMedia: filePath: File not found #2");
                    return Json(new { Success = "false" });
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during the upload process
                Console.WriteLine("Exception GetUploadedMedia: filePath: File exception.");
                return Json(new { Success = "false" });
            }
        }

        [HttpGet]
		public IActionResult GetUploadedMedia(string filename)
		{
			string filetype = "";
			Console.WriteLine("Exception GetUploadedMedia: filename: " + filename);
			try
			{
				if (!string.IsNullOrEmpty(filename))
				{

					string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
					using (var conn = new SqliteConnection(connectionString))
					{


						conn.Open();
						using (var cmd = conn.CreateCommand())
						{
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT filetype FROM mediafiles WHERE filename = @FileName";
							cmd.Parameters.AddWithValue("@FileName", filename);
							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								if (rdr.Read())
								{
									filetype = !rdr.IsDBNull(rdr.GetOrdinal("filetype")) ? rdr.GetString(rdr.GetOrdinal("filetype")) : "";
								}
							}
						}
					}
					string filePath = Path.Combine(ApplicationSettings.MediaPath, filename); 
					Console.WriteLine("Exception GetUploadedMedia: filePath: " + filePath);
					// Check if the file exists
					if (System.IO.File.Exists(filePath))
					{
						// Read the file content as bytes
						byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
						long size1 = fileBytes.Length;
						// Convert the bytes to base64
						string base64String = Convert.ToBase64String(fileBytes);
						long size2 = fileBytes.Length;
						// Return base64 in JSON response
						return Json(new { filetype = filetype, base64 = base64String });
					}
					else
					{
						// Handle file not found
						Console.WriteLine("Exception GetUploadedMedia: filePath: File not found.");
						return Json(new { Success = false, Message = "File not found." });
					}

				}
				else
				{
					// Handle no file uploaded
					Console.WriteLine("Exception GetUploadedMedia: filePath: File not found #2");
					return Json(new { Success = "false" });
				}
			}
			catch (Exception ex)
			{
				// Handle any exceptions that may occur during the upload process
				Console.WriteLine("Exception GetUploadedMedia: filePath: File exception.");
				return Json(new { Success = "false" });
			}
		}

		[HttpPost]
		public IActionResult UploadMedia(IFormCollection formCollection)
		{
			try
			{
				IFormFile file = formCollection.Files.First();
				// Check if the request contains file data
				if (file.Length > 0 && file.Length <= 10485760) // 10MB limit
				{

					// Generate a GUID for the new filename
					string guid = Guid.NewGuid().ToString();
					string originalFileName = Path.GetFileName(file.FileName);
					string extension = Path.GetExtension(originalFileName);

					string fileName = $"{guid}{extension}";
					Console.WriteLine("Exception UploadMedia: filename: " + fileName);

					// Set the file path where you want to store the file
					string filePath = Path.Combine(ApplicationSettings.MediaPath, fileName);
					string filePathDB = Path.Combine(ApplicationSettings.MediaPath, fileName);

					using (var stream = new FileStream(filePath, FileMode.Create))
					{
						file.CopyTo(stream);
					}


					string filetype = DetermineFileType(filePath, fileName);
					if (filetype != "unknown")
					{
						// Save the file to the server
						Console.WriteLine("Exception UploadMedia: filePath: " + filePath);
						UpdateMediaDatabase(fileName, filePath, file.Length.ToString(), filetype);
						return Json(new { Success = "true", FilePath = ApplicationSettings.MediaPath + filePathDB, filename = fileName });

					}
					else
					{
						Console.WriteLine("Exception UploadMedia: Delete: " + filePath);
						System.IO.File.Delete(filePath);
						return Json(new { Success = "false" });
					}

				}
				else
				{
					// Handle no file uploaded
					Console.WriteLine("Exception UploadMedia:File size exceeds the limit");
					return Json(new { Message = "File size exceeds the limit (10MB)." });
				}
			}
			catch (Exception ex)
			{
				// Handle any exceptions that may occur during the upload process
				Console.WriteLine("Exception UploadMedia:EX:" + ex.Message);
				return Json(new { Message = "Error uploading file. " + ex.Message });
			}
		}

		private string CalculateFileHash(Stream stream)
		{
			using (var sha256 = SHA256.Create())
			{
				byte[] hashBytes = sha256.ComputeHash(stream);
				return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
			}
		}
		private void UpdateMediaDatabase(string fileName, string filePath, string filesize, string filetype)
		{
			string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
			using (var conn = new SqliteConnection(connectionString))
			{
				conn.Open();
				using (var cmd = conn.CreateCommand())
				{
					cmd.CommandType = System.Data.CommandType.Text;
					cmd.CommandText = "INSERT INTO mediafiles (filename, filepath, fileSize, filetype, dateadded) VALUES (@FileName, @FilePath, @FileSize, @FileType, datetime('now'))";
					cmd.Parameters.AddWithValue("@FileName", fileName);
					cmd.Parameters.AddWithValue("@FilePath", filePath);
					cmd.Parameters.AddWithValue("@FileSize", filesize);
					cmd.Parameters.AddWithValue("@FileType", filetype);
					cmd.ExecuteNonQuery();
				}
			}
		}

		string DetermineFileType(string filePath, string filename)
		{
			using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
				byte[] buffer = new byte[512]; // Adjust the buffer size as needed
				int bytesRead = fileStream.Read(buffer, 0, buffer.Length);

				// Determine file type
				return IdentifyFileType(buffer, bytesRead, filename);
			}
		}

		string IdentifyFileType(byte[] buffer, int bytesRead, string filename)
		{
			string type = ""; 
			//Check via file extension first
			if (GetPDfFromExtension(filename))
			{
				type = "pdf";
			}

			if (GetVideoTypeFromExtension(filename))
			{
				type = "video";
			}

			if (GetImageTypeFromExtension(filename))
			{
				type = "image";
			}

			if (GetAudioTypeFromExtension(filename))
			{
				type = "audio";
			}

			switch (type)
			{
				case "pdf":
					if (IsPdf(buffer, bytesRead))
					{
						return "pdf";
					}
					break;
				case "video":
					if (IsVideo(buffer, bytesRead))
					{
						return "video"; 
					} 
					break;
				case "audio":
					if (IsAudio(buffer, bytesRead))
					{
						return "audio";
					}
					break;
				case "image":
					if (IsImage(buffer, bytesRead))
					{
						return "image"; 
					}
					break;
				default:
					return type;
					break;
			}
			return type; // This is a catch all to attempt to return media, in case the CheckMagicBytes function fails.
		}


		// Get video type from extension
		private bool GetVideoTypeFromExtension(string filename)
		{
			string extension = Path.GetExtension(filename).ToLower();
			switch (extension)
			{
				case ".mp4": return true;
				case ".avi": return true;
				default: return false;
			}
		}

		private bool GetPDfFromExtension(string filename)
		{
			string extension = Path.GetExtension(filename).ToLower();
			switch (extension)
			{
				case ".pdf": return true;
				default: return false;
			}
		}

		// Get image type from extension
		private bool GetImageTypeFromExtension(string filename)
		{
			string extension = Path.GetExtension(filename).ToLower();
			switch (extension)
			{
				case ".jpg": return true;
				case ".jpeg": return true;
				case ".png": return true;
				// Add more cases as needed
				default: return false;
			}
		}

		// Get audio type from extension
		private bool GetAudioTypeFromExtension(string filename)
		{
			string extension = Path.GetExtension(filename).ToLower();
			switch (extension)
			{
				case ".mp3": return true;
				case ".wav": return true;
				// Add more cases as needed
				default: return false;
			}
		}


		bool IsPdf(byte[] buffer, int bytesRead)
		{
			// PDF files typically start with '%PDF'
			return bytesRead >= 4 &&
				   buffer[0] == 0x25 && // %
				   buffer[1] == 0x50 && // P
				   buffer[2] == 0x44 && // D
				   buffer[3] == 0x46;   // F
		}

		bool IsVideo(byte[] buffer, int bytesRead)
		{
			// Check for common video file types
			// MP4: 00 00 00 18 66 74 79 70 33 67 00
			// MP4: 00 00 00 18 66 74 79 70 69 73 6F 6D 00
			// MP4: 66 74 79 70 69 73 6F 6D
			// WebM: 1A 45 DF A3
			// AVI: 52 49 46 46
			// Additional magic numbers for MP4
			// MP4 (another variation): 00 00 00 20 66 74 79 70 4D 53 4E 56 01
			// MP4 (another variation): 00 00 00 20 66 74 79 70 4D 53 4E 56 20 00 00 00 00 00 00 00 01 69 73 6F 6D

			return CheckMagicNumber(buffer, bytesRead, "00 00 00 18 66 74 79 70 33 67 00") ||
				   CheckMagicNumber(buffer, bytesRead, "00 00 00 18 66 74 79 70 69 73 6F 6D 00") ||
				   CheckMagicNumber(buffer, bytesRead, "00 00 00 0D 66 74 79 70 69 73 6F 6D") ||
				   CheckMagicNumber(buffer, bytesRead, "66 74 79 70 69 73 6F 6D") ||
				   CheckMagicNumber(buffer, bytesRead, "1A 45 DF A3") ||
				   CheckMagicNumber(buffer, bytesRead, "52 49 46 46") ||
				   CheckMagicNumber(buffer, bytesRead, "00 00 00 20 66 74 79 70 4D 53 4E 56 01") ||
				   CheckMagicNumber(buffer, bytesRead, "00 00 00 20 66 74 79 70 4D 53 4E 56 20 00 00 00 00 00 00 00 01 69 73 6F 6D");

		}

		bool IsImage(byte[] buffer, int bytesRead)
		{
			// Check for common image file types
			// JPEG: FF D8 FF E0
			// PNG: 89 50 4E 47 0D 0A 1A 0A
			// GIF: 47 49 46 38
			// You may need to extend this list based on your requirements
			return CheckMagicNumber(buffer, bytesRead, "FF D8 FF E0") ||
				   CheckMagicNumber(buffer, bytesRead, "89 50 4E 47 0D 0A 1A 0A") ||
				   CheckMagicNumber(buffer, bytesRead, "47 49 46 38");
		}

		bool IsAudio(byte[] buffer, int bytesRead)
		{
			// Check for common audio file types
			// MP3: 49 44 33
			// WAV: 52 49 46 46
			// OGG: 4F 67 67 53
			// You may need to extend this list based on your requirements
			return CheckMagicNumber(buffer, bytesRead, "49 44 33") ||
				   CheckMagicNumber(buffer, bytesRead, "52 49 46 46") ||
				   CheckMagicNumber(buffer, bytesRead, "4F 67 67 53");
		}

		bool CheckMagicNumber(byte[] buffer, int bytesRead, string magicNumber)
		{
			// Convert the string representation of the magic number to byte array
			string[] bytes = magicNumber.Split(' ');
			byte[] magicBytes = Array.ConvertAll(bytes, s => Convert.ToByte(s, 16));
			Console.WriteLine("Exception CheckMagicNumber: bytes" + bytes);

			// Compare the magic number in the buffer
			if (bytesRead >= magicBytes.Length)
			{
				for (int i = 0; i < magicBytes.Length; i++)
				{
					if (buffer[i] != magicBytes[i])
					{
						return false;
					}
				}
				return true;
			}

			return false;
		}

		[HttpGet]
		public async Task<IActionResult> CreateBuilderLoadEdit(string id, string Type, string Template, string pid, string ipfspid, string cidtype, string isDraft)
		{
            var sanitizer = new HtmlSanitizer();
            List<object> items = new List<object>();
            try
			{

				if (pid != "0" && isDraft != "1")
				{


                    int superblock = 0;
                    int isArchive = 0;
                    HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
                    webRequest.Credentials = new NetworkCredential(_userName, _password);
                    webRequest.ContentType = "application/json-rpc";
                    webRequest.Method = "POST";
                    webRequest.Timeout = 5000;
                    string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [\"get\", \"" + pid + "\"] }";

                    // serialize json for the request
                    byte[] byteArray = Encoding.UTF8.GetBytes(jsonstring);
                    webRequest.ContentLength = byteArray.Length;
                    Stream dataStream = webRequest.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();

                    WebResponse webResponse = webRequest.GetResponse();
                    StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                    string getResp = sr.ReadToEnd();
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

                    if (cidtype != "0")
					{
						var Name = "";
						var Summary = "";
                        var ParentName = sanitizer.Sanitize(proposal1.summary.name.ToString());
                        var ParentSummary = sanitizer.Sanitize(proposal1.summary.description.ToString());
                        var html = "";
						var IpfsPid = proposal1.ipfscid.ToString();
						var PaymentAddress = "";
						var PaymentAmount = "";
						var IsDraft = "0";
						Type = proposal1.type.ToString();
						var item = new { pid = pid, Name = sanitizer.Sanitize(Name), Summary = sanitizer.Sanitize(Summary), html = html, PaymentAddress = PaymentAddress, PaymentAmount = PaymentAmount, IsDraft = IsDraft, Type = Type, IpfsPid = IpfsPid, CidType = cidtype, parentname = ParentName, parentsummary = ParentSummary };
						items.Add(item);
					}
					else
					{

						var Name = sanitizer.Sanitize(proposal1.summary.name.ToString());
						var Summary = sanitizer.Sanitize(proposal1.summary.description.ToString());
                        var ParentName = sanitizer.Sanitize(proposal1.summary.name.ToString());
                        var ParentSummary = sanitizer.Sanitize(proposal1.summary.description.ToString());
                        System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
						//client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36");
						var url = "https://" + _ipfsUrl + "/ipfs/" + proposal1.ipfscid.ToString() + "/index.html";
						var html = await client.GetStringAsync(url);

						var IpfsPid = proposal1.ipfscid.ToString();
						var PaymentAddress = "";
						var PaymentAmount = "";
						var IsDraft = "0";
						Type = proposal1.type.ToString();
						var item = new { pid = pid, Name = sanitizer.Sanitize(Name), Summary = sanitizer.Sanitize(Summary), html = html, PaymentAddress = PaymentAddress, PaymentAmount = PaymentAmount, IsDraft = IsDraft, Type = Type, IpfsPid = IpfsPid, CidType = cidtype, parentname = ParentName, parentsummary = ParentSummary };
						items.Add(item);

					}
				}
				else
				{
					string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT * FROM items WHERE id = @id";
							cmd.Parameters.AddWithValue("id", id);
							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{

									var Name = !rdr.IsDBNull(rdr.GetOrdinal("Name")) ? rdr.GetString(rdr.GetOrdinal("Name")) : "";
									var Summary = !rdr.IsDBNull(rdr.GetOrdinal("Summary")) ? rdr.GetString(rdr.GetOrdinal("Summary")) : "";
									var html = !rdr.IsDBNull(rdr.GetOrdinal("html")) ? rdr.GetString(rdr.GetOrdinal("html")) : "";
									//var IpfsPid = proposal1.ipfscid.ToString();
									var css = !rdr.IsDBNull(rdr.GetOrdinal("css")) ? rdr.GetString(rdr.GetOrdinal("css")) : "";
									var PaymentAddress = "";
									var PaymentAmount = "";
									var IsDraft = "0";
									//Type = proposal1.type.ToString();
									var item = new { pid = pid, Name = sanitizer.Sanitize(Name), Summary = sanitizer.Sanitize(Summary), html = html, css = css, PaymentAddress = PaymentAddress, PaymentAmount = PaymentAmount, IsDraft = IsDraft, Type = Type, CidType = cidtype };
									items.Add(item);

								}
							}
						}
					}



				}
				return Json(items);

			}
			catch (Exception ex)
			{

				var Name = "";
				var Summary = "";
				var html = "Could not load record from IPFS. Check IPFS Gateway in Settings and verify the connection.";
				var IpfsPid = "";
				var css = "";
				var PaymentAddress = "";
				var PaymentAmount = "";
				var IsDraft = "0";
				Type = "0";
				var item = new { pid = pid, Name = sanitizer.Sanitize(Name), Summary = sanitizer.Sanitize(Summary), html = html, PaymentAddress = PaymentAddress, PaymentAmount = PaymentAmount, IsDraft = IsDraft, Type = Type, IpfsPid = IpfsPid, CidType = cidtype };
				items.Add(item);
				return Json(items);

			}


			return Json(new { Success = false, Error = "Can not load" });
		}



        [HttpGet]
        public async Task<IActionResult> CreateBuilderLoadEditDraft(string id, string Type, string Template, string pid, string ipfspid, string cidtype, string isDraft)
        {
            List<object> items = new List<object>();
            var sanitizer = new HtmlSanitizer();
            int superblock = 0;
            int isArchive = 0;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
            webRequest.Credentials = new NetworkCredential(_userName, _password);
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";
            webRequest.Timeout = 5000;
            string jsonstring = "{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"gobject\", \"params\": [\"get\", \"" + pid + "\"] }";

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

                if (!string.IsNullOrEmpty(pid) && isDraft != "1")
                {
                    if (cidtype != "0")
                    {
                        var Name = "";
                        var Summary = "";
                        var html = "";
                        var IpfsPid = proposal1.ipfscid.ToString();
                        var PaymentAddress = "";
                        var PaymentAmount = "";
                        var IsDraft = "0";
                        Type = proposal1.type.ToString();
                        var item = new { pid = pid, Name = sanitizer.Sanitize(Name), Summary = sanitizer.Sanitize(Summary), html = html, PaymentAddress = PaymentAddress, PaymentAmount = PaymentAmount, IsDraft = IsDraft, Type = Type, IpfsPid = IpfsPid, CidType = cidtype };
                        items.Add(item);
                    }
                    else
                    {

                        var Name = sanitizer.Sanitize(proposal1.summary.name.ToString());
                        var Summary = sanitizer.Sanitize(proposal1.summary.description.ToString());
                        System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                        //client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36");
                        var url = "https://" + _ipfsUrl + "/ipfs/" + proposal1.ipfscid.ToString() + "/index.html";
                        var html = await client.GetStringAsync(url);

                        var IpfsPid = proposal1.ipfscid.ToString();
                        var PaymentAddress = "";
                        var PaymentAmount = "";
                        var IsDraft = "0";
                        Type = proposal1.type.ToString();
                        var item = new { pid = pid, Name = sanitizer.Sanitize(Name), Summary = sanitizer.Sanitize(Summary), html = html, PaymentAddress = PaymentAddress, PaymentAmount = PaymentAmount, IsDraft = IsDraft, Type = Type, IpfsPid = IpfsPid, CidType = cidtype };
                        items.Add(item);

                    }
                }
                else
                {
                    string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
                    using (var conn = new SqliteConnection(connectionString))
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            conn.Open();
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.CommandText = "SELECT * FROM items WHERE id = @id";
                            cmd.Parameters.AddWithValue("id", id);
                            using (SqliteDataReader rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {

                                    var Name = !rdr.IsDBNull(rdr.GetOrdinal("Name")) ? rdr.GetString(rdr.GetOrdinal("Name")) : "";
                                    var Summary = !rdr.IsDBNull(rdr.GetOrdinal("Summary")) ? rdr.GetString(rdr.GetOrdinal("Summary")) : "";
                                    var html = !rdr.IsDBNull(rdr.GetOrdinal("html")) ? rdr.GetString(rdr.GetOrdinal("html")) : "";
                                    var IpfsPid = proposal1.ipfscid.ToString();
                                    var css = !rdr.IsDBNull(rdr.GetOrdinal("css")) ? rdr.GetString(rdr.GetOrdinal("css")) : "";
                                    var PaymentAddress = "";
                                    var PaymentAmount = "";
                                    var IsDraft = "0";
                                    Type = proposal1.type.ToString();
                                    var item = new { pid = pid, Name = sanitizer.Sanitize(Name), Summary = sanitizer.Sanitize(Summary), html = html, PaymentAddress = PaymentAddress, PaymentAmount = PaymentAmount, IsDraft = IsDraft, Type = Type, IpfsPid = IpfsPid, CidType = cidtype };
                                    items.Add(item);

                                }
                            }
                        }
                    }



                }
                return Json(items);

            }
            catch (Exception ex)
            {

                var Name = "";
                var Summary = "";
                var html = "Could not load record from IPFS. Check IPFS Gateway in Settings and verify the connection.";
                var IpfsPid = "";
                var css = "";
                var PaymentAddress = "";
                var PaymentAmount = "";
                var IsDraft = "0";
                Type = "0";
                var item = new { pid = pid, Name = sanitizer.Sanitize(Name), Summary = sanitizer.Sanitize(Summary), html = html, PaymentAddress = PaymentAddress, PaymentAmount = PaymentAmount, IsDraft = IsDraft, Type = Type, IpfsPid = IpfsPid, CidType = cidtype };
                items.Add(item);
                return Json(items);

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
					string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
					using (var conn = new SqliteConnection(connectionString))
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
		public async Task<IActionResult> CreateBuilderLoadArchiveURL(int id)
		{
			List<object> items = new List<object>();
			var sanitizer = new HtmlSanitizer();

			try
			{
				string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
				using (var conn = new SqliteConnection(connectionString))
				{
					using (var cmd = conn.CreateCommand())
					{
						//Need to do this because of issues with the extension.
						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "DELETE FROM archives";
						//cmd.ExecuteNonQuery();

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
		public async Task<IActionResult> PollForArchive(int id)
		{
			List<object> items = new List<object>();
			var sanitizer = new HtmlSanitizer();

			try
			{
				while (items.Count == 0)
				{
					string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT * FROM archives WHERE  id = @id LIMIT 1";
							cmd.Parameters.AddWithValue("@id", id);
							cmd.ExecuteNonQuery();

							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								while (rdr.Read())
								{
									var url = rdr.GetString(rdr.GetOrdinal("url"));
									var urlInsert = HttpUtility.HtmlEncode(rdr.GetString(rdr.GetOrdinal("url")));
									var html = !rdr.IsDBNull(rdr.GetOrdinal("html")) ? rdr.GetString(rdr.GetOrdinal("html")) : "";
									var image = !rdr.IsDBNull(rdr.GetOrdinal("image")) ? rdr.GetString(rdr.GetOrdinal("image")) : "";
									string orgUrl = "";
									string pattern = @"url:\s*(\S+)";
									Match match = Regex.Match(html, pattern);
									if (match.Success)
									{
										orgUrl = match.Groups[1].Value;
										Console.WriteLine(url);
									}
									else
									{
										Console.WriteLine("URL not found.");
									}

									var logo = @"<div id=""logo"" class=""container"" style=""margin-top: 20px;""><div class=""row""><div class=""col-md-6""><a href=""https://historia.network""> <img src=""data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHhtbG5zOnhsaW5rPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5L3hsaW5rIiB2aWV3Qm94PSIwIDAgMTEzOS40MSAyMDEuMTIiPjxkZWZzPjxzdHlsZT4uY2xzLTF7ZmlsbDp1cmwoI2xpbmVhci1ncmFkaWVudCk7fS5jbHMtMntmaWxsOnVybCgjbGluZWFyLWdyYWRpZW50LTIpO30uY2xzLTN7ZmlsbDp1cmwoI2xpbmVhci1ncmFkaWVudC0zKTt9LmNscy00e2ZpbGw6dXJsKCNsaW5lYXItZ3JhZGllbnQtNCk7fS5jbHMtNXtmaWxsOnVybCgjbGluZWFyLWdyYWRpZW50LTYpO308L3N0eWxlPjxsaW5lYXJHcmFkaWVudCBpZD0ibGluZWFyLWdyYWRpZW50IiB4MT0iNjAuNjkiIHkxPSIxMDAuMTEiIHgyPSIxNTguNDMiIHkyPSIxMDAuMTEiIGdyYWRpZW50VW5pdHM9InVzZXJTcGFjZU9uVXNlIj48c3RvcCBvZmZzZXQ9IjAiIHN0b3AtY29sb3I9IiMxMjRlNjMiLz48c3RvcCBvZmZzZXQ9IjAuNjgiIHN0b3AtY29sb3I9IiMxMjUxNjYiLz48c3RvcCBvZmZzZXQ9IjEiIHN0b3AtY29sb3I9IiMxMTU2NmIiLz48L2xpbmVhckdyYWRpZW50PjxsaW5lYXJHcmFkaWVudCBpZD0ibGluZWFyLWdyYWRpZW50LTIiIHgxPSIxMS41OCIgeTE9IjEwNy43NiIgeDI9Ijg1LjAxIiB5Mj0iMTA3Ljc2IiBncmFkaWVudFVuaXRzPSJ1c2VyU3BhY2VPblVzZSI+PHN0b3Agb2Zmc2V0PSIwIiBzdG9wLWNvbG9yPSIjNWZiZmNkIi8+PHN0b3Agb2Zmc2V0PSIwLjEzIiBzdG9wLWNvbG9yPSIjNGZiOWM3Ii8+PHN0b3Agb2Zmc2V0PSIwLjUyIiBzdG9wLWNvbG9yPSIjMjRhOGI2Ii8+PHN0b3Agb2Zmc2V0PSIwLjgyIiBzdG9wLWNvbG9yPSIjMGE5ZGFjIi8+PHN0b3Agb2Zmc2V0PSIxIiBzdG9wLWNvbG9yPSIjMDA5OWE4Ii8+PC9saW5lYXJHcmFkaWVudD48bGluZWFyR3JhZGllbnQgaWQ9ImxpbmVhci1ncmFkaWVudC0zIiB4MT0iMTUuNTkiIHkxPSIxNjguOTUiIHgyPSIxNTQuNDEiIHkyPSIzMC4xMyIgeGxpbms6aHJlZj0iI2xpbmVhci1ncmFkaWVudC0yIi8+PGxpbmVhckdyYWRpZW50IGlkPSJsaW5lYXItZ3JhZGllbnQtNCIgeDE9IjEzNi4xNyIgeTE9Ijk4Ljc3IiB4Mj0iMTE0NS4yIiB5Mj0iOTguNzciIGdyYWRpZW50VW5pdHM9InVzZXJTcGFjZU9uVXNlIj48c3RvcCBvZmZzZXQ9IjAiIHN0b3AtY29sb3I9IiM0OWI3YzYiLz48c3RvcCBvZmZzZXQ9IjAuMjYiIHN0b3AtY29sb3I9IiM0N2I0YzMiLz48c3RvcCBvZmZzZXQ9IjAuNDUiIHN0b3AtY29sb3I9IiM0MmFhYmEiLz48c3RvcCBvZmZzZXQ9IjAuNjIiIHN0b3AtY29sb3I9IiMzODlhYWIiLz48c3RvcCBvZmZzZXQ9IjAuNzgiIHN0b3AtY29sb3I9IiMyYjgzOTUiLz48c3RvcCBvZmZzZXQ9IjAuOTMiIHN0b3AtY29sb3I9IiMxYTY2N2EiLz48c3RvcCBvZmZzZXQ9IjEiIHN0b3AtY29sb3I9IiMxMTU2NmIiLz48L2xpbmVhckdyYWRpZW50PjxsaW5lYXJHcmFkaWVudCBpZD0ibGluZWFyLWdyYWRpZW50LTYiIHgxPSIxMzYuMTciIHkxPSI5OC43NyIgeDI9IjExNDUuMiIgeTI9Ijk4Ljc3IiB4bGluazpocmVmPSIjbGluZWFyLWdyYWRpZW50LTQiLz48L2RlZnM+PHRpdGxlPmxvZ29ibHVlPC90aXRsZT48ZyBpZD0iTGF5ZXJfMiIgZGF0YS1uYW1lPSJMYXllciAyIj48ZyBpZD0iTGF5ZXJfMS0yIiBkYXRhLW5hbWU9IkxheWVyIDEiPjxwYXRoIGNsYXNzPSJjbHMtMSIgZD0iTTE0Ny43MSw1MS4yMSw5OS40NywyMy43NGEyOS41MSwyOS41MSwwLDAsMC0yOS0uMTEsMTkuMTcsMTkuMTcsMCwwLDAtOS43OSwxNi43MXY4LjgyQzYwLjY5LDI4LjcsODUsNDMsODUsNDNsNDMuMzMsMjQuNjhhMTEuNDYsMTEuNDYsMCwwLDEsNS43OSwxMHYyMy41N2wtMjQuOCwxMy41M1Y4Ni40OUw4NSwxMDAuMTF2NTcuMTVjMCwyNi41LTE0LjQ2LDE5LjIyLTE0LjQ2LDE5LjIyYTI5LjUxLDI5LjUxLDAsMCwwLDI5LC4xMSwxOS4xNywxOS4xNywwLDAsMCw5Ljc5LTE2LjcxVjE0Mi40MWwyNC44LTEzLjUydjI2LjY2bDI0LjMxLTEzLjYyVjY5LjY2QTIxLjIzLDIxLjIzLDAsMCwwLDE0Ny43MSw1MS4yMVoiLz48cGF0aCBjbGFzcz0iY2xzLTIiIGQ9Ik02MC42OSw0OS4xNnY4LjY4TDM1Ljg5LDcxLjMzVjQ0LjY3TDExLjU4LDU4LjI5djcyLjI3QTIxLjIzLDIxLjIzLDAsMCwwLDIyLjMsMTQ5bDQ4LjI0LDI3LjQ3Uzg1LDE4My43Niw4NSwxNTcuMjZMNDEuNjgsMTMyLjU4YTExLjQ2LDExLjQ2LDAsMCwxLTUuNzktMTBWOTkuMDVsMjQuOC0xMy41M3YyOC4yMUw4NSwxMDAuMTFWNDNTNjAuNjksMjguNyw2MC42OSw0OS4xNloiLz48cGF0aCBjbGFzcz0iY2xzLTMiIGQ9Ik04NS4xNiwyMDEuMTIsMCwxNTMuMzVWNDUuODhMODUuMTYsMCwxNzAsNDUuNzNWMTUzLjE5Wk00LjY5LDE1MC42bDgwLjQ2LDQ1LjE0LDgwLjE3LTQ1LjI5VjQ4LjUzTDg1LjE2LDUuMzMsNC42OSw0OC42OFoiLz48cGF0aCBjbGFzcz0iY2xzLTQiIGQ9Ik0zMTMsNDkuMzV2OTguODRIMjg3LjY4VjExMS40NGgtNDAuM3YzNi43NUgyMjIuMTdWNDkuMzVoMjUuMjFWODcuNzRoNDAuM1Y0OS4zNVoiLz48cGF0aCBjbGFzcz0iY2xzLTQiIGQ9Ik0zODkuMzUsNDkuMzV2OTguODRIMzY0VjQ5LjM1WiIvPjxwYXRoIGNsYXNzPSJjbHMtNSIgZD0iTTQ2MS41MywxMTguNTR2LjYzYTUuNTgsNS41OCwwLDAsMCwxLDMsMTAsMTAsMCwwLDAsMywyLjkxLDE4LjQzLDE4LjQzLDAsMCwwLDUuMiwyLjIyLDI3LjkxLDI3LjkxLDAsMCwwLDcuNDcuODlxNy4xLDAsOS44Mi0ydDIuNzMtNC4xMnEwLTMuMTctMy45My01LjQ0QTc3LjQ3LDc3LjQ3LDAsMCwwLDQ3NCwxMTEuMzFxLTYtMS44OS0xMi4zNS00LjVhNTYuOTEsNTYuOTEsMCwwLDEtMTEuNzItNi4zOSwzMy40MywzMy40MywwLDAsMS04LjgxLTkuMjUsMjMuNzQsMjMuNzQsMCwwLDEtMy40OC0xMywyNy44NiwyNy44NiwwLDAsMSwyLjg1LTEyLjc0LDI4LjIsMjguMiwwLDAsMSw4LTkuNjMsMzcuNDksMzcuNDksMCwwLDEsMTIuMjMtNi4xNSw1My42NCw1My42NCwwLDAsMSwxNS41OC0yLjE1LDU1LjYxLDU1LjYxLDAsMCwxLDE2LDIuMjIsMzguNzQsMzguNzQsMCwwLDEsMTIuOCw2LjQsMzAuNzQsMzAuNzQsMCwwLDEsOC40OSwxMC4wNywyOCwyOCwwLDAsMSwzLjEsMTMuMjR2MS40SDQ5MnYtLjg5YTguMjksOC4yOSwwLDAsMC0uOTUtMy42OCwxMC41LDEwLjUsMCwwLDAtMi44Ni0zLjQyLDE1LjM5LDE1LjM5LDAsMCwwLTQuNzUtMi40NywyMS4xNiwyMS4xNiwwLDAsMC02LjY1LS45NXEtNi40NywwLTkuMjUsMi4wOXQtMi43OSw0LjYzcTAsMi45MSwzLjc0LDQuOTRhOTEuNjUsOTEuNjUsMCwwLDAsMTIuNjEsNS4wN3E1LjA3LDEuNzcsMTEuNTMsNC4zN2E1OC44MSw1OC44MSwwLDAsMSwxMi4xLDYuNTksMzcuMTcsMzcuMTcsMCwwLDEsOS41LDkuNzUsMjQuMzYsMjQuMzYsMCwwLDEsMy44NywxMy44OCwyNS42MywyNS42MywwLDAsMS0xMS4yMiwyMS45MiwzOC44OSwzOC44OSwwLDAsMS0xMi40MSw1LjU4LDYxLjI0LDYxLjI0LDAsMCwxLTE1LjY1LDEuOSw2NC41Niw2NC41NiwwLDAsMS0xNi41NC0yLDQyLjM1LDQyLjM1LDAsMCwxLTEzLjMtNS44OSwyOC42MSwyOC42MSwwLDAsMS04Ljg3LTkuNTcsMjUuNjgsMjUuNjgsMCwwLDEtMy4yMy0xMi45MnYtMS4xNFoiLz48cGF0aCBjbGFzcz0iY2xzLTQiIGQ9Ik01NTguMjksNDkuMzVoODYuOTNWNzMuOTNoLTMwLjh2NzQuMjZINTg5LjA4VjczLjkzSDU1OC4yOVoiLz48cGF0aCBjbGFzcz0iY2xzLTUiIGQ9Ik02ODMsOTguNzdhNDguMTMsNDguMTMsMCwwLDEsNC4xOC0yMCw1Mi42MSw1Mi42MSwwLDAsMSwxMS4zNC0xNi4yOCw1My42OCw1My42OCwwLDAsMSwxNi43My0xMSw1Mi42Myw1Mi42MywwLDAsMSw0MC41NCwwLDUzLjY4LDUzLjY4LDAsMCwxLDE2LjczLDExLDUxLjc4LDUxLjc4LDAsMCwxLDExLjI4LDE2LjI4LDUwLjQxLDUwLjQxLDAsMCwxLDAsMzkuOTJBNTEuNzgsNTEuNzgsMCwwLDEsNzcyLjUxLDEzNWE1My42OCw1My42OCwwLDAsMS0xNi43MywxMSw1Mi42Myw1Mi42MywwLDAsMS00MC41NCwwLDUzLjY4LDUzLjY4LDAsMCwxLTE2LjczLTExLDUyLjYxLDUyLjYxLDAsMCwxLTExLjM0LTE2LjI4QTQ4LjEzLDQ4LjEzLDAsMCwxLDY4Myw5OC43N1ptMjUuNTksMGEyNi44LDI2LjgsMCwwLDAsMi4wOSwxMC41MiwyNi4yOCwyNi4yOCwwLDAsMCw1Ljc3LDguNTUsMjcuNDcsMjcuNDcsMCwwLDAsOC41NSw1LjcsMjYuMTYsMjYuMTYsMCwwLDAsMTAuNDYsMi4wOSwyNi42OSwyNi42OSwwLDAsMCwxOS4wNy03Ljc5QTI2Ljg5LDI2Ljg5LDAsMCwwLDc0Niw3NGEyNi41LDI2LjUsMCwwLDAtMTAuNTgtMi4wOUEyNi4xNiwyNi4xNiwwLDAsMCw3MjUsNzRhMjcuNDcsMjcuNDcsMCwwLDAtOC41NSw1LjcsMjYuMjgsMjYuMjgsMCwwLDAtNS43Nyw4LjU1QTI2LjgsMjYuOCwwLDAsMCw3MDguNTgsOTguNzdaIi8+PHBhdGggY2xhc3M9ImNscy00IiBkPSJNODgyLjMyLDQ5LjM1YTQzLjQsNDMuNCwwLDAsMSwxNi40NywyLjg1QTMwLjksMzAuOSwwLDAsMSw5MTcuMSw3MS41M2E0My4zNCw0My4zNCwwLDAsMSwyLjIyLDEzLjkzLDQyLjI2LDQyLjI2LDAsMCwxLTEuMjEsMTAuMTQsMzUuMjcsMzUuMjcsMCwwLDEtMy42MSw5LjE5LDMzLjE3LDMzLjE3LDAsMCwxLTYuMDgsNy43MywzMS43LDMxLjcsMCwwLDEtOC42Miw1Ljc2bDIxLjQyLDI5LjkxSDg5MC45M0w4NzIuMTgsMTIxLjdIODU5LjI2djI2LjQ5SDgzNFY0OS4zNVpNODU5LjI2LDcyLjQxVjk4Ljc3aDIxLjY2YTE3LjI0LDE3LjI0LDAsMCwwLDYuNDctMS4wOCwxMS42OCwxMS42OCwwLDAsMCw0LjMtMi44NSwxMC43NywxMC43NywwLDAsMCwyLjQxLTQuMTgsMTYuMTgsMTYuMTgsMCwwLDAsLjc2LTQuOTQsMTQuMzksMTQuMzksMCwwLDAtMy4xNy05LjUxcS0zLjE2LTMuNzktMTAuNzctMy44WiIvPjxwYXRoIGNsYXNzPSJjbHMtNCIgZD0iTTk5Mi4wNiw0OS4zNXY5OC44NEg5NjYuNzJWNDkuMzVaIi8+PHBhdGggY2xhc3M9ImNscy00IiBkPSJNMTA5OS41LDQ5LjM1bDM5LjkxLDk4Ljg0aC0yNy42MmwtNS40NS0xNC40NWgtMzguOUwxMDYyLDE0OC4xOWgtMjcuNjJsNDAtOTguODRaTTEwODcsODEuNzlsLTEwLjksMjloMjEuNjdaIi8+PC9nPjwvZz48L3N2Zz4="" width=""175""></a> </div></div><br></div>";

									string indexHtml = "<!DOCTYPE html><html lang=\"en\"> <head> <meta charset=\"utf-8\"/> <meta name=\"viewport\" " +
										"content=\"width=device-width, initial-scale = 1, user-scalable = yes\"/> <link rel=\"stylesheet\" href=\"https://maxcdn.bootstrapcdn.com/bootstrap/4.4.1/css/bootstrap.min.css\"/> " +
                                        "<link rel =\"stylesheet\" href=\"/ipns/12D3KooWKEdFVQe36DwxRxWX5DSq7DzS2tXvs6twCWBPC9hxHtEk/bootstrap.min.css\"/>" +
                                        "<script src=\"https://ajax.googleapis.com/ajax/libs/jquery/3.5.1/jquery.min.js\"></script>" +
                                        "<script src=\"/ipns/12D3KooWKEdFVQe36DwxRxWX5DSq7DzS2tXvs6twCWBPC9hxHtEk/jquery.min.js\"></script>" +
                                        "<script src=\"https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.16.0/umd/popper.min.js\"></script> " +
                                         "<script src=\"/ipns/12D3KooWKEdFVQe36DwxRxWX5DSq7DzS2tXvs6twCWBPC9hxHtEk/popper.min.js\"></script> " +
                                        "<script src=\"https://maxcdn.bootstrapcdn.com/bootstrap/4.4.1/js/bootstrap.min.js\"></script> " +
                                        "<script src=\"/ipns/12D3KooWKEdFVQe36DwxRxWX5DSq7DzS2tXvs6twCWBPC9hxHtEk/bootstrap.min.js\"></script>" +
                                        "</head> <body> " + logo + " <center>" +
										"This page was archived on " + DateTime.UtcNow.ToString("F") + " UTC<br> Original URL: <a href = " + urlInsert + ">" + urlInsert + " </a></center> <div class=\"container\"> <div class=\"row\" id=\"ArchivePreview\" style=\"max-width:1500px\"> " +
										"<div class=\"col-12\"> <ul id=\"tabsJustified\" class=\"nav nav-tabs\" style=\"background: #fff;\"> " +
										"<li class=\"nav-item\"><a href=\"#archiveHtml\" data-target=\"#archiveHtml\" data-toggle=\"tab\" role=\"tab\" " +
										"class=\"nav-link small text-uppercase active\" aria-selected=\"true\">HTML</a></li><li class=\"nav-item\">" +
										"<a href=\"#archiveImage\" data-target=\"#archiveImage\" data-toggle=\"tab\" role=\"tab\" class=\"nav-link small " +
										"text-uppercase\" aria-selected=\"false\">IMAGE</a></li></ul> <div class=\"tab-content\" style=\"max-width: 1318px;\">" +
										"<div id=\"archiveHtml\" class=\"tab-pane fade show active\"><div style=\"height: 100%;\"><div class=\"row mt-4\">" +
										"<div class=\"col-12 mx-auto\"><iframe id=\"archiveHtmlContainer\" height=\"1000px\" width=\"100%\" srcdoc=\"" + HttpUtility.HtmlEncode(html) + "\"></iframe>" +
										"</div></div></div></div><div id=\"archiveImage\" class=\"tab-pane fade\"><div style=\"height: 100%;\"><div class=\"row mt-4\">" +
										"<div class=\"col-12 mx-auto\"><div id=\"archiveImageContainer\" style=\"width: 100%; height: 100%; overflow-y: scroll;" +
										" overflow-x: scroll;\"><img style=\"width:100%; height:100%;\" src=\"data:image/png;base64, " + image + "\"/></div></div></div></div></div></div>" +
										"</div></div></div>" + "<script type=\"text/javascript\"> var iframe = document.getElementById(\"archiveHtmlContainer\"); iframe.onload = function() { var maxAllowedHeight = 1500; var actualHeight = iframe.contentWindow.document.body.scrollHeight + 50; if (actualHeight > maxAllowedHeight) { 	iframe.style.height = maxAllowedHeight + 'px'; } else { iframe.style.height = actualHeight + 'px'; } } </script></body></html>";

									var size = Encoding.Default.GetByteCount(indexHtml);
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

		[HttpPost]
		public async Task<JsonResult> DeleteArchiveDraft(IFormCollection formCollection)
		{
			var sanitizer = new HtmlSanitizer();
			var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
			try
			{
				string id = sanitizer.Sanitize(formCollection["id"]);

				if (id != "0")
				{
					string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "DELETE FROM archives WHERE id=@id";
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
				string IpfsPid = sanitizer.Sanitize(formCollection["IPFSPID"]);
				string cidType = sanitizer.Sanitize(formCollection["cidtype"]);
				string pid = sanitizer.Sanitize(formCollection["pid"]);
				int testId = 0;

				if (id != "0")
				{
					string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "SELECT id FROM items WHERE Id = @Id";
							cmd.Parameters.AddWithValue("Id", id);
							using (SqliteDataReader rdr = cmd.ExecuteReader())
							{
								if (rdr.Read())
								{
									testId = rdr.GetInt16(rdr.GetOrdinal("id"));

								}
								else
								{
									id = "0";
								}
							}
						}
					}
				}
				if (string.IsNullOrEmpty(cidType))
				{
					cidType = "0";
				}
				if (string.IsNullOrEmpty(pid))
				{
					pid = "0";
				}


				if (id == "0")
				{
					string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "Insert into items (Name, Summary, html, css, PaymentAddress, PaymentAmount, IsDraft, Type, ParentIPFSCID, cidtype, proposalhash, imported) " +
								"VALUES (@Name, @Summary, @html, @css, @PaymentAddress, @PaymentAmount, @IsDraft, @Type, @ParentIPFSCID, @cidtype, @proposalhash, @imported)";
							cmd.Parameters.AddWithValue("Name", Name);
							cmd.Parameters.AddWithValue("Summary", Summary);
							cmd.Parameters.AddWithValue("html", html);
							cmd.Parameters.AddWithValue("css", css);
							cmd.Parameters.AddWithValue("PaymentAddress", PaymentAddress);
							cmd.Parameters.AddWithValue("PaymentAmount", PaymentAmount);
							cmd.Parameters.AddWithValue("IsDraft", IsDraft);
							cmd.Parameters.AddWithValue("Type", Type);
							cmd.Parameters.AddWithValue("ParentIPFSCID", IpfsPid);
							cmd.Parameters.AddWithValue("cidtype", cidType);
							cmd.Parameters.AddWithValue("proposalhash", pid);
							cmd.Parameters.AddWithValue("imported", 0);
							cmd.ExecuteNonQuery();

						}
					}
				}
				else
				{

					string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
					using (var conn = new SqliteConnection(connectionString))
					{
						using (var cmd = conn.CreateCommand())
						{
							conn.Open();
							cmd.CommandType = System.Data.CommandType.Text;
							cmd.CommandText = "UPDATE items " +
								"SET Name = @Name, Summary = @Summary, html = @html, css = @css, " +
								"PaymentAddress = @PaymentAddress, PaymentAmount = @PaymentAmount, " +
								"IsDraft = @IsDraft, cidtype= @cidtype, Type = @Type, proposalhash=@proposalhash where id = @id";
							cmd.Parameters.AddWithValue("Name", Name);
							cmd.Parameters.AddWithValue("Summary", Summary);
							cmd.Parameters.AddWithValue("html", html);
							cmd.Parameters.AddWithValue("css", css);
							cmd.Parameters.AddWithValue("PaymentAddress", PaymentAddress);
							cmd.Parameters.AddWithValue("PaymentAmount", PaymentAmount);
							cmd.Parameters.AddWithValue("IsDraft", IsDraft);
							cmd.Parameters.AddWithValue("Type", Type);
							cmd.Parameters.AddWithValue("id", id);
							cmd.Parameters.AddWithValue("cidtype", cidType);
							cmd.Parameters.AddWithValue("proposalhash", pid);
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

		[HttpGet]
		public JsonResult GetArchiveDrafts()
		{
			var sanitizer = new HtmlSanitizer();
			var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
			string proposalhash = "";
			try
			{
				List<object> items = new List<object>();
				string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
				using (var conn = new SqliteConnection(connectionString))
				{
					using (var cmd = conn.CreateCommand())
					{
						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "SELECT * FROM archives";

						using (SqliteDataReader rdr = cmd.ExecuteReader())
						{
							while (rdr.Read())
							{
								var id = rdr.GetInt32(rdr.GetOrdinal("Id"));
								var url = !rdr.IsDBNull(rdr.GetOrdinal("url")) ? rdr.GetString(rdr.GetOrdinal("url")) : "";
								//var date = !rdr.IsDBNull(rdr.GetOrdinal("dateadded")) ? rdr.GetDateTime(rdr.GetOrdinal("dateadded")).ToString() : "";
								double dateAdded = double.Parse(rdr.GetString(rdr.GetOrdinal("dateadded")));
								var date = UnixTimeStampToDateTime(dateAdded).ToString("MMM dd, yyyy HH:mm:ss");
								var item = new { Id = id, draftName = sanitizer.Sanitize(url), date = date };
								items.Add(item);
							}

						}
					}
				}

				var json1 = JsonConvert.SerializeObject(items);
				return Json(json1);
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
					string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
					using (var conn = new SqliteConnection(connectionString))
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
            lockWallet(); // If wallet is unlocked before hand, this will fail, we will unlock later on.
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

				string IpfsCidTemp = ret.Id.Hash.ToString();
				if (!string.IsNullOrEmpty(IpfsCidTemp))
				{
					//return Json(new { success = true });
				}
				else
				{
					dynamic prepRespJsonFail = new { Success = false, Error = "IPFS API is not running or not accessible. Enable IPFS API on Settings Page -> Advanced Settings -> Enable IPFS API Server" };
					return Json(prepRespJsonFail);
				}

			}
			catch (Exception ex)
			{
				dynamic prepRespJsonFail = new { Success = false, Error = "IPFS API is not running or not accessible. Enable IPFS API on Settings Page -> Advanced Settings -> Enable IPFS API Server" };
				return Json(prepRespJsonFail);
			}




			var sanitizer = new HtmlSanitizer();
			string html = formCollection["html"];
			string css = sanitizer.Sanitize(formCollection["css"]);
			string id = sanitizer.Sanitize(formCollection["id"]);
			bool isArchive = false;
			if (sanitizer.Sanitize(formCollection["isArchive"]) == "1")
			{
				isArchive = true;
			}
            bool isMedia = false;
            if (sanitizer.Sanitize(formCollection["isMedia"]) == "1")
            {
                isMedia = true;
            }
            string SummaryName = sanitizer.Sanitize(formCollection["Name"]);
			string Summary = sanitizer.Sanitize(formCollection["Summary"]);
			string PaymentAddress = sanitizer.Sanitize(formCollection["PaymentAddress"]);
			string PaymentAmount = sanitizer.Sanitize(formCollection["PaymentAmount"]);
			string PaymentDate = sanitizer.Sanitize(formCollection["PaymentDate"]);
			string IsDraft = sanitizer.Sanitize(formCollection["IsDraft"]);
			string Type = sanitizer.Sanitize(formCollection["Type"]);
			string IpfsPid = sanitizer.Sanitize(formCollection["IPFSPID"]);
			string passphrase = sanitizer.Sanitize(formCollection["passphrase"]);
			string cidType = sanitizer.Sanitize(formCollection["cidType"]);
			long uniqueName = DateTimeOffset.Now.ToUnixTimeSeconds();

			if (String.IsNullOrEmpty(cidType)) { cidType = "0"; }

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
					bool validPassphrase = UnlockWallet(passphrase); // Unlock wallet here
					if (!validPassphrase)
					{
						dynamic prepRespJsonFail = new { Success = false, Error = "Invalid Passphrase" };
						lockWallet();

                        return Json(prepRespJsonFail);
					}
				}
				catch (Exception ex)
				{
					dynamic prepRespJsonFail = new { Success = false, Error = "Unable to validate unlock wallet. Is Historia Core running?" };
                    lockWallet();
                    return Json(prepRespJsonFail);

				}
			}
			string endEpoch = CalcEndEpoch(PaymentDate);
			string HtmlFinal = GenerateHTMLForIpfs(html, css, SummaryName, Summary, isArchive, isMedia);
			string IpfsCid;


            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(HtmlFinal);
            const int maxSizeInBytes = 20 * 1024 * 1024;
            if(byteArray.Length > maxSizeInBytes)
			{
                dynamic prepRespJson = new { Success = false, Error = "Object is larger than allowed: "+ byteArray.Length + " bytes." };
                lockWallet();
                return Json(prepRespJson);
            }

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
                    lockWallet();
                    return Json(prepRespJsonFail);
				}
			}
			else
			{
				dynamic prepRespJsonFail = new { Success = false, Error = "Something went wrong" };
                lockWallet();
                return Json(prepRespJsonFail);
			}

			//Do submission via gobject submit
			string proposalhash = GobjectPrepareSubmit(endEpoch, uniqueName, PaymentAddress, PaymentAmount, Summary, SummaryName, Type, IpfsCid, IpfsPid, cidType);

			if (string.IsNullOrEmpty(proposalhash) || proposalhash == "1")
			{
				if (proposalhash == "1")
				{
					dynamic prepRespJsonFail = new { Success = false, Error = "Could Not Submit Proposal. Can't reach your Historia Core Wallet" };
                    lockWallet();
                    return Json(prepRespJsonFail);
				}
				else
				{
					dynamic prepRespJsonFail = new { Success = false, Error = "Could Not Submit Proposal. Do you have available coins in your wallet?" };
                    lockWallet();
                    return Json(prepRespJsonFail);
				}
			}

			try
			{
				dynamic prepRespJson = new { Success = true, proposalhash=proposalhash, summaryname = SummaryName, summary = Summary };
                lockWallet();
                return Json(prepRespJson);
			}
			catch (Exception ex)
			{
				var prepRespJson1 = new { Success = false };
                lockWallet();
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
				string jsonstring = $"{{\"jsonrpc\": \"1.0\", \"id\":\"curltest\", \"method\": \"walletpassphrase\", \"params\": [\"{passphrase}\", 6000] }}";

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

        private void lockWallet()
        {
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

        private string GenerateHTMLForIpfs(string html, string css, string title, string description, bool isArchive, bool isMedia)
        {
            string htmlRet;

            string pattern = @"(<a\s+[^>]*href=""https?:\/\/[^\/]+)(\/[^""]*)(""[^>]*class=""(custom-ipfs-link|custom-ipfs-link-block)""[^>]*>)";

            // Regex replacement to convert to relative links
            string replacement = @"<a href=""$2""$3";

            // Perform the replacement
            html = Regex.Replace(html, pattern, replacement);

            if (isArchive)
            {
				if(isMedia) {
                    htmlRet =
					"<!DOCTYPE html><html lang=\"en-US\"><head><title>" + title + "</title><meta name=\"description\" content=\"" + description + "\" />" +
                    "<meta name=\"keywords\" content=\"Historia, History, blockchain, cryptocurrency, HTA, HTAArchive, HTAMedia\" />" +
					"<meta name=\"robots\" content=\"index, follow\" ><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />" +
					"<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" /><meta http-equiv=\"X-UA-Compatible\" " +
					"content=\"IE=edge\" ></head>" + html + "</html>";
                } else { 
					htmlRet =
					"<!DOCTYPE html><html lang=\"en-US\"><head><title>" + title + "</title><meta name=\"description\" content=\"" + description + "\" />" +
					"<meta name=\"keywords\" content=\"Historia, History, blockchain, cryptocurrency, HTA, HTAArchive\" />" +
					"<meta name=\"robots\" content=\"index, follow\" ><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />" +
					"<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" /><meta http-equiv=\"X-UA-Compatible\" " +
					"content=\"IE=edge\" ></head>" + html + "</html>";
                }
            }
            else
            {
                htmlRet =
                "<!DOCTYPE html><html lang=\"en-US\"><head><title>" + title + "</title><meta name=\"description\" content=\"" + description + "\" />" +
                "<meta name=\"keywords\" content=\"Historia, History, blockchain, cryptocurrency, HTA\" />" +
                "<meta name=\"robots\" content=\"index, follow\" ><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />" +
                "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" /><meta http-equiv=\"X-UA-Compatible\" " +
                "content=\"IE=edge\" ><style>" + css + "</style></head>" + html + "</html>";
            }
            return htmlRet;
        }

        public class TweetContents
		{
			public Stream Screenshot { get; set; }
		}

		[HttpGet]
		public async Task<JsonResult> ImportTweet(string tweetUrl)
		{
			string screenshotDataUrl = "";
			string imagepng = "";
			// Configure PuppeteerSharp
			await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
			var browser = await Puppeteer.LaunchAsync(new LaunchOptions
			{
				Headless = true
			});

			try
			{
				using (var page = await browser.NewPageAsync())
				{
					// Navigate to the tweet page

					await page.SetUserAgentAsync("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36");
					NavigationOptions navigationOptions = new NavigationOptions()
					{
						Timeout = 60000,
						WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
					};
					await page.SetViewportAsync(new ViewPortOptions { Width = 550, Height = 672 }); // Adjust the dimensions accordingly



					var url = "https://publish.twitter.com/?query=" + tweetUrl + "&widget=Tweet";
					await page.GoToAsync(url, navigationOptions);

					var iframeHandle = await page.QuerySelectorAsync("iframe[id^=twitter-widget-]");
					int pageHeight = (int)await page.EvaluateFunctionAsync(@"() => {
						var pageHeight = document.body.scrollHeight;
						return pageHeight;
					}");
					if (pageHeight > 1250)
					{
						if (iframeHandle != null)
						{
							//Weird hack required because of Twitter
							var boundingBox = await iframeHandle.BoundingBoxAsync();

							if (boundingBox != null)
							{
								await iframeHandle.EvaluateFunctionAsync("element => element.scrollIntoView(true)");

								await Task.Delay(500);  // Adjust the delay if needed
								await page.AddStyleTagAsync(new AddTagOptions { Content = "body { overflow: hidden; }" });
								int desiredViewportHeight = Math.Min(800, (int)boundingBox.Height);

								int maxScrollY = (int)Math.Max(0, boundingBox.Y + boundingBox.Height - desiredViewportHeight);

								await page.SetViewportAsync(new ViewPortOptions
								{
									Width = (int)boundingBox.Width,
									Height = desiredViewportHeight
								});
								// Capture a screenshot of the iframe
								imagepng = await page.ScreenshotBase64Async(new ScreenshotOptions()
								{
									Quality = 50,
									Type = ScreenshotType.Jpeg,
								});
							}
						}
					}
					else
					{
						var tweetHandles = await page.QuerySelectorAllAsync("iframe[id^=twitter-widget-]");
						var tweetHandle = tweetHandles.FirstOrDefault();

						imagepng = await tweetHandle.ScreenshotBase64Async(new ScreenshotOptions()
						{
							//FullPage = true,
							Quality = 50,
							Type = ScreenshotType.Jpeg,
						});

					}
				}
				// Close the browser
				await browser.CloseAsync();

				// Serialize tweetContents object to JSON string
				dynamic json = JsonConvert.SerializeObject(imagepng);
				return Json(json);
			}
			catch (Exception ex)
			{
				dynamic prepRespJsonFail = new { Success = false, Error = "Can't authenticate" };
				return Json(prepRespJsonFail);
			}
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
			string temp = "";
			string HTML = pageData.content;
			var sanitizer = new HtmlSanitizer();
			//Get URL



			foreach (Match item in Regex.Matches(HTML, @"(Page saved with SingleFile \n url: )(http|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?"))
			{
				temp = item.Value;
				break;
			}

			foreach (Match item in Regex.Matches(temp, @"(http|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?"))
			{
				URL = item.Value;
				break;
			}

            //Get Screenshot of Archive Site via PuppeteerSharp
            var image5jpg = "";
            var image10jpg = "";
            var image25jpg = "";
			var image50jpg = "";
			var image75jpg = "";
			var image100jpg = "";
			var imagepng = "";
			var image = "";
			int size = 0;
			bool imageTest = true;

			try
			{
				using var browserFetcher = new BrowserFetcher();
				await browserFetcher.DownloadAsync(BrowserFetcher.DefaultRevision);
				var launchOptions = new LaunchOptions()
				{
					Headless = true,
				};
				using (var browser = await Puppeteer.LaunchAsync(launchOptions))
				{
					await browser.CreateIncognitoBrowserContextAsync();
					using (var page = await browser.NewPageAsync())
					{
						await page.SetUserAgentAsync("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36");

						NavigationOptions navigationOptions = new NavigationOptions()
						{
							Timeout = 200000,
							WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
						};

						await page.GoToAsync(URL, navigationOptions);

						var bodyHandle = await page.QuerySelectorAsync("body");
						var boundingBox = await bodyHandle.BoundingBoxAsync();
						await bodyHandle.DisposeAsync();

						var viewportHeight = page.Viewport.Height;
						var viewportIncr = 0;
						while (viewportIncr + viewportHeight < boundingBox.Height)
						{
							await page.EvaluateExpressionAsync($"window.scrollBy(0, {viewportHeight});");
							await Task.Delay(20);
							viewportIncr += viewportHeight;
						}

						await page.EvaluateExpressionAsync("window.scrollTo(0, 0);");

						await Task.Delay(100); // Extra delay to let images load

						var SetViewportOptions = new ViewPortOptions()
						{
							Width = 1280,
						};
						await page.SetViewportAsync(SetViewportOptions);

						imagepng = await page.ScreenshotBase64Async(new ScreenshotOptions()
						{
							FullPage = true,
							Type = ScreenshotType.Png,
						});

						size = Encoding.Default.GetByteCount(HTML) + Encoding.Default.GetByteCount(imagepng);
						double megabytes = size / (1024.0 * 1024.0);
						if (megabytes < 15.00)
						{
							image = imagepng;
						}
						else
						{
							image75jpg = await page.ScreenshotBase64Async(new ScreenshotOptions()
							{
								FullPage = true,
								Quality = 75,
								Type = ScreenshotType.Jpeg,
							});
							size = Encoding.Default.GetByteCount(HTML) + Encoding.Default.GetByteCount(image75jpg);
							megabytes = size / (1024.0 * 1024.0);
                            if (megabytes < 15.00)
                            {
								image = image75jpg;
							}
							else
							{
								image50jpg = await page.ScreenshotBase64Async(new ScreenshotOptions()
								{
									FullPage = true,
									Quality = 50,
									Type = ScreenshotType.Jpeg,
								});
								size = Encoding.Default.GetByteCount(HTML) + Encoding.Default.GetByteCount(image50jpg);
								megabytes = size / (1024.0 * 1024.0);
                                if (megabytes < 15.00)
                                {
									image = image50jpg;
								}
								else
								{
									image25jpg = await page.ScreenshotBase64Async(new ScreenshotOptions()
									{
										FullPage = true,
										Quality = 25,
										Type = ScreenshotType.Jpeg,
									});
									size = Encoding.Default.GetByteCount(HTML) + Encoding.Default.GetByteCount(image25jpg);
									megabytes = size / (1024.0 * 1024.0);
                                    if (megabytes < 15.00)
                                    {
										image = image25jpg;
									} else
									{
                                        image10jpg = await page.ScreenshotBase64Async(new ScreenshotOptions()
                                        {
                                            FullPage = true,
                                            Quality = 10,
                                            Type = ScreenshotType.Jpeg,
                                        });
                                        size = Encoding.Default.GetByteCount(HTML) + Encoding.Default.GetByteCount(image10jpg);
                                        megabytes = size / (1024.0 * 1024.0);
                                        if (megabytes < 15.00)
                                        {
                                            image = image10jpg;
                                        } else
										{
                                            image5jpg = await page.ScreenshotBase64Async(new ScreenshotOptions()
                                            {
                                                FullPage = true,
                                                Quality = 5,
                                                Type = ScreenshotType.Jpeg,
                                            });
                                            size = Encoding.Default.GetByteCount(HTML) + Encoding.Default.GetByteCount(image10jpg);
                                            megabytes = size / (1024.0 * 1024.0);
                                            if (megabytes < 15.00)
                                            {
                                                image = image5jpg;
                                            }
                                        }

                                    }

								}

							}

						}
					}
					await browser.CloseAsync();
					await browser.DisposeAsync();
				}
				imageTest = true;
			}
			catch (Exception ex)
			{
				//Screen shot failed
				imageTest = false;
			}
			DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;

			try
			{
				string connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
				using (var conn = new SqliteConnection(connectionString))
				{
					using (var cmd = conn.CreateCommand())
					{
						conn.Open();
						cmd.CommandType = System.Data.CommandType.Text;
						cmd.CommandText = "Insert into archives (url, html, image, dateadded) VALUES (@url, @html, @image, @dateadded)";
						cmd.Parameters.AddWithValue("url", sanitizer.Sanitize(URL));
						cmd.Parameters.AddWithValue("html", HTML);
						cmd.Parameters.AddWithValue("image", image);
						cmd.Parameters.AddWithValue("dateadded", dateTimeOffset.ToUnixTimeSeconds());
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
		private string GobjectPrepareSubmit(string EndEpoch, long name, string PaymentAddress, string PaymentAmount,
			string ProposalSummary, string ProposalSummaryName, string Type, string IpfsCID, string ipfsPID, string cidType)
		{

			long BlockHeightBefore = GetBlockCount();
			long BlockHeightAfter = BlockHeightBefore;

			string PrepareResp = "0";
			var hexString = "";
			string hash = "";

			try
			{
				HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(_rpcServerUrl);
				webRequest.Credentials = new NetworkCredential(_userName, _password);
				webRequest.ContentType = "application/json-rpc";
				webRequest.Method = "POST";
				webRequest.Timeout = 5000;
				if(Type == "5")
				{
					Type = "4";
                } else if (Type == "4.1")
				{
                    Type = "4";
                }

				string proposalJson = string.Format(@"{{""end_epoch"":{0},""name"":""{1}"",""payment_address"":""{2}"",""payment_amount"":{3},""start_epoch"":{4},""type"":{5},""ipfscid"":""{6}"",""ipfscidtype"":""{7}"",""ipfspid"":""{8}"",""summary"":{{""name"":""{9}"", ""description"":""{10}""}}}}",
	EndEpoch, name, PaymentAddress, PaymentAmount, name, Type, IpfsCID, cidType, ipfsPID, ProposalSummaryName, ProposalSummary.Length > 255 ? HttpUtility.JavaScriptStringEncode(ProposalSummary.Substring(0, 254)) : HttpUtility.JavaScriptStringEncode(ProposalSummary));
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
				return "1";
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

				dynamic result = JObject.Parse(PrepareResp);

				//Get Datastring Info
				hash = result.result.ToString();

				return hash;
			}
			catch (Exception ex)
			{
				return "1";
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
