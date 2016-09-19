﻿using Amazon.EC2;
using Amazon.EC2.Model;
using log4net;
using Nager.AmazonEc2.Helper;
using Nager.AmazonEc2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Nager.AmazonEc2.Project
{
    public class Couchbase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Couchbase));
        private AmazonEC2Client _client;

        public Couchbase(AmazonAccessKey accessKey)
        {
            this._client = new AmazonEC2Client(accessKey.AccessKeyId, accessKey.SecretKey, Amazon.RegionEndpoint.EUWest1);
        }

        private string CreateSecurityGroup(string prefix)
        {
            var name = $"{prefix}.Couchbase";
            var description = "This security group was generated by Nager.AmazonEc2 System";

            try
            {
                var result = this._client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest() { GroupNames = new List<string> { name } });
                if (result.HttpStatusCode == HttpStatusCode.OK)
                {
                    return result.SecurityGroups.Select(o => o.GroupId).FirstOrDefault();
                }
            }
            catch (AmazonEC2Exception exception)
            {
                if (exception.ErrorCode != "InvalidGroup.NotFound")
                {
                    return null;
                }
            }

            var createSecurityGroupRequest = new CreateSecurityGroupRequest(name, description);
            var createSecurityGroupResponse = this._client.CreateSecurityGroup(createSecurityGroupRequest);

            if (createSecurityGroupResponse.HttpStatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            //http://docs.couchbase.com/admin/admin/Install/install-networkPorts.html

            var ipPermissionWebinterface = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 8091,
                ToPort = 8091,
                IpRanges = new List<string>() { "0.0.0.0/0" },
                UserIdGroupPairs = new List<UserIdGroupPair>() { new UserIdGroupPair() { GroupId = createSecurityGroupResponse.GroupId } }
            };

            var ipPermissionApi = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 8092,
                ToPort = 8092,
                IpRanges = new List<string>() { "0.0.0.0/0" },
                UserIdGroupPairs = new List<UserIdGroupPair>() { new UserIdGroupPair() { GroupId = createSecurityGroupResponse.GroupId } }
            };

            var ipPermissionInternalExternalBucketSsl = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 11207,
                ToPort = 11207,
                IpRanges = new List<string>() { "0.0.0.0/0" },
            };

            var ipPermissionInternalBucket = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 11209,
                ToPort = 11209,
                UserIdGroupPairs = new List<UserIdGroupPair>() { new UserIdGroupPair() { GroupId = createSecurityGroupResponse.GroupId } }
            };

            var ipPermissionInternalExternalBucket = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 11210,
                ToPort = 11210,
                IpRanges = new List<string>() { "0.0.0.0/0" },
                UserIdGroupPairs = new List<UserIdGroupPair>() { new UserIdGroupPair() { GroupId = createSecurityGroupResponse.GroupId } }
            };

            var ipPermissionClientInterface = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 11211,
                ToPort = 11211,
                IpRanges = new List<string>() { "0.0.0.0/0" },
            };

            var ipPermissionIncomingSslProxy = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 11214,
                ToPort = 11214,
                IpRanges = new List<string>() { "0.0.0.0/0" },
            };

            var ipPermissionOutgoingSslProxy = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 11215,
                ToPort = 11215,
                IpRanges = new List<string>() { "0.0.0.0/0" },
            };

            var ipPermissionInternalRest = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 18091,
                ToPort = 18091,
                IpRanges = new List<string>() { "0.0.0.0/0" },
            };

            var ipPermissionInternalCapi = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 18092,
                ToPort = 18092,
                IpRanges = new List<string>() { "0.0.0.0/0" },
            };

            var ipPermissionErlangPortMapper = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 4369,
                ToPort = 4369,
                UserIdGroupPairs = new List<UserIdGroupPair>() { new UserIdGroupPair() { GroupId = createSecurityGroupResponse.GroupId } }
            };

            var ipPermissionNodeDataExchange = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 21100,
                ToPort = 21299,
                UserIdGroupPairs = new List<UserIdGroupPair>() { new UserIdGroupPair() { GroupId = createSecurityGroupResponse.GroupId } }
            };

            var ipPermissionSsh = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 22,
                ToPort = 22,
                IpRanges = new List<string>() { "0.0.0.0/0" },
            };

            var ingressRequest = new AuthorizeSecurityGroupIngressRequest();
            ingressRequest.GroupId = createSecurityGroupResponse.GroupId;
            ingressRequest.IpPermissions = new List<IpPermission>();
            ingressRequest.IpPermissions.Add(ipPermissionWebinterface);
            ingressRequest.IpPermissions.Add(ipPermissionApi);
            ingressRequest.IpPermissions.Add(ipPermissionInternalExternalBucketSsl);
            ingressRequest.IpPermissions.Add(ipPermissionInternalBucket);
            ingressRequest.IpPermissions.Add(ipPermissionInternalExternalBucket);
            ingressRequest.IpPermissions.Add(ipPermissionClientInterface);
            ingressRequest.IpPermissions.Add(ipPermissionIncomingSslProxy);
            ingressRequest.IpPermissions.Add(ipPermissionOutgoingSslProxy);
            ingressRequest.IpPermissions.Add(ipPermissionInternalRest);
            ingressRequest.IpPermissions.Add(ipPermissionInternalCapi);
            ingressRequest.IpPermissions.Add(ipPermissionErlangPortMapper);
            ingressRequest.IpPermissions.Add(ipPermissionNodeDataExchange);
            ingressRequest.IpPermissions.Add(ipPermissionSsh);

            var ingressResponse = this._client.AuthorizeSecurityGroupIngress(ingressRequest);
            if (ingressResponse.HttpStatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            return createSecurityGroupResponse.GroupId;
        }

        public string GetManagementUrl(List<InstallResult> installResults)
        {
            var results = this._client.DescribeInstances(new DescribeInstancesRequest() { InstanceIds = new List<string> { installResults.First().InstanceId } });
            var publicUrl = results.Reservations[0]?.Instances[0]?.PublicDnsName;

            return $"http://{publicUrl}:8091/";
        }

        public List<InstallResult> InstallCluster(CouchbaseClusterConfig clusterConfig)
        {
            Log.Debug("InstallCluster");

            var securityGroupId = this.CreateSecurityGroup(clusterConfig.Prefix);

            var installResults = new List<InstallResult>();
            var result = InstallNode(clusterConfig.NodeInstance, $"{clusterConfig.ClusterName}.node0", securityGroupId, clusterConfig.KeyName, null, clusterConfig.AdminUsername, clusterConfig.AdminPassword);
            installResults.Add(result);

            for (var i = 1; i < clusterConfig.NodeCount; i++)
            {
                var resultSlave = InstallNode(clusterConfig.NodeInstance, $"{clusterConfig.ClusterName}.node{i}", securityGroupId, clusterConfig.KeyName, result.PrivateIpAddress, clusterConfig.AdminUsername, clusterConfig.AdminPassword);
                installResults.Add(resultSlave);
            }

            return installResults;
        }

        public InstallResult InstallNode(AmazonInstance amazonInstance, string name, string securityGroupId, string keyName, string clusterIpAddress, string adminUsername, string adminPassword)
        {
            var instanceInfo = InstanceInfoHelper.GetInstanceInfo(amazonInstance);

            var instanceRequest = new RunInstancesRequest();
            instanceRequest.ImageId = "ami-7abd0209"; //centos
            instanceRequest.InstanceType = instanceInfo.InstanceType;
            instanceRequest.MinCount = 1;
            instanceRequest.MaxCount = 1;
            instanceRequest.KeyName = keyName;
            instanceRequest.SecurityGroupIds = new List<string>() { securityGroupId };

            if (!instanceInfo.LocalStorage)
            {
                var blockDeviceMappingSystem = new BlockDeviceMapping
                {
                    DeviceName = "/dev/sda1",
                    Ebs = new EbsBlockDevice
                    {
                        DeleteOnTermination = true,
                        VolumeType = VolumeType.Gp2,
                        VolumeSize = 12
                    }
                };

                var blockDeviceMappingData = new BlockDeviceMapping
                {
                    DeviceName = "/dev/sda2",
                    Ebs = new EbsBlockDevice
                    {
                        DeleteOnTermination = true,
                        VolumeType = VolumeType.Io1,
                        Iops = 100,
                        VolumeSize = (int)Math.Ceiling(instanceInfo.Memory * 2)
                    }
                };

                instanceRequest.BlockDeviceMappings.Add(blockDeviceMappingSystem);
                instanceRequest.BlockDeviceMappings.Add(blockDeviceMappingData);
            }

            //Install Process can check in this log file
            //</var/log/cloud-init-output.log>
            instanceRequest.UserData = InstallScriptHelper.CreateLinuxScript(GetInstallScript(clusterIpAddress, adminUsername, adminPassword, instanceInfo));

            var response = this._client.RunInstances(instanceRequest);
            var instance = response.Reservation.Instances.First();

            var installResult = new InstallResult();
            installResult.Name = name;
            installResult.InstanceId = instance.InstanceId;
            installResult.PrivateIpAddress = instance.PrivateIpAddress;

            var tags = new List<Tag> { new Tag("Name", name) };
            this._client.CreateTags(new CreateTagsRequest(new List<string>() { instance.InstanceId }, tags));

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                installResult.Successful = true;
            }

            return installResult;
        }

        private static List<string> GetInstallScript(string clusterIpAddress, string adminUsername, string adminPassword, AmazonInstanceInfo instanceInfo)
        {
            var items = new List<string>();

            //Prepare Data Disk
            items.AddRange(InstallScriptHelper.PrepareDataDisk());

            //Disable Swap
            items.Add("sysctl vm.swappiness=1");
            items.Add("echo \"vm.swappiness = 1\" >> /etc/sysctl.conf");

            items.Add("echo never > /sys/kernel/mm/transparent_hugepage/enabled");
            items.Add("echo \"if test -f /sys/kernel/mm/transparent_hugepage/enabled; then\" >> /etc/rc.local");
            items.Add("echo \"  echo never > /sys/kernel/mm/transparent_hugepage/enabled\" >> /etc/rc.local");
            items.Add("echo \"fi\" >> /etc/rc.local");

            items.Add("echo never > /sys/kernel/mm/transparent_hugepage/defrag");
            items.Add("echo \"if test -f /sys/kernel/mm/transparent_hugepage/defrag; then\" >> /etc/rc.local");
            items.Add("echo \"  echo never > /sys/kernel/mm/transparent_hugepage/defrag\" >> /etc/rc.local");
            items.Add("echo \"fi\" >> /etc/rc.local");

            //Install Couchbase
            items.Add("curl -O -s http://packages.couchbase.com/releases/4.5.0/couchbase-server-enterprise-4.5.0-centos7.x86_64.rpm");
            items.Add("rpm -i couchbase-server-enterprise-4.5.0-centos7.x86_64.rpm");

            items.Add("mkdir /data/couchbase");
            items.Add("chown couchbase:couchbase /data/couchbase -R");

            items.Add("sudo yum install openssl098e");

            items.Add("service couchbase-server start");

            //Wait for webinterface
            items.Add("until $(curl --output /dev/null --silent --head --fail http://localhost:8091); do");
            items.Add("  printf '.'");
            items.Add("  sleep 5");
            items.Add("done");

            //Change data path
            items.Add("/opt/couchbase/bin/couchbase-cli node-init -c localhost:8091 -u Administrator -p password --node-init-data-path=/data/couchbase");

            if (String.IsNullOrEmpty(clusterIpAddress))
            {
                var dataRamSize = 256;
                var indexRamSize = 256;

                if (instanceInfo.Memory > 6)
                {
                    dataRamSize = ((int)instanceInfo.Memory - 4) * 1000;
                    indexRamSize = 2 * 1000;
                }

                items.Add($"/opt/couchbase/bin/couchbase-cli cluster-init -c localhost --cluster-username={adminUsername} --cluster-password={adminPassword} --cluster-ramsize={dataRamSize} --cluster-index-ramsize={indexRamSize} --services=data,index");
            }
            else
            {
                //Wait for master
                items.Add($"until $(curl --output /dev/null --silent --head --fail http://{clusterIpAddress}:8091); do");
                items.Add("  printf '.'");
                items.Add("  sleep 5");
                items.Add("done");

                items.Add("serverip=`/sbin/ifconfig eth0 | grep \"inet\" | awk '{print $2}' | awk 'NR==1' | cut -d':' -f2`");
                //items.Add("echo $serverip");
                items.Add($"/opt/couchbase/bin/couchbase-cli server-add -c {clusterIpAddress} -u {adminUsername} -p {adminPassword} --server-add=$serverip --server-add-username=Administrator --server-add-password=password --services=data,index");
                items.Add($"/opt/couchbase/bin/couchbase-cli rebalance -c {clusterIpAddress} -u {adminUsername} -p {adminPassword}");
            }

            return items;
        }
    }
}