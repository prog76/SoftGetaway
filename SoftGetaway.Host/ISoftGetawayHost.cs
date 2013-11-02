using System.Collections.Generic;
using System.ServiceModel;
using softGetaway;
using System.Runtime.Serialization;
using System;

namespace softGetawayHost
{
    [ServiceContract(Namespace = "http://softGetaway")]
    public interface ISoftGetawayHost
    {     
        [OperationContract]
        bool IsShouldStart();
        
        [OperationContract]
        void Start(string sharedConnectionGuid);

        [OperationContract]
        void Stop();

        [OperationContract]
        bool SetPrivateConnectionSettings(ConnectionSettings settings);

        [OperationContract]
        ConnectionSettings GetPrivateConnectionSettings();

        [OperationContract]
        IEnumerable<SharableConnection> GetSharableConnections();  
        
        [OperationContract]
        string GetSharedConnection();

        [OperationContract]
        IEnumerable<NetworkPeerService> GetPeers();

        [OperationContract]
        IEnumerable<String> GetTraceLines();

        [OperationContract]
        void SetPeer(NetworkPeerService peer);

        [OperationContract]
        bool SetIP(string ip);

        [OperationContract]
        string GetIP();

        [OperationContract]
        void LoadConfig();

        [OperationContract]
        void SaveConfig();    
        
        [OperationContract]
        getawayState GetState();
    }

    [DataContract]
    public enum getawayState {
        [EnumMember]
        Idle,
        [EnumMember]
        Initialization,
        [EnumMember]
        Starting,
        [EnumMember]
        StartFailed,
        [EnumMember]
        Started,
        [EnumMember]
        Stopping,
        [EnumMember]
        StopFailed,
        [EnumMember]
        Stopped,
        [EnumMember]
        StartingIP
    }
}
