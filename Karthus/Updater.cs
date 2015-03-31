using LeagueSharp.Common;
using System;
using System.Reflection;

namespace Karthus
{
    internal class Updater
    {
        private static readonly System.Version Version = Assembly.GetExecutingAssembly().GetName().Version;
        public static void Init(string path)
        {
            try
            {
                var data = new BetterWebClient(null).DownloadString("https://raw.github.com/" + path + "/Properties/AssemblyInfo.cs");
                foreach (var line in data.Split('\n'))
                {
                    if (line.StartsWith("//"))
                    {
                        continue;
                    }

                    if (line.StartsWith("[assembly: AssemblyVersion"))
                    {
                        var serverVersion = new System.Version(line.Substring(28, (line.Length - 4) - 28 + 1));
                        if (serverVersion > Version)
                        {
                            LeagueSharp.Game.PrintChat("<font color='#E62E00'>Update available: </font>" + Version + " => " + serverVersion);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            LeagueSharp.Game.PrintChat("<font color='#008AE6'>No update available: </font>" + Version);
        }
    }
}