// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace LPRunner;

internal class DockerRunner
{
    private const string ExposedPort = "Port1";

    public static DockerRunner? Create(ILogger<RunnerMachine> logger, RunnerInformation information)
    {
        var client = new DockerClientConfiguration().CreateClient();
        try
        {
            var result = client.Containers.ListContainersAsync(new() { Limit = 10, }).Result;
        }
        catch
        {// No docker
            return null;
        }

        return new DockerRunner(client, logger, information);
    }

    private DockerRunner(DockerClient client, ILogger<RunnerMachine> logger, RunnerInformation information)
    {
        this.client = client;
        this.logger = logger;
        this.information = information;
    }

    public async Task<IEnumerable<ContainerListResponse>> EnumerateContainersAsync()
    {
        var list = await this.client.Containers.ListContainersAsync(new() { Limit = 100, });
        return list.Where(x => x.Image.StartsWith(this.information.Image));
    }

    public async Task RemoveContainer()
    {// Remove containers
        var array = (await this.EnumerateContainersAsync()).ToArray();
        foreach (var x in array)
        {
            try
            {
                await this.client.Containers.StopContainerAsync(x.ID, new());
            }
            catch
            {
            }

            try
            {
                await this.client.Containers.RemoveContainerAsync(x.ID, new());
                this.logger.TryGet()?.Log($"Container removed: {x.ID}");
                // this.logger.TryGet()?.Log("Success");
            }
            catch
            {
                // this.logger.TryGet()?.Log("Failure");
            }
        }
    }

    public async Task<bool> RunContainer()
    {
        // Create image
        this.logger.TryGet()?.Log($"Create image: {this.information.Image}");
        var progress = new Progress<JSONMessage>();
        try
        {
            await this.client.Images.CreateImageAsync(
                new ImagesCreateParameters
                {
                    FromImage = this.information.Image,
                    Tag = this.information.Tag,
                },
                null,
                progress);

            this.logger.TryGet()?.Log("Success");
        }
        catch
        {
            this.logger.TryGet()?.Log("Failure");
            return false;
        }

        // Create container
        this.logger.TryGet()?.Log($"Start container: {this.information.Image}");
        // RunnerHelper.DispatchCommand(this.logger, "docker run -it --mount type=bind,source=$(pwd)/lp,destination=/lp --rm -p 49152:49152/udp archidoc422/lpconsole -rootdir \"/lp\" -ns [-port 49152 -test true -alternative false]"); // C:\\App\\docker

        string directory;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            directory = "C:\\App\\docker";
        }
        else
        {
            directory = $"$(pwd)/{this.information.HostDirectory}";
        }

        var nodename = string.IsNullOrEmpty(this.information.NodeName) ? string.Empty : $"-name {this.information.NodeName}";

        var command = $"docker run -d -it --mount type=bind,source={directory},destination={this.information.DestinationDirectory} --rm -p {this.information.HostPort}:{this.information.DestinationPort}/udp {this.information.Image} -rootdir {this.information.DestinationDirectory} {nodename} -ns [-port {this.information.DestinationPort} {this.information.NetsphereOptions}] -remotekey {this.information.RemotePublicKey.ToBase64()} {this.information.AdditionalArgs}";
        RunnerHelper.DispatchCommand(this.logger, command);

        /*try
        {
        // var exposedPort = this.information.DestinationPort.ToString() + "/udp";
            var containerResponse = await this.client.Containers.CreateContainerAsync(new()
            {// docker run -it --mount type=bind,source=$(pwd)/lp,destination=/lp --rm -p 49152:49152/udp
                Image = this.information.Image,
                // WorkingDir = "c:\\app\\docker", // this.information.Directory,
                AttachStdin = true,
                AttachStderr = true,
                AttachStdout = true,
                Tty = true,
                Cmd = new[] { "-rootdir \"/lp\" -ns [-port 49152 -test true -alternative false]" },
                ExposedPorts = new Dictionary<string, EmptyStruct> { { exposedPort, default } },
                HostConfig = new HostConfig
                {
                    Mounts = new Mount[]
                    {
                        // new Mount() { Type = "bind", Source = "/home/ubuntu/lp", Target = "/lp", },
                        new Mount() { Type = "bind", Source = "C:\\App\\docker", Target = "/lp", },
                    },

                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { exposedPort, new List<PortBinding> { new PortBinding { HostIP = "localhost", HostPort = this.information.TargetPort.ToString() + "/udp" } } },
                    },
                },
            });

            await this.client.Containers.StartContainerAsync(containerResponse.ID, new());
            this.logger.TryGet()?.Log($"Success: {containerResponse.ID}");
        }
        catch
        {
            this.logger.TryGet()?.Log("Failure");
            return false;
        }*/

        return true;
    }

    public async Task RestartContainer()
    {
        var array = (await this.EnumerateContainersAsync()).ToArray();
        foreach (var x in array)
        {
            if (x.State == "created" || x.State == "exited")
            {
                this.logger.TryGet()?.Log($"Restart container: {x.ID}");
                try
                {
                    await this.client.Containers.StartContainerAsync(x.ID, new());
                }
                catch
                {
                }
            }
        }
    }

    private DockerClient client;
    private ILogger<RunnerMachine> logger;
    private RunnerInformation information;
}
