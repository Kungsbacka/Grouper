using System.DirectoryServices.Protocols;
using System.Net;

namespace GrouperLib.Store;

internal class Ldap
{

    private LdapConnection? _ldapConnection;
    private string? _defaultNamingContext;
    private string? _ldapServer;
    private readonly NetworkCredential? _credential;

    private const string CatchAllFilter = "(objectClass=*)";
    private const string DnsHostNameAttribute = "dNSHostName";
    private const string DefaultNamingContextAttribute = "defaultNamingContext";


    public Ldap() { }

    public Ldap(string userName, string password)
    {
        string[] parts = userName.Split('\\');
        if (parts.Length != 2)
        {
            throw new ArgumentException(nameof(userName));
        }
        _credential = new NetworkCredential(parts[1], password, parts[0]);
    }

    public async IAsyncEnumerable<SearchResultEntry> GetObjectsAsync(string searchBase, string? ldapFilter, SearchScope searchScope, params string[] attributeList)
    {
        SearchRequest searchRequest = new(searchBase, ldapFilter, searchScope, attributeList);
        PageResultRequestControl pageResultRequestControl = new(pageSize: 1000);
        searchRequest.Controls.Add(pageResultRequestControl);
        while(true)
        {
            SearchResponse searchResponse = await SendSearchRequestAsync(searchRequest);
            foreach (SearchResultEntry entry in searchResponse.Entries)
            {
                yield return entry;
            }
            PageResultResponseControl? pageResultResponseControl = 
                searchResponse.Controls.OfType<PageResultResponseControl>().FirstOrDefault();
            if (pageResultResponseControl == null || pageResultResponseControl.Cookie.Length == 0)
            {
                break;
            }
            pageResultRequestControl.Cookie = pageResultResponseControl.Cookie;
        }
    }

    public async IAsyncEnumerable<SearchResultEntry> SearchObjectsAsync(string? ldapFilter, params string[] attributeList)
    {
        (string _, string defaultNamingContext) = GetServerAndDefaultNamingContext();

        await foreach (SearchResultEntry entry in GetObjectsAsync(
                           defaultNamingContext,
                           ldapFilter,
                           SearchScope.Subtree,
                           attributeList
                       ))
        {
            yield return entry;
        }
    }

    public static string GetObjectGuidFilter(Guid guid)
    {
        return string.Concat("(objectGUID=", ConvertToLdapGuidString(guid), ")");

    }

    public static string GetMemberOfFilter(string distinguishedName)
    {
        return string.Concat("(memberOf=", distinguishedName, ")");
    }

    public static string ConvertToLdapGuidString(Guid guid)
    {
        byte[] guidBytes = guid.ToByteArray();
        char[] charArray = new char[48];

        Span<char> span = charArray.AsSpan();
        int i = 0;

        foreach (byte b in guidBytes)
        {
            span[i++] = '\\';
            span[i++] = Helpers.HexChar(b >> 4);
            span[i++] = Helpers.HexChar(b);
        }

        return new string(span);
    }

    public async Task<SearchResponse> SendSearchRequestAsync(SearchRequest request)
    {
        return (SearchResponse)(await SendRequestAsync(request));
    }

    public async Task<ModifyResponse> SendModifyRequestAsync(ModifyRequest request)
    {
        return (ModifyResponse)(await SendRequestAsync(request));
    }

    private async Task<DirectoryResponse> SendRequestAsync(DirectoryRequest request)
    {
        LdapConnection connection = GetConnection();
        return await Task.Factory.FromAsync((callback, state) =>
                connection.BeginSendRequest(request, PartialResultProcessing.NoPartialResultSupport, callback, state),
            connection.EndSendRequest,
            null
        );
    }

    public (string Server, string DefaultNamingContext) GetServerAndDefaultNamingContext()
    {
        if (_defaultNamingContext != null && _ldapServer != null)
        {
            return (Server: _ldapServer, DefaultNamingContext: _defaultNamingContext);
        }
        LdapConnection ldapConnection = new(new LdapDirectoryIdentifier(null));
        SearchRequest searchRequest = new(
            distinguishedName: null,
            CatchAllFilter,
            SearchScope.Base,
            DnsHostNameAttribute,
            DefaultNamingContextAttribute
        );
        SearchResponse searchResponse = (SearchResponse)ldapConnection.SendRequest(searchRequest);
        _ldapServer = searchResponse.Entries[0].GetAsString(DnsHostNameAttribute) ??
                      throw new InvalidOperationException("Could not retrieve default LDAP server");
        _defaultNamingContext = searchResponse.Entries[0].GetAsString(DefaultNamingContextAttribute) ??
                                throw new InvalidOperationException("Could not retrieve default naming context");
        return (Server: _ldapServer, DefaultNamingContext: _defaultNamingContext);
    }

    private LdapConnection GetConnection()
    {
        if (_ldapConnection != null)
        {
            return _ldapConnection;
        }

        (string server, string dnc) = GetServerAndDefaultNamingContext();
        _defaultNamingContext = dnc;
        _ldapConnection = new LdapConnection(server)
        {
            AuthType = AuthType.Kerberos
        };
        _ldapConnection.SessionOptions.Sealing = true;
        _ldapConnection.SessionOptions.Signing = true;
        _ldapConnection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
        _ldapConnection.SessionOptions.RootDseCache = true;
        if (_credential != null)
        {
            _ldapConnection.Credential = _credential;
        }
        return _ldapConnection;
    }
}