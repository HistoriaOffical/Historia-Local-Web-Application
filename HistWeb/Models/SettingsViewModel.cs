using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HistWeb.Home.Models
{
    public class SettingsViewModel : PageModel
    {
        public SettingsViewModel()
        {

        }
        public string IPFSHost { get; set; }
        public int IPFSPort { get; set; }
        public string IPFSApiHost { get; set; }
        public int IPFSApiPort { get; set; }
        public string HistoriaClientIPAddress { get; set; }
        public int HistoriaRPCPort { get; set; }
        public string HistoriaRPCUserName { get; set; }
        public string HistoriaRPCPassword { get; set; }
    }
}
