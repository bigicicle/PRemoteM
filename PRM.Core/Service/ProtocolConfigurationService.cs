﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorPickerWPF.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PRM.Core.Protocol;
using PRM.Core.Protocol.FileTransmit.FTP;
using PRM.Core.Protocol.FileTransmit.SFTP;
using PRM.Core.Protocol.Putty.SSH;
using PRM.Core.Protocol.Putty.Telnet;
using PRM.Core.Protocol.RDP;
using PRM.Core.Protocol.Runner;
using PRM.Core.Protocol.Runner.Default;
using PRM.Core.Protocol.VNC;
using Shawn.Utils;

namespace PRM.Core.Service
{
    public class ProtocolConfigurationService
    {
        public Dictionary<string, ProtocolConfig> ProtocolConfigs { get; set; } = new Dictionary<string, ProtocolConfig>();
        public string[] CustomProtocolBlackList => new string[] { "SSH", "RDP", "VNC", "TELNET", "FTP", "SFTP", "RemoteApp", "APP" };

        public readonly string ProtocolFolderName;

        public ProtocolConfigurationService()
        {
            // TODO 绿色版和安装版使用不同的路径，日志系统也需如此修改
            var appDateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ConfigurationService.AppName);
            ProtocolFolderName = Path.Combine(appDateFolder, "Protocols");
            if (Directory.Exists(ProtocolFolderName) == false)
                Directory.CreateDirectory(ProtocolFolderName);
            Load();
        }


