﻿using GrouperLib.Core;
using System.Text.Json;

namespace GrouperLib.Test;

public class EventLogItemTest
{
    private static readonly Guid documentId = Guid.Parse("3cbc0481-23b0-4860-a58a-7a723ee250c5");
    private static readonly Guid groupId = Guid.Parse("baefe5f4-d404-491d-89d0-fb192afa3c1d");
    private static readonly string groupName = "Test Group";
    private static readonly GroupStore store = GroupStore.OnPremAd;
    private static readonly string? message = "Message";
    private static readonly LogLevel logLevel = LogLevel.Error;
    private static readonly DateTime time = DateTime.Parse("2020-11-19T21:28:18.3926113+01:00");

    [Fact]
    public void TestConstruction()
    {
        EventLogItem logItem = new(
            logTime: time,
            documentId: documentId,
            groupId: groupId,
            groupDisplayName: groupName,
            groupStore: store.ToString(),
            message: message,
            logLevel: logLevel
        );
        Assert.Equal(time, logItem.LogTime);
        Assert.Equal(documentId, logItem.DocumentId);
        Assert.Equal(groupName, logItem.GroupDisplayName);
        Assert.Equal(groupId, logItem.GroupId);
        Assert.Equal(store, logItem.GroupStore);
        Assert.Equal(message, logItem.Message);
        Assert.Equal(logLevel, logItem.LogLevel);
    }

    [Fact]
    public void TestConstructionWithDocument()
    {
        GrouperDocument document = TestHelpers.MakeDocument();
        EventLogItem logItem = new(document, message, logLevel);
        Assert.Equal(TestHelpers.DefaultDocumentId, logItem.DocumentId);
        Assert.Equal(TestHelpers.DefaultGroupName, logItem.GroupDisplayName);
        Assert.Equal(TestHelpers.DefaultGroupId, logItem.GroupId);
        Assert.Equal(TestHelpers.DefaultGroupStore, logItem.GroupStore);
        Assert.Equal(message, logItem.Message);
        Assert.Equal(logLevel, logItem.LogLevel);
    }

    [Fact]
    public void TestConstructionWithEmptyGroupDisplayName()
    {
        EventLogItem logItem = new(
            logTime: time,
            documentId: documentId,
            groupId: groupId,
            groupDisplayName: string.Empty,
            groupStore: store.ToString(),
            message: message,
            logLevel: logLevel
        );
        Assert.Null(logItem.GroupDisplayName);
    }

    [Fact]
    public void TestConstructionWithoutGroupStore()
    {
        EventLogItem logItem = new(
            logTime: time,
            documentId: documentId,
            groupId: groupId,
            groupDisplayName: groupName,
            groupStore: null,
            message: message,
            logLevel: logLevel
        );
        Assert.Null(logItem.GroupStore);
    }

    [Fact]
    public void TestSerializedNames()
    {
        EventLogItem logItem = new(
            logTime: time,
            documentId: documentId,
            groupId: groupId,
            groupDisplayName: groupName,
            groupStore: store.ToString(),
            message: message,
            logLevel: logLevel
        );
        string json = JsonSerializer.Serialize(logItem);
        Dictionary<string,object>? obj = JsonSerializer.Deserialize<Dictionary<string,object>>(json);
        Assert.True(obj?.ContainsKey("logTime"));
        Assert.True(obj?.ContainsKey("documentId"));
        Assert.True(obj?.ContainsKey("groupId"));
        Assert.True(obj?.ContainsKey("groupDisplayName"));
        Assert.True(obj?.ContainsKey("groupStore"));
        Assert.True(obj?.ContainsKey("message"));
        Assert.True(obj?.ContainsKey("logLevel"));
    }
}