﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace InputshareLib.Server
{
    internal class ClientManager
    {
        /// <summary>
        /// Max clients allowed by the server
        /// </summary>
        public int MaxClients { get; private set; }
        /// <summary>
        /// Current ammount of clients connected to the server (including localhost)
        /// </summary>
        public int ClientCount { get => clients.Count; }

        /// <summary>
        /// Returns a read only list of all connected clients
        /// 
        /// </summary>
        public ReadOnlyCollection<ISServerSocket> AllClients { get => new ReadOnlyCollection<ISServerSocket>(clients);}
        private List<ISServerSocket> clients;

        public ClientManager(int maxClients)
        {
            MaxClients = maxClients;
            clients = new List<ISServerSocket>(maxClients);
        }

        /// <summary>
        /// Adds a client to the clientlist
        /// </summary>
        /// <exception cref="ClientLimitException"></exception>
        /// <exception cref="DuplicateNameException"></exception>
        /// <exception cref="DuplicateGuidException"></exception>
        /// <param name="client"></param>
        public void AddClient(ISServerSocket client)
        {
            if (ClientCount == MaxClients)
                throw new ClientLimitException();

            if (GetClientByName(client.ClientName) != null)
                throw new DuplicateNameException();

            if (GetClientById(client.ClientId) != null)
                throw new DuplicateGuidException();

            clients.Add(client);
        }

        /// <summary>
        /// Removes a client from the clientlist
        /// </summary>
        /// <param name="client"></param>
        public void RemoveClient(ISServerSocket client)
        {
            clients.Remove(client);
        }

        /// <summary>
        /// Returns the client identified by the ClientInfo
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public ISServerSocket GetClientFromInfo(ClientInfo info)
        {
            return GetClientById(info.Id);
        }

        /// <summary>
        /// Returns a client with a matching name (NULL IF NONE)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ISServerSocket GetClientByName(string name)
        {
            return clients.Where(item => item.ClientName == name).FirstOrDefault();
        }
        /// <summary>
        /// Returns a client with a matching guid (NULL IF NONE)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ISServerSocket GetClientById(Guid id)
        {
            return clients.Where(item => item.ClientId == id).FirstOrDefault();
        }

        /// <summary>
        /// Gets the name of a client based off the GUID (NULL if guid not found)
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public string GetClientName(Guid clientId)
        {
            if (GetClientById(clientId) != null)
                return GetClientById(clientId).ClientName;

            return null;
        }

        public class DuplicateGuidException : Exception
        {

        }

        public class DuplicateNameException : Exception
        {

        }

        public class ClientLimitException : Exception
        {

        }
    }
}
