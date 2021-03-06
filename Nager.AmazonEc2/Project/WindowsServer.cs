﻿using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using log4net;
using Nager.AmazonEc2.Helper;
using Nager.AmazonEc2.InstallScript;
using Nager.AmazonEc2.Model;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Nager.AmazonEc2.Project
{
    public class WindowsServer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsServer));
        private AmazonEC2Client _client;

        public string CreateSecurityGroup(string name, List<int> ports)
        {
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

            var ipPermissionRdp = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 3389,
                ToPort = 3389,
                IpRanges = new List<string>() { "0.0.0.0/0" }
            };

            var ingressRequest = new AuthorizeSecurityGroupIngressRequest();
            ingressRequest.GroupId = createSecurityGroupResponse.GroupId;
            ingressRequest.IpPermissions = new List<IpPermission>();
            ingressRequest.IpPermissions.Add(ipPermissionRdp);

            if (ports != null)
            {
                foreach (var port in ports)
                {
                    var ipPermission = new IpPermission()
                    {
                        IpProtocol = "tcp",
                        FromPort = port,
                        ToPort = port,
                        IpRanges = new List<string>() { "0.0.0.0/0" }
                    };

                    ingressRequest.IpPermissions.Add(ipPermission);
                }
            }

            var ingressResponse = this._client.AuthorizeSecurityGroupIngress(ingressRequest);
            if (ingressResponse.HttpStatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            return createSecurityGroupResponse.GroupId;
        }

        public WindowsServer(AmazonAccessKey accessKey, RegionEndpoint regionEnpoint)
        {
            this._client = new AmazonEC2Client(accessKey.AccessKeyId, accessKey.SecretKey, regionEnpoint);
        }

        public string GetImageId(WindowsVersion windowsVersion)
        {
            var filterOwner = new Filter("owner-id", new List<string> { "801119661308" }); //amazon
            var filterPlatform = new Filter("platform", new List<string> { "windows" });

            Filter filterName;
            switch (windowsVersion)
            {
                case WindowsVersion.V2012:
                    filterName = new Filter("name", new List<string> { "Windows_Server-2012-R2_RTM-English-64Bit-Base*" });
                    break;
                case WindowsVersion.V2016:
                    filterName = new Filter("name", new List<string> { "Windows_Server-2016-English-Full-Base*" });
                    break;
                default:
                    return null;
            }

            var describeImagesRequest = new DescribeImagesRequest() { Filters = new List<Filter>() { filterOwner, filterPlatform, filterName } };
            var response = this._client.DescribeImages(describeImagesRequest);
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                Log.Error("GetImageId - Cannot get the ami id");
            }

            return response.Images?.OrderByDescending(o => o.CreationDate).Select(o => o.ImageId).FirstOrDefault();
        }

        public InstallResult Install(AmazonInstance amazonInstance, string name, string securityGroupId, string keyName, IInstallScript installScript, WindowsVersion windowsVersion)
        {
            var imageId = this.GetImageId(windowsVersion);
            if (imageId == null)
            {
                Log.Error("Install - imageId is null");
                return new InstallResult() { Successful = false };
            }

            var instanceInfo = InstanceInfoHelper.GetInstanceInfo(amazonInstance);

            var instanceRequest = new RunInstancesRequest();
            instanceRequest.ImageId = imageId;
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
                        VolumeSize = 30
                    }
                };

                instanceRequest.BlockDeviceMappings.Add(blockDeviceMappingSystem);
            }

            //Install Process can check in this log file
            //Windows 2012
            //<C:\Program Files\Amazon\Ec2ConfigService\Logs\Ec2ConfigLog.txt>
            //Windows 2016
            //<C:\ProgramData\Amazon\EC2-Windows\Launch\Log\UserdataExecution.log>
            instanceRequest.UserData = installScript.Create();

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
    }
}