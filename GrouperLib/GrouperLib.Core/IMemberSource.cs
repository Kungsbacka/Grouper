using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrouperLib.Core
{
    public interface IMemberSource
    {
        Task GetMembersFromSourceAsync(GroupMemberCollection memberCollection, GrouperDocumentMember grouperMember);

        IEnumerable<GroupMemberSources> GetSupportedGrouperMemberSources();
    }
}
