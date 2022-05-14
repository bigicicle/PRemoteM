﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using PRM.Model;
using PRM.Model.Protocol;
using PRM.Model.Protocol.Base;
using PRM.Model.ProtocolRunner;
using PRM.Model.ProtocolRunner.Default;
using PRM.Utils.KiTTY;
using PRM.View.Host.ProtocolHosts;
using Shawn.Utils;

namespace PRM.Utils
{
    public static class ProtocolRunnerHostHelper
    {
        /// <summary>
        /// get a selected runner, or default runner. some protocol i.e. 'APP' will return null.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="protocolName"></param>
        /// <param name="assignRunnerName"></param>
        /// <returns></returns>
        public static Runner GetRunner(PrmContext context, string protocolName, string? assignRunnerName = null)
        {
            if (context.ProtocolConfigurationService.ProtocolConfigs.ContainsKey(protocolName) == false)
            {
                SimpleLogHelper.Fatal($"we don't have a protocol named: {protocolName}");
                return new InternalDefaultRunner(protocolName);
            }

            var p = context.ProtocolConfigurationService.ProtocolConfigs[protocolName];
            if (p.Runners.Count == 0)
            {
                SimpleLogHelper.Warning($"{protocolName} does not have any runner!");
                return new InternalDefaultRunner(protocolName);
            }

            var runnerName = assignRunnerName;
            if (string.IsNullOrEmpty(runnerName))
                runnerName = p.SelectedRunnerName;
            var r = p.Runners.FirstOrDefault(x => x.Name == runnerName);
            if (r == null)
            {
                SimpleLogHelper.Fatal($"{protocolName} does not have a runner name == the selection '{p.SelectedRunnerName}'!");
                r = p.Runners.FirstOrDefault();
            }

            return r ?? new InternalDefaultRunner(protocolName);
        }

        /// <summary>
        /// get a host for the runner if RunWithHosting == true, or start the runner if RunWithHosting == false.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="protocolServerBase"></param>
        /// <param name="runner"></param>
        /// <returns></returns>
        public static HostBase? GetHostOrRunDirectlyForExternalRunner<T>(PrmContext context, T protocolServerBase, Runner runner) where T : ProtocolBase
        {
            if (runner is not ExternalRunner er) return null;

            var exePath = er.ExePath;
            var args = er.Arguments;
            if (!File.Exists(exePath))
            {
                MessageBoxHelper.ErrorAlert($"Exe file '{er.ExePath}' of runner '{er.Name}' does not existed!");
                return null;
            }


            // make exeArguments and environment variables
            var exeArguments = "";
            var environmentVariables = new Dictionary<string, string>();
            {
                exeArguments = OtherNameAttributeExtensions.Replace(protocolServerBase, args);
                foreach (var kv in er.EnvironmentVariables)
                {
                    environmentVariables.Add(kv.Key, OtherNameAttributeExtensions.Replace(protocolServerBase, kv.Value));
                }
            }

            // start process
            if (er.RunWithHosting)
            {
                var integrateHost = new IntegrateHost(protocolServerBase, exePath, exeArguments, environmentVariables);
                return integrateHost;
            }
            else
            {
                var startInfo = new ProcessStartInfo();
                if (environmentVariables?.Count > 0)
                    foreach (var kv in environmentVariables)
                    {
                        if (startInfo.EnvironmentVariables.ContainsKey(kv.Key) == false)
                            startInfo.EnvironmentVariables.Add(kv.Key, kv.Value);
                        startInfo.EnvironmentVariables[kv.Key] = kv.Value;
                    }
                startInfo.UseShellExecute = false;
                startInfo.FileName = exePath;
                startInfo.Arguments = exeArguments;
                var process = new Process() { StartInfo = startInfo };
                process.Start();
                return null;
            }

        }

        /// <summary>
        /// get host for a ProtocolBase, can return null
        /// </summary>
        /// <param name="context"></param>
        /// <param name="server"></param>
        /// <param name="runner">指定使用哪个 runner 来启动 session，可以为空</param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static HostBase? GetHostForInternalRunner(PrmContext context, ProtocolBase server, Runner runner, double width = 0, double height = 0)
        {
            Debug.Assert(runner is InternalDefaultRunner);
            switch (server)
            {
                case RDP rdp:
                    {
                        var host = new AxMsRdpClient09Host(rdp, (int)width, (int)height);
                        return host;
                    }
                case SSH ssh:
                    {
                        Debug.Assert(runner is KittyRunner);
                        var kitty = (KittyRunner)runner;
                        ssh.InstallKitty();
                        var host = new IntegrateHost(ssh, ssh.GetExeFullPath(), ssh.GetExeArguments(context))
                        {
                            RunBeforeConnect = () => ssh.SetKittySessionConfig(kitty.GetPuttyFontSize(), kitty.GetPuttyThemeName(), ssh.PrivateKey),
                            RunAfterConnected = () => ssh.DelKittySessionConfig()
                        };
                        return host;
                    }
                case Telnet telnet:
                    {
                        Debug.Assert(runner is KittyRunner);
                        var kitty = (KittyRunner)runner;
                        telnet.InstallKitty();
                        var host = new IntegrateHost(telnet, telnet.GetExeFullPath(), telnet.GetExeArguments(context))
                        {
                            RunBeforeConnect = () => telnet.SetKittySessionConfig(kitty.GetPuttyFontSize(), kitty.GetPuttyThemeName(), ""),
                            RunAfterConnected = () => telnet.DelKittySessionConfig()
                        };
                        return host;
                    }
                case VNC vnc:
                    {
                        return new VncHost(vnc);
                    }
                case SFTP sftp:
                    {
                        return new FileTransmitHost(sftp);
                    }
                case FTP ftp:
                    {
                        return new FileTransmitHost(ftp);
                    }
                case LocalApp app:
                    {
                        if (File.Exists(app.ExePath) == false)
                        {
                            MessageBoxHelper.ErrorAlert($"the path '{app.ExePath}' does not existed!");
                            return null;
                        }

                        if (app.RunWithHosting)
                        {
                            var host = new IntegrateHost(app, app.ExePath, app.Arguments);
                            return host;
                        }
                        else
                        {
                            Process.Start(app.ExePath, app.Arguments);
                        }
                        return null;
                    }
                default:
                    throw new NotImplementedException($"Host of {server.GetType()} is not implemented");
            }
        }
    }
}