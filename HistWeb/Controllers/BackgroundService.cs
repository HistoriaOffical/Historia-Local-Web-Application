using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NCrontab;

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
using System.Data;
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
using System.Net.Mime;
using System.Net.Http;
using OpenGraphNet;
using Microsoft.Data.Sqlite;

public class RecurringJobService : BackgroundService
{
	private Timer timer;
	private IConfiguration _configuration = null;

	private readonly Dictionary<Func<Task>, (TimeSpan Interval, string CronExpression)> recurringFunctions;

	public RecurringJobService(IConfiguration config)
	{
		_configuration = config;

		recurringFunctions = new Dictionary<Func<Task>, (TimeSpan, string)>
		{
			//Prod
			{ () => GenerateProposalMatrix(_configuration), (TimeSpan.FromMinutes(1), "*/5 * * * *") },

		};

		timer = new Timer(async state => await RunScheduledTasks(), null, Timeout.Infinite, Timeout.Infinite);

		// Add logging
		Console.WriteLine("RecurringJobService constructor called");
	}

	public class datastring
	{
		public string end_epoch { get; set; }
		public string name { get; set; }
		public string payment_address { get; set; }
		public string payment_amount { get; set; }
		public string start_epoch { get; set; }
		public string type { get; set; }
		public string ipfscid { get; set; }
		public string ipfspid { get; set; }
		public string ipfscidtype { get; set; }
		public string Hash { get; internal set; }
	}

	public class DataStringObject
	{
		public SummaryObject summary { get; set; }
	}

	public class SummaryObject
	{
		public string name { get; set; }
		public string description { get; set; }
	}

