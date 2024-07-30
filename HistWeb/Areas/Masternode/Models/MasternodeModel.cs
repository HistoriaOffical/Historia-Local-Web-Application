using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HistWeb.Areas.Masternode.Models
{
    public class MasternodeModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IPAddress { get; set; }
        public string Hash { get; set; }
        public string CollateralHash { get; set; }
        public int CollateralIndex { get; set; }
        public string blsprivkey { get; set; }
        public string blspublickey { get; set; }
        public string VotingPrivateKey { get; set; }
        public string Status { get; set; }
        public string Payee { get; set; }

        public string feeSourceAddress { get; set; }
        public string ActiveSeconds { get; set; }
        public string LastSeen { get; set; }
        public string Identity { get; internal set; }
        public string Country { get; internal set; }
        public long PingTime { get; internal set; }
        public bool HttpsAvailable { get; internal set; }

        public string ProTxHash { get; set; }
        public string OwnerAddress { get; set; }
        public string VotingAddress { get; set; }
        public string CollateralAddress { get; set; }
        public string PubKeyOperator { get; set; }
        public string IpfsPeerId { get; set; }

        public string lastpaidtime { get; set; }

        public string lastpaidblock { get; set; }
    }

    public class CollateralHashModel
    {
        public string CollateralHash { get; set; }
        public string operatorPayoutAddress { get; set; }
    }

    public class SaveFieldsModel
    {
        public string feeSourceAddress { get; set; }
        public string blsprivkey { get; set; }
        public string blspublickey { get; set; }
        public string CollateralHash { get; set; }
    }

    public class PassphraseModel
    {
        public string Passphrase { get; set; }
        public string Time { get; set; }
    }

    public class ListUnspenteModel
    {
        public string amount { get; set; }
    }

    public class LogEntry
    {
        public string Timestamp { get; set; }
        public string LogType { get; set; }
        public string LogSource { get; set; }
        public string Log { get; set; }
    }

    public class SshConnectionInfo
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DNS { get; set; }
    }

    public class CreateUserRequest : SshConnectionInfo
    {
        public string NewUsername { get; set; }
        public string NewPassword { get; set; }
        public string SshUsername { get; set; }
        public string SshPassword { get; set; }
    }

    public class GiveSudoAccessRequest : SshConnectionInfo
    {
        public string RootUsername { get; set; }
        public string RootPassword { get; set; }
        public string TargetUsername { get; set; }
    }

    public class SignMessageModel
    {
        public string Message { get; set; }
        public string privateKey { get; set; }
    }

    public class AddUpdateMasternode
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CollateralHash { get; set; }
        public string PrivateKey { get; set; }

        public string PublicKey { get; set; }
        public bool Notify { get; set; }
        public int CollateralIndex { get; set; }

        public string status { get; set; }
        public string identity { get; set; }

        public string ipaddress { get; set; }

    }

    public class CheckMasternode
    {
        public string collateralhash { get; set; }
        public string ipaddress { get; set; }
        public int port { get; set; }
        public string username { get; set; }

        public string password { get; set; }
        public string dnsName { get; set; }


    }

}
