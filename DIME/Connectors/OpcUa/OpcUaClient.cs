using System.Text;
using LibUA;
using LibUA.Core;

namespace DIME.Connectors.OpcUa;

public class OpcUaClient
    {
        private Client _client = null;
        private bool _useAnonymousUser = false;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private MessageSecurityMode _messageSecurityMode = MessageSecurityMode.None;
        private SecurityPolicy _securityPolicy = SecurityPolicy.None;
        
        public OpcUaClient(
            string address, 
            int port, 
            int timeoutMs, 
            bool useAnonymousUser = true,
            string username = null,
            string password = null,
            MessageSecurityMode securityMode = MessageSecurityMode.None, 
            SecurityPolicy securityPolicy = SecurityPolicy.None)
            {
                _client = new Client(address, port, timeoutMs);
                
                _useAnonymousUser = useAnonymousUser;
                _username = username;
                _password = password;
                _messageSecurityMode = securityMode;
                _securityPolicy = securityPolicy;
            }

        public bool Connect()
        {
            var appDesc = new ApplicationDescription(
                "urn:DIMEApplication", "uri:DIMEApplication", new LocalizedText("DIME client"),
                ApplicationType.Client, null, null, null);
            
            _client.Connect();
            _client.OpenSecureChannel(MessageSecurityMode.None, SecurityPolicy.None, null);
            _client.FindServers(out ApplicationDescription[] appDescs, new[] { "en" });
            _client.GetEndpoints(out EndpointDescription[] endpointDescs, new[] { "en" });
            _client.Disconnect();
        
            var endpointDesc = endpointDescs.First(e =>
                e.SecurityMode == _messageSecurityMode &&
                e.SecurityPolicyUri == Types.SLSecurityPolicyUris[(int)_securityPolicy]);
            byte[] serverCert = endpointDesc.ServerCertificate;
        
            var connectRes = _client.Connect();
            var openRes = _client.OpenSecureChannel(_messageSecurityMode, _securityPolicy, serverCert);
            var createRes = _client.CreateSession(appDesc, "urn:DIMEApplication", 120);

            StatusCode activateRes;
            if (_useAnonymousUser)
            {
                // Will fail if this endpoint does not allow Anonymous user tokens
                string policyId = endpointDesc.UserIdentityTokens.First(e => e.TokenType == UserTokenType.Anonymous).PolicyId;
                activateRes = _client.ActivateSession(new UserIdentityAnonymousToken(policyId), new[] { "en" });
            }
            else
            {
                // Will fail if this endpoint does not allow UserName user tokens
                string policyId = endpointDesc.UserIdentityTokens.First(e => e.TokenType == UserTokenType.UserName).PolicyId;
                activateRes = _client.ActivateSession(
                    new UserIdentityUsernameToken(policyId, _username,
                        (new UTF8Encoding()).GetBytes(_password), Types.SignatureAlgorithmRsaOaep),
                    new[] { "en" });
            }
            
            return _client.IsConnected;
        }

        public bool Disconnect()
        {
            _client.Disconnect();
            return !_client.IsConnected;
        }

        public DataValue Read(ushort namespaceId, string stringIdentifier)
        {
            var readRes = _client.Read(new ReadValueId[]
            {
                new ReadValueId(new NodeId(namespaceId, stringIdentifier), NodeAttribute.Value, null, new QualifiedName(0, null))
            }, out DataValue[] dvs);

            return dvs[0];
        }
        
        public DataValue[] Read(List<Tuple<ushort,string>> identifiers)
        {
            var readValueIds = identifiers
                .Select(x => new ReadValueId(
                    new NodeId(x.Item1, x.Item2), 
                    NodeAttribute.Value, 
                    null, 
                    new QualifiedName(0, null))
                )
                .ToArray();
            
            var readRes = _client.Read(readValueIds, out DataValue[] dvs);
            
            return dvs;
        }
    }