using GrouperLib.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace GrouperLib.Test
{
    static class TestHelpers
    {
        public static GrouperDocument MakeDocument(dynamic def)
        {
            var members = new List<GrouperDocumentMember>();
            foreach (dynamic member in def.Members)
            {
                members.Add(MakeMember(member));
            }
            return (GrouperDocument)Activator.CreateInstance(typeof(GrouperDocument), BindingFlags.Instance | BindingFlags.NonPublic, binder: null, culture: null, args: new object[] {
                def.Id, def.Interval, def.GroupId, def.GroupName, def.Store, def.Owner, members
            });
        }

        public static GrouperDocumentMember MakeMember(dynamic def)
        {
            var rules = new List<GrouperDocumentRule>();
            foreach (dynamic rule in def.Rules)
            {
                rules.Add(MakeRule(rule));
            }
            return (GrouperDocumentMember)Activator.CreateInstance(typeof(GrouperDocumentMember), BindingFlags.Instance | BindingFlags.NonPublic, binder: null, culture: null, args: new object[] {
                def.Source, def.Action, rules
            });
        }

        public static GrouperDocumentRule MakeRule(dynamic def)
        {
            return (GrouperDocumentRule)Activator.CreateInstance(typeof(GrouperDocumentRule), BindingFlags.Instance | BindingFlags.NonPublic, binder: null, culture: null, args: new object[] {
                def.Name, def.Value.ToString()
            });
        }
    }
}
