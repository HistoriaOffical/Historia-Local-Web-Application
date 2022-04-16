using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HistWeb.Home.Models
{
    public class HomeViewModel
    {
        public HomeViewModel()
        {
            CurrentModel = new ProposalRecordModel() { PaymentDate = DateTime.Now.ToString("MM/dd/yyyy") };
            Proposals = new List<ProposalRecordModel>();
            ProposalDetails = new List<ProposalRecordModel>();

        }
        public int PastSuperBlock { get; set; }

        public List<ProposalRecordModel> Proposals = new List<ProposalRecordModel>();
        public List<ProposalRecordModel> All = new List<ProposalRecordModel>();
        public List<ProposalRecordModel> Records = new List<ProposalRecordModel>();
        public List<ProposalRecordModel> ApprovedProposals = new List<ProposalRecordModel>();
        public List<ProposalRecordModel> ProposalDetails = new List<ProposalRecordModel>();
        public ProposalRecordModel CurrentModel;

        public ProposalRecordModel NewProposal;

    }
}
