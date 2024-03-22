using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HistWeb.Areas.Masternode.Models
{
    public class MasternodeModel
    {
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string Hash { get; set; }
        public string CollateralHash { get; set; }
        public int CollateralIndex { get; set; }
        public string Status { get; set; }
        public string Payee { get; set; }
        public string ActiveSeconds { get; set; }
        public string LastSeen { get; set; }
        public string Identity { get; internal set; }
    }
}