        private void Load()
        {
            ProtocolConfigs.Clear();
            var di = new DirectoryInfo(ProtocolFolderName);

            // build-in protocol
            //LoadRdp();
            LoadVnc();
            LoadSsh();
            LoadTelnet();
            LoadSftp();
            LoadFtp();


            // custom protocol
            {
                var customs = new Dictionary<string, ProtocolConfig>();
                foreach (var directoryInfo in di.GetDirectories())
                {
                    var protocolName = directoryInfo.Name;
                    if (ProtocolConfigs.ContainsKey(protocolName))
                        continue;

                    var c = LoadConfig(protocolName);
                    if (c != null)
                    {
                        customs.Add(protocolName, c);
                    }
                }

                // remove special protocol
                foreach (var name in CustomProtocolBlackList)
                {
                    if (customs.Any(kv => String.Equals(kv.Key, name, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        customs.Remove(name);
                    }
                }

                foreach (var custom in customs)
                {
                    ProtocolConfigs.Add(custom.Key, custom.Value);
                }
            }


            // ExternalRunner + macros
            foreach (var config in ProtocolConfigs)
            {
                foreach (var runner in config.Value.Runners)
                {
                    if (runner is ExternalRunner er)
                    {
                        er.MarcoNames = config.Value.MarcoNames;
                        er.ProtocolType = config.Value.ProtocolType;
                    }
                }
            }
        }


        public ProtocolConfig LoadConfig(string protocolName)
        {
            protocolName = protocolName.ToUpper();
            var file = Path.Combine(ProtocolFolderName, $"{protocolName}.json");
            if (File.Exists(file))
            {
                var jsonStr = File.ReadAllText(file, Encoding.UTF8);
                var jobj = JObject.Parse(jsonStr);
                var runners = jobj[nameof(ProtocolConfig.Runners)] as JArray;
                jobj.Remove(nameof(ProtocolConfig.Runners));
                var serializer = new JsonSerializer();
                var c = (ProtocolConfig)serializer.Deserialize(new JTokenReader(jobj), typeof(ProtocolConfig));

                if (runners != null)
                    foreach (var runner in runners)
                    {
                        try
                        {
                            var r = JsonConvert.DeserializeObject<Runner>(runner.ToString());
                            if (r != null)
                                c.Runners.Add(r);

                        }
                        catch (Exception e)
                        {
                            Console.Write(e);
                        }
                    }

                if (c != null)
                    return c;
            }

            return null;
        }

        private void LoadVnc()
        {
            var t = typeof(ProtocolServerVNC);
            var protocolName = ProtocolServerVNC.ProtocolName;
            var macros = OtherNameAttributeExtensions.GetOtherNames(t);
            var c = LoadConfig(protocolName) ?? new ProtocolConfig();
            c.Init(macros.Select(x => x.Value).ToList(), macros.Select(x => x.Key).ToList(), t);
            c.Runners ??= new List<Runner>();
            if (c.Runners.Count == 0 || c.Runners[0] is InternalDefaultRunner == false)
            {
                c.Runners.RemoveAll(x => x is InternalDefaultRunner);
                c.Runners.Insert(0, new InternalDefaultRunner());
            }

            c.Runners.First(x => x is InternalDefaultRunner).Name = $"Internal {protocolName}";
            ProtocolConfigs.Add(protocolName, c);
        }
        private void LoadSsh()
        {
            var t = typeof(ProtocolServerSSH);
            var protocolName = ProtocolServerSSH.ProtocolName;
            var macros = OtherNameAttributeExtensions.GetOtherNames(t);
            var c = LoadConfig(protocolName) ?? new ProtocolConfig();
            c.Init(macros.Select(x => x.Value).ToList(), macros.Select(x => x.Key).ToList(), t);
            c.Runners ??= new List<Runner>();
            if (c.Runners.Count == 0 || c.Runners[0] is KittyRunner == false)
            {
                c.Runners.RemoveAll(x => x is KittyRunner);
                c.Runners.Insert(0, new KittyRunner());
            }

            c.Runners.First(x => x is KittyRunner).Name = $"Internal KiTTY";
            ProtocolConfigs.Add(protocolName, c);
        }
        private void LoadTelnet()
        {
            var t = typeof(ProtocolServerTelnet);
            var protocolName = ProtocolServerTelnet.ProtocolName;
            var macros = OtherNameAttributeExtensions.GetOtherNames(t);
            var c = LoadConfig(protocolName) ?? new ProtocolConfig();
            c.Init(macros.Select(x => x.Value).ToList(), macros.Select(x => x.Key).ToList(), t);
            c.Runners ??= new List<Runner>();
            if (c.Runners.Count == 0 || c.Runners[0] is KittyRunner == false)
            {
                c.Runners.RemoveAll(x => x is KittyRunner);
                c.Runners.Insert(0, new KittyRunner());
            }

            c.Runners.First(x => x is KittyRunner).Name = $"Internal KiTTY";
            ProtocolConfigs.Add(protocolName, c);
        }

        private void LoadSftp()
        {
            var t = typeof(ProtocolServerSFTP);
            var protocolName = ProtocolServerSFTP.ProtocolName;
            var macros = OtherNameAttributeExtensions.GetOtherNames(t);
            var c = LoadConfig(protocolName) ?? new ProtocolConfig();
            c.Init(macros.Select(x => x.Value).ToList(), macros.Select(x => x.Key).ToList(), t);
            c.Runners ??= new List<Runner>();
            if (c.Runners.Count == 0 || c.Runners[0] is InternalDefaultRunner == false)
            {
                c.Runners.RemoveAll(x => x is InternalDefaultRunner);
                c.Runners.Insert(0, new InternalDefaultRunner());
            }

            c.Runners.First(x => x is InternalDefaultRunner).Name = $"Internal {protocolName}";
            ProtocolConfigs.Add(protocolName, c);
        }

        private void LoadFtp()
        {
            var t = typeof(ProtocolServerFTP);
            var protocolName = ProtocolServerFTP.ProtocolName;
            var macros = OtherNameAttributeExtensions.GetOtherNames(t);
            var c = LoadConfig(protocolName) ?? new ProtocolConfig();
            c.Init(macros.Select(x => x.Value).ToList(), macros.Select(x => x.Key).ToList(), t);
            c.Runners ??= new List<Runner>();
            if (c.Runners.Count == 0 || c.Runners[0] is InternalDefaultRunner == false)
            {
                c.Runners.RemoveAll(x => x is InternalDefaultRunner);
                c.Runners.Insert(0, new InternalDefaultRunner());
            }

            c.Runners.First(x => x is InternalDefaultRunner).Name = $"Internal {protocolName}";
            ProtocolConfigs.Add(protocolName, c);
        }

        public bool Check()
        {
            return true;
        }

        public void Save()
        {
            foreach (var kv in ProtocolConfigs)
            {
                var protocolName = kv.Key;
                var config = kv.Value;
                foreach (var runner in config.Runners)
                {
                    if (runner is ExternalRunner er)
                    {
                        foreach (var ev in er.EnvironmentVariables.ToArray())
                        {
                            if (ev.Key == "")
                                er.EnvironmentVariables.Remove(ev);
                        }
                    }
                }
                var file = Path.Combine(ProtocolFolderName, $"{protocolName}.json");
                File.WriteAllText(file, JsonConvert.SerializeObject(config, Formatting.Indented), Encoding.UTF8);
            }
        }
    }
}