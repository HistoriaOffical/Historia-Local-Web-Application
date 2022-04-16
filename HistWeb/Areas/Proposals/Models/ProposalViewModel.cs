using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HistWeb.Areas.Proposals.Models
{
    public class ProposalViewModel
    {
        public ProposalViewModel()
        {
            CurrentModel = new ProposalModel() { PaymentDate = DateTime.Now.ToString("MM/dd/yyyy") };
            Proposals = new List<ProposalModel>();
            ProposalDetails = new List<ProposalModel>();
        }

        public List<ProposalModel> Proposals = new List<ProposalModel>();
        public List<ProposalModel> ApprovedProposals = new List<ProposalModel>();
        public List<ProposalModel> ProposalDetails = new List<ProposalModel>();

        public ProposalModel CurrentModel;

        public ProposalModel NewProposal;

    }
}
