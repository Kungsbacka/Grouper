﻿using System;
using Xunit;
using GrouperLib.Core;

namespace GrouperLib.Test
{
    public class GroupInfoTest
    {
        private static readonly string validGuidString = "16551e3f-6ce9-4630-b873-e50321bf97a7";
        private static readonly Guid validGuid = Guid.Parse(validGuidString);
        private static readonly string invalidGuid = "not-a-valid-guid";

        [Fact]
        public void TestConstruction1()
        {
            GroupInfo info = new GroupInfo(validGuid, "Name", GroupStores.OnPremAd);
            Assert.Equal(validGuid, info.Id);
            Assert.Equal("Name", info.DisplayName);
            Assert.Equal(GroupStores.OnPremAd, info.Store);
        }

        [Fact]
        public void TestConstruction2()
        {
            GroupInfo info = new GroupInfo(validGuidString, "Name", GroupStores.OnPremAd);
            Assert.Equal(validGuid, info.Id);
            Assert.Equal("Name", info.DisplayName);
            Assert.Equal(GroupStores.OnPremAd, info.Store);
        }

        [Fact]
        public void TestConstructionWithInvalidGuid()
        {
            Assert.Throws<ArgumentException>(() => { new GroupInfo(invalidGuid, "Name", GroupStores.OnPremAd); });
        }
    }
}
