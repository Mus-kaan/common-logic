//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Contracts;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Liftr.StatusStore.Blob.Tests
{
    public class StatusWriterTests
    {
        private readonly ITestOutputHelper _output;

        public StatusWriterTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [CheckInValidation(skipLinux: true)]
        public async Task TwoMachineUpdateSameStatusAsync()
        {
            using (var scope = new TestResourceGroupScope("ut-status-", _output))
            {
                try
                {
                    var az = scope.Client;
                    var rg = await az.CreateResourceGroupAsync(TestCommon.Location, scope.ResourceGroupName, TestCommon.Tags);
                    var name = SdkContext.RandomResourceName("st", 15);
                    var st = await az.GetOrCreateStorageAccountAsync(TestCommon.Location, scope.ResourceGroupName, name, TestCommon.Tags);

                    var connectionStr = await st.GetPrimaryConnectionStringAsync();

                    BlobServiceClient blobServiceClient = new BlobServiceClient(connectionStr);
                    var container = blobServiceClient.GetBlobContainerClient("rw-container");
                    await container.CreateIfNotExistsAsync();
                    var ts = new MockTimeSource();
                    ts.Add(TimeSpan.FromMinutes(1200));

                    var writer1 = new WriterMetaData()
                    {
                        MachineName = "machine11111",
                        RunningSessionId = "39274bf8-7f9a-41aa-9540-331442c75f74",
                        ProcessStartTime = ts.UtcNow,
                        VMName = "testvm1111111111",
                    };

                    ts.Add(TimeSpan.FromSeconds(17.6));
                    var writer2 = new WriterMetaData()
                    {
                        MachineName = "machine22222",
                        RunningSessionId = "f991e6b5-5602-4fab-b32e-dd423b53957e",
                        ProcessStartTime = ts.UtcNow,
                        VMName = "testvm22222222",
                    };

                    ts.Add(TimeSpan.FromMinutes(1200));
                    BlobStatusStore store1 = new BlobStatusStore(writer1, container, ts, scope.Logger);
                    BlobStatusStore store2 = new BlobStatusStore(writer2, container, ts, scope.Logger);

                    var key = "key1";
                    var value1 = "value1";
                    var value2 = "value2";

                    ts.Add(TimeSpan.FromMilliseconds(137));
                    await store1.UpdateStateAsync(key, value1);
                    var records = await store1.GetStateAsync(key);

                    Assert.Single(records);

                    // Check records
                    {
                        var w = writer1;
                        var r = records.First();
                        Assert.Equal(key, r.Key);
                        Assert.Equal(value1, r.Value);
                        Assert.Equal(w.MachineName, r.MachineName);
                        Assert.Equal(w.RunningSessionId, r.RunningSessionId);
                        Assert.Equal(w.ProcessStartTime, r.ProcessStartTime);
                        Assert.Equal(w.VMName, r.VMName);
                    }

                    ts.Add(TimeSpan.FromMilliseconds(137));
                    await store2.UpdateStateAsync(key, value2);

                    records = await store1.GetStateAsync(key);

                    Assert.Equal(2, records.Count());

                    // Check records
                    {
                        var w = writer1;
                        var r = records.First();
                        Assert.Equal(key, r.Key);
                        Assert.Equal(value1, r.Value);
                        Assert.Equal(w.MachineName, r.MachineName);
                        Assert.Equal(w.RunningSessionId, r.RunningSessionId);
                        Assert.Equal(w.ProcessStartTime, r.ProcessStartTime);
                        Assert.Equal(w.VMName, r.VMName);
                    }

                    // Check records
                    {
                        var w = writer2;
                        var r = records[1];
                        Assert.Equal(key, r.Key);
                        Assert.Equal(value2, r.Value);
                        Assert.Equal(w.MachineName, r.MachineName);
                        Assert.Equal(w.RunningSessionId, r.RunningSessionId);
                        Assert.Equal(w.ProcessStartTime, r.ProcessStartTime);
                        Assert.Equal(w.VMName, r.VMName);
                    }

                    // check self record
                    {
                        var w = writer1;
                        var r = await store1.GetCurrentMachineStateAsync(key);
                        Assert.Equal(key, r.Key);
                        Assert.Equal(value1, r.Value);
                        Assert.Equal(w.MachineName, r.MachineName);
                        Assert.Equal(w.RunningSessionId, r.RunningSessionId);
                        Assert.Equal(w.ProcessStartTime, r.ProcessStartTime);
                        Assert.Equal(w.VMName, r.VMName);
                    }

                    // check self record
                    {
                        var w = writer2;
                        var r = await store2.GetCurrentMachineStateAsync(key);
                        Assert.Equal(key, r.Key);
                        Assert.Equal(value2, r.Value);
                        Assert.Equal(w.MachineName, r.MachineName);
                        Assert.Equal(w.RunningSessionId, r.RunningSessionId);
                        Assert.Equal(w.ProcessStartTime, r.ProcessStartTime);
                        Assert.Equal(w.VMName, r.VMName);
                    }

                    // same key new version
                    ts.Add(TimeSpan.FromMilliseconds(137));
                    await store1.UpdateStateAsync(key, value2);

                    // Check records
                    {
                        var w = writer1;
                        var r = await store1.GetCurrentMachineStateAsync(key);
                        Assert.Equal(key, r.Key);
                        Assert.Equal(value2, r.Value);
                        Assert.Equal(w.MachineName, r.MachineName);
                        Assert.Equal(w.RunningSessionId, r.RunningSessionId);
                        Assert.Equal(w.ProcessStartTime, r.ProcessStartTime);
                        Assert.Equal(w.VMName, r.VMName);
                    }

                    // Check history
                    records = await store1.GetHistoryAsync(key);
                    Assert.Equal(3, records.Count());

                    // Multiple key verification
                    var key2 = "key2";
                    var val2 = "asfafvalue22222as";

                    ts.Add(TimeSpan.FromSeconds(137));
                    await store1.UpdateStateAsync(key2, value1);

                    ts.Add(TimeSpan.FromSeconds(137));
                    await store1.UpdateStateAsync(key2, value2);

                    ts.Add(TimeSpan.FromSeconds(137));
                    await store1.UpdateStateAsync(key2, val2);

                    // Check records
                    {
                        var w = writer1;
                        var r = await store1.GetCurrentMachineStateAsync(key2);
                        Assert.Equal(key2, r.Key);
                        Assert.Equal(val2, r.Value);
                        Assert.Equal(w.MachineName, r.MachineName);
                        Assert.Equal(w.RunningSessionId, r.RunningSessionId);
                        Assert.Equal(w.ProcessStartTime, r.ProcessStartTime);
                        Assert.Equal(w.VMName, r.VMName);
                    }

                    // Check history
                    records = await store1.GetHistoryAsync(key2);
                    Assert.Equal(3, records.Count());

                    records = await store1.GetHistoryAsync();
                    Assert.Equal(6, records.Count());
                }
                catch (Exception ex)
                {
                    scope.Logger.Error(ex, "StatusWriter test Failed.");
                    scope.TimedOperation.FailOperation(ex.Message);
                    throw;
                }
            }
        }
    }
}
