using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;




namespace HistWeb.Home.Views
{
    public partial class SettingsModel : PageModel
    {
        public SettingsModel()
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
