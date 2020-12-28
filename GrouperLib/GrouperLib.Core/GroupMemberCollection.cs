using System;
using System.Collections.Generic;

namespace GrouperLib.Core
{
    // This collection does not implement the typical interfaces (i.g. IEnumerable<T>),
    // because it is going to be used i PowerShell and it is easy to accidentaly enumerate
    // the contents instead of passing the object. To enumerate call AsEnumerable() to get
    // an IEnumerable<GroupMember>.
    public sealed class GroupMemberCollection
    {
        private readonly HashSet<GroupMember> _members;

        public int Count => _members.Count;

        public GroupMemberCollection()
        {
            _members = new HashSet<GroupMember>();
        }

        public void Add(GroupMember member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }
            _members.Add(member);
        }

        public void Add(GroupMemberCollection collection) => _members.UnionWith(collection.AsEnumerable());

        public IEnumerable<GroupMember> AsEnumerable() => _members;

        public void ExceptWith(GroupMemberCollection collection) => _members.ExceptWith(collection.AsEnumerable());

        protected void SymmetricExceptWith(GroupMemberCollection collection) => _members.SymmetricExceptWith(collection.AsEnumerable());

        public void IntersectWith(GroupMemberCollection collection) => _members.IntersectWith(collection.AsEnumerable());

        public GroupMemberCollection Clone()
        {
            GroupMemberCollection newCollection = new GroupMemberCollection();
            newCollection.Add(this);
            return newCollection;
        }

        /* FilterUniqueMember filter two hash sets so that they only contain
         * objects that are unique to each set.
         *
         * {A, B, C} and {C, D, E} => (FilterUniqueMember) => {A, B} and {D, E}
         */
        public void FilterUniqueMember(GroupMemberCollection collection)
        {
            // 1. Copy all members from set A (this set) to set B (set in collection),
            //    removing all members that exists in both sets. Now set B will contain
            //    all *unique* members from both sets.
            collection.SymmetricExceptWith(this);
            // 2. Remove all members from set A is not in set B. Now set A will only contain
            //    members that was unique to that set from the beginning.
            IntersectWith(collection);
            // 3. Remove all members from set B that is in set A. This does the same with
            //    set B, leaving only truly unique members in set B.
            collection.ExceptWith(this);
        }

        public bool ContainsMatchingMemberType(GroupMemberCollection collection)
        {
            if (Count == 0)
            {
                return MemberTypesAsFlags(collection) < 3;
            }
            if (collection.Count == 0)
            {
                return MemberTypesAsFlags(this) < 3;
            }
            int thisMemberTypes = MemberTypesAsFlags(this);
            int otherMemberTypes = MemberTypesAsFlags(collection);
            return thisMemberTypes == otherMemberTypes && thisMemberTypes < 3;
        }

        private static int MemberTypesAsFlags(GroupMemberCollection collection)
        {
            int flags = 0;
            foreach (GroupMember member in collection.AsEnumerable())
            {
                if (member.MemberType == GroupMemberTypes.OnPremAd)
                {
                    flags |= 1;
                }
                else // AzureAd
                {
                    flags |= 2;
                }
            }
            return flags;
        }
    }
}
