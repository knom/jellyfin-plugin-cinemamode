using System;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.CinemaMode
{
    public class PluginRegistrator : IPluginServiceRegistrator
    {
        public PluginRegistrator()
        {
        }

        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddSingleton<IChannel, TrailerChannel>();
        }
    }
}
