﻿using System.Threading;
using System.Threading.Tasks;
using Forge.Networking.Players;

namespace Forge.Networking.Sockets
{
	public class ForgeUDPSocketServerContainer : ForgeUDPSocketContainerBase, ISocketServerContainer
	{
		private const int MAX_PARALLEL_CONNECTION_REQUEST = 64;

		private readonly IServerSocket _socket;
		public override ISocket ManagedSocket => _socket;
		private CancellationTokenSource _newConnectionsTokenSource;

		public ForgeUDPSocketServerContainer()
		{
			_socket = ForgeTypeFactory.GetNew<IServerSocket>();
		}

		public void StartServer(ushort port, int maxPlayers, INetworkContainer netContainer)
		{
			// TODO:  Use maxPlayers
			this.netContainer = netContainer;
			_socket.Listen(port, MAX_PARALLEL_CONNECTION_REQUEST);
			_newConnectionsTokenSource = new CancellationTokenSource();
			readTokenSource = new CancellationTokenSource();
			Task.Run(ListenForConnections, _newConnectionsTokenSource.Token);
			Task.Run(ReadNetwork, readTokenSource.Token);
		}

		public override void ShutDown()
		{
			_newConnectionsTokenSource.Cancel();
			base.ShutDown();
		}

		private void ListenForConnections()
		{
			while (!_newConnectionsTokenSource.Token.IsCancellationRequested)
			{
				ISocket newClient = _socket.AwaitAccept();
				synchronizationContext.Post(SynchronizedPlayerConnected, newClient);
			}
		}

		private void SynchronizedPlayerConnected(object state)
		{
			var newClient = (ISocket)state;
			if (!netContainer.PlayerRepository.Exists(newClient.EndPoint))
			{
				var newPlayer = ForgeTypeFactory.GetNew<INetPlayer>();
				newPlayer.Socket = newClient;
				netContainer.PlayerRepository.AddPlayer(newPlayer);
				netContainer.EngineContainer.PlayerJoined(newPlayer);
			}
		}
	}
}