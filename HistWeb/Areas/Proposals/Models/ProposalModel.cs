using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HistWeb.Areas.Proposals.Models
{
	public class ProposalModel
	{
		public string Type { get; set; }

		public string IPFSUrl { get; set; }
		public string IPFSWebPort { get; set; }
		public string IPFSApiPort { get; set; }
		[Key]
		public int Id { get; set; }
		public string DataString { get; set; }

		public string orgUrl { get; set; }
		public List<SelectListItem> ProposalTypes { get; set; }
		public List<SelectListItem> PaymentDates { get; set; }

		public ProposalModel()
		{


		}

		public string ProposalType { get; set; }

		[Required]
		[Display(Name = "Title")]
		public string ProposalName { get; set; }

		[Display(Name = "Description Text")]
		[DataType(DataType.MultilineText)]
		public string ProposalSummary { get; set; }

		[Required]
		[Display(Name = "Description")]
		[DataType(DataType.MultilineText)]
		public string ProposalDescription { get; set; }

		[Required]
		[Display(Name = "Project URL")]
		public string ProposalDescriptionUrl { get; set; }
		public int oglinksid { get; set; }
		public string oglinksimageurl { get; set; }
        public string oglinksimage { get; set; }
        public string oglinkstitle { get; set; }
		public string oglinksurl { get; set; }
		public string oglinkssitename { get; set; }
		public string oglinksdescription { get; set; }
		public string ParentIPFSCID { get; set; }
		public string start_epoch { get; set; }
		public string cidtype { get; set; }
		public string IsUpdate { get; set; }

		public string ProposalDescriptionUrlRazor { get; set; }

		[Required]
		[Display(Name = "Payment Date")]
		public string PaymentDate { get; set; }

		[Required]
		[Display(Name = "Payment End Date")]
		public DateTime PaymentEndDate { get; set; }

		[Required]
		[Display(Name = "Proposal Date")]
		public string ProposalDate { get; set; }

		[Required]
		[Display(Name = "Payment Address")]
		public string PaymentAddress { get; set; }

		[Required]
		[Display(Name = "Payment Amount")]
		public decimal PaymentAmount { get; set; }


		public string Hash { get; set; }
		public string Hostname { get; set; }

		public long YesCount { get; set; }

		public long NoCount { get; set; }

		public long AbstainCount { get; set; }

		public bool CachedLocked { get; set; }

		public bool CachedFunding { get; set; }
		public int Expired { get; set; }
		public int PastSuperBlock { get; set; }

		public int sig { get; set; }
		public bool PermLocked { get; set; }
	}
	public class PreviousVersionsModel
	{
		public int Id { get; set; }
		public string cidtype { get; set; }
		public string Name { get; set; }
		public string Summary { get; set; }
		public string ParentIPFSCid { get; set; }

		public string IPFSHash { get; set; }

		public string url { get; set; }

		public string DateAdded { get; set; }

		public string ProposalHash { get; set; }

	}

}