	public class GovData
	{
		public string DataHex { get; set; }
		public string DataString { get; set; }
		public string fCachedFunding { get; set; }
		public string fCachedLocked { get; set; }
		public string Hash { get; set; }
		public string YesCount { get; set; }
		public string ipfscid { get; internal set; }
		public object CreationTime { get; internal set; }
		// Add other properties as needed
	}
	public class GovListResult
	{
		public Dictionary<string, GovData> Result { get; set; }
	}
	public class GovCurrentVotes
	{
		public Dictionary<string, string> result { get; set; }
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(10));
		return Task.CompletedTask;
	}

	private async Task RunScheduledTasks()
	{
		Console.WriteLine("RunScheduledTasks called");

		foreach (var kvp in recurringFunctions)
		{
			if (ShouldCallFunction(kvp.Value))
			{
				Console.WriteLine($"Calling function: {kvp.Key.Method.Name}");
				await kvp.Key.Invoke();
			}
		}
	}

	private static async Task GenerateProposalMatrix(IConfiguration _configuration)
	{

		try
		{
			int id, innerId;
			string ipfscid;
			string proposalhash;
			string ParentIPFSCID;
			using (var conn = new SqliteConnection("Data Source=basex.db"))
			{
				using (var cmd = conn.CreateCommand())
				{
					conn.Open();
					cmd.CommandType = System.Data.CommandType.Text;
					cmd.CommandText = "SELECT id, proposalhash, ipfscid, ParentIPFSCID FROM items WHERE ipfscid IS NOT NULL AND (ParentIPFSCID = '' OR ParentIPFSCID IS NULL)";

					List<dynamic> resultList1 = new List<dynamic>();

					using (SqliteDataReader rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							dynamic result = new System.Dynamic.ExpandoObject();

							result.id = rdr.GetInt32(rdr.GetOrdinal("id"));
							result.ipfscid = !rdr.IsDBNull(rdr.GetOrdinal("ipfscid")) ? rdr.GetString("ipfscid") : string.Empty;
							result.proposalhash = !rdr.IsDBNull(rdr.GetOrdinal("proposalhash")) ? rdr.GetString("proposalhash") : string.Empty;
							result.ParentIPFSCID = !rdr.IsDBNull(rdr.GetOrdinal("ParentIPFSCID")) ? rdr.GetString("ParentIPFSCID") : string.Empty;

							resultList1.Add(result);
						}
					}

					using (var conn1 = new SqliteConnection("Data Source=basex.db"))
					{
						conn1.Open();

						foreach (var result in resultList1)
						{
							using (var cmd1 = conn1.CreateCommand())
							{
								cmd1.CommandType = System.Data.CommandType.Text;

								cmd1.CommandText = "INSERT OR REPLACE INTO proposalmatrix(url, proposalmatrixid, ParentIPFS, proposalid) VALUES(@url, @proposalmatrixid, @ParentIPFS, @proposalid)";

								cmd1.Parameters.AddWithValue("url", result.ipfscid);
								cmd1.Parameters.AddWithValue("proposalmatrixid", result.id);
								cmd1.Parameters.AddWithValue("ParentIPFS", result.ParentIPFSCID);
								cmd1.Parameters.AddWithValue("proposalid", result.id);

								cmd1.ExecuteNonQuery();
							}
						}
					}



				}
			}
			List<dynamic> resultList = new List<dynamic>();
			using (var conn = new SqliteConnection("Data Source=basex.db"))
			{
				using (var cmd = conn.CreateCommand())
				{
					conn.Open();
					cmd.CommandType = System.Data.CommandType.Text;
					cmd.CommandText = "SELECT id, ipfscid, proposalhash, ParentIPFSCID FROM items WHERE ipfscid IS NOT NULL AND LENGTH(ParentIPFSCID) > 0;";



					using (SqliteDataReader rdr = cmd.ExecuteReader())
					{
						while (rdr.Read())
						{
							dynamic result = new System.Dynamic.ExpandoObject();
							result.id = rdr.GetInt32(rdr.GetOrdinal("id"));
							result.ipfscid = !rdr.IsDBNull(rdr.GetOrdinal("ipfscid")) ? rdr.GetString("ipfscid") : string.Empty;
							result.proposalhash = !rdr.IsDBNull(rdr.GetOrdinal("proposalhash")) ? rdr.GetString("proposalhash") : string.Empty;
							result.ParentIPFSCID = !rdr.IsDBNull(rdr.GetOrdinal("ParentIPFSCID")) ? rdr.GetString("ParentIPFSCID") : string.Empty;
							resultList.Add(result);
						}
					}
				}
			}

			using (var conn1 = new SqliteConnection("Data Source=basex.db"))
			{
				conn1.Open();

				// Set a longer busy timeout (e.g., 5000 milliseconds)
				using (var cmdTimeout = conn1.CreateCommand())
				{
					cmdTimeout.CommandText = "PRAGMA busy_timeout = 5000;";
					cmdTimeout.ExecuteNonQuery();
				}

				using (var transaction = conn1.BeginTransaction())
				{
					try
					{
						foreach (var result in resultList)
						{
							using (var cmd1 = conn1.CreateCommand())
							{
								cmd1.CommandType = System.Data.CommandType.Text;
								cmd1.CommandText = "SELECT id FROM proposalmatrix WHERE url = @ParentIPFS";
								cmd1.Parameters.AddWithValue("ParentIPFS", result.ParentIPFSCID);

								using (SqliteDataReader rdr1 = cmd1.ExecuteReader())
								{
									while (rdr1.Read())
									{
										innerId = rdr1.GetInt32("Id");

										using (var cmd2 = conn1.CreateCommand())
										{
											cmd2.CommandType = System.Data.CommandType.Text;
											cmd2.CommandText = "INSERT OR REPLACE INTO proposalmatrix (url, proposalmatrixid, ParentIPFS, proposalid) VALUES(@url, @proposalmatrixid, @ParentIPFS, @proposalid)";

											cmd2.Parameters.AddWithValue("url", result.ipfscid);
											cmd2.Parameters.AddWithValue("proposalmatrixid", innerId);
											cmd2.Parameters.AddWithValue("ParentIPFS", result.ParentIPFSCID);
											cmd2.Parameters.AddWithValue("proposalid", result.id);
											cmd2.ExecuteNonQuery();
										}
									}
								}
							}
						}

						// Commit the transaction after all operations
						transaction.Commit();
					}
					catch (Exception)
					{
						// Roll back the transaction on exception
						transaction.Rollback();
						throw;
					}
				}
			}
			var rep = new { Success = true };
		}
		catch (Exception ex)
		{
			var rep = new { Success = false };
		}

	}


	private bool ShouldCallFunction((TimeSpan Interval, string CronExpression) functionConfig)
	{
		try
		{
			var schedule = CrontabSchedule.Parse(functionConfig.CronExpression);
			var nextOccurrence = schedule.GetNextOccurrence(DateTime.Now);

			Console.WriteLine($"Next occurrence for {functionConfig.CronExpression}: {nextOccurrence}");
			//return DateTime.Now >= nextOccurrence;
			return DateTime.Now.AddSeconds(10) >= nextOccurrence;

		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in ShouldCallFunction: {ex}");
			return false; // Handle the error gracefully
		}
	}


}
