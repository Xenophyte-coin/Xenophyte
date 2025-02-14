﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeguraChain_Lib.Instance.Node.Network.Database;
using SeguraChain_Lib.Instance.Node.Network.Database.Manager;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Broadcast;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.Model;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.SubPacket.Request;
using SeguraChain_Lib.Instance.Node.Setting.Object;
using SeguraChain_Lib.Log;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Other.Object.Network;
using SeguraChain_Lib.Utility;
using static SeguraChain_Lib.Other.Object.Network.ClassCustomSocket;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.ClientSync.ClientConnect.Object
{
    public class ClassPeerNetworkClientSyncObject : IDisposable
    {
        /// <summary>
        /// Tcp info and tcp client object.
        /// </summary>
        private ClassCustomSocket _peerSocketClient;
        public bool PeerConnectStatus => _peerSocketClient != null ? _peerSocketClient.IsConnected() : false;

        /// <summary>
        /// Peer informations.
        /// </summary>
        public string PeerIpTarget;
        public int PeerPortTarget;
        public int PeerApiPortTarget;
        public string PeerUniqueIdTarget;
     

        /// <summary>
        /// Packet received.
        /// </summary>
        public ClassPeerPacketRecvObject PeerPacketReceived;
        public ClassPeerEnumPacketResponse PeerPacketTypeReceived;
        public bool PeerPacketReceivedStatus;

        /// <summary>
        /// Peer task status.
        /// </summary>
        public bool PeerTaskStatus;
        private bool _peerTaskKeepAliveStatus;
        private CancellationTokenSource _peerCancellationTokenMain;
        private CancellationTokenSource _peerCancellationTokenKeepAlive;

        /// <summary>
        /// Peer Database.
        /// </summary>
        private ClassPeerDatabase _peerDatabase;

        /// <summary>
        /// Network settings.
        /// </summary>
        private ClassPeerNetworkSettingObject _peerNetworkSetting;
        private ClassPeerFirewallSettingObject _peerFirewallSettingObject;

        /// <summary>
        /// Specifications of the connection opened.
        /// </summary>
        public ClassPeerEnumPacketResponse PacketResponseExpected;
        private DisposableList<ClassReadPacketSplitted> listPacketReceived;
        private bool _keepAlive;


        #region Dispose functions

        ~ClassPeerNetworkClientSyncObject()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            PeerTaskStatus = false;
            CleanUpTask();
            DisconnectFromTarget();
        }

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="peerIpTarget"></param>
        /// <param name="peerPort"></param>
        /// <param name="peerUniqueId"></param>
        /// <param name="peerNetworkSetting"></param>
        /// <param name="peerFirewallSettingObject"></param>
        public ClassPeerNetworkClientSyncObject(ClassPeerDatabase peerDatabase, string peerIpTarget, int peerPort, string peerUniqueId, ClassPeerNetworkSettingObject peerNetworkSetting, ClassPeerFirewallSettingObject peerFirewallSettingObject)
        {
            _peerDatabase = peerDatabase;
            PeerIpTarget = peerIpTarget;
            PeerPortTarget = peerPort;
            PeerUniqueIdTarget = peerUniqueId;
            _peerNetworkSetting = peerNetworkSetting;
            _peerFirewallSettingObject = peerFirewallSettingObject;
        }

        /// <summary>
        /// Attempt to send a packet to a peer target.
        /// </summary>
        /// <param name="packetSendObject"></param>
        /// <param name="cancellation"></param>
        /// <param name="packetResponseExpected"></param>
        /// <param name="keepAlive"></param>
        /// <param name="broadcast"></param>
        /// <returns></returns>
        public async Task<bool> TrySendPacketToPeerTarget(ClassPeerPacketSendObject packetSendObject, bool toSignAndEncrypt, int peerPort, string peerUniqueId, CancellationTokenSource cancellation, ClassPeerEnumPacketResponse packetResponseExpected, bool keepAlive, bool broadcast)
        {

            #region Clean up and cancel previous task.

            CleanUpTask();

            #endregion

            _peerCancellationTokenMain = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);

            if (toSignAndEncrypt)
            {
                packetSendObject = await ClassPeerNetworkBroadcastShortcutFunction.BuildSignedPeerSendPacketObject(_peerDatabase, packetSendObject, PeerIpTarget, peerUniqueId, false, _peerNetworkSetting, _peerCancellationTokenMain);

                if (packetSendObject == null ||
                    packetSendObject.PacketContent.IsNullOrEmpty(false, out _) ||
                    packetSendObject.PacketHash.IsNullOrEmpty(false, out _) ||
                    packetSendObject.PacketSignature.IsNullOrEmpty(false, out _) ||
                    packetSendObject.PublicKey.IsNullOrEmpty(false, out _))
                {
#if DEBUG
                    Debug.WriteLine("Failed, to sign packet data target " + PeerIpTarget + " | Type: " + System.Enum.GetName(typeof(ClassPeerEnumPacketSend), PacketResponseExpected));
#endif
                    return false;
                }
            }


            byte[] packetData = packetSendObject.GetPacketData();

            if (packetData == null)
            {
#if DEBUG
                Debug.WriteLine("Failed, packet data empty target" + PeerIpTarget + " | Type: " + System.Enum.GetName(typeof(ClassPeerEnumPacketSend), packetSendObject.PacketOrder));
#endif
                return false;
            }
            #region Init the client sync object.

            PacketResponseExpected = packetResponseExpected;
            _keepAlive = keepAlive;
            PeerPortTarget = peerPort;
            PeerUniqueIdTarget = peerUniqueId;

            #endregion

            #region Check the current connection status opened to the target.

            if (!PeerConnectStatus || !keepAlive)
            {
                DisconnectFromTarget();


                if (!await DoConnection(_peerCancellationTokenMain))
                {
#if DEBUG
                    Debug.WriteLine("Failed to connect to peer " + PeerIpTarget + ":" + PeerPortTarget);
#endif
                    ClassLog.WriteLine("Failed to connect to peer " + PeerIpTarget + ":" + PeerPortTarget, ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY, true);
                    ClassPeerCheckManager.InputPeerClientAttemptConnect(_peerDatabase, PeerIpTarget, PeerUniqueIdTarget, _peerNetworkSetting, _peerFirewallSettingObject, _peerCancellationTokenMain);
                    DisconnectFromTarget();
                    return false;
                }
            }

            #endregion

            #region Send packet and wait packet response.


            if (!await SendPeerPacket(packetData, _peerCancellationTokenMain))
            {
#if DEBUG
                Debug.WriteLine("Failed to send packet data to " + PeerIpTarget + ":" + PeerPortTarget);
#endif
                ClassPeerCheckManager.InputPeerClientNoPacketConnectionOpened(_peerDatabase, PeerIpTarget, PeerUniqueIdTarget, _peerNetworkSetting, _peerFirewallSettingObject, _peerCancellationTokenMain);
                DisconnectFromTarget();
                return false;
            }
            else
                return broadcast ? true : await WaitPacketExpected();

            #endregion

        }

        #region Initialize connection functions.

        /// <summary>
        /// Clean up the task.
        /// </summary>
        private void CleanUpTask()
        {
            PeerPacketTypeReceived = ClassPeerEnumPacketResponse.NONE;
            PeerPacketReceived = null;
            PeerPacketReceivedStatus = false;
            CancelKeepAlive();
            try
            {
                if (_peerCancellationTokenMain != null)
                {
                    if (!_peerCancellationTokenMain.IsCancellationRequested)
                        _peerCancellationTokenMain.Cancel();
                }
            }
            catch
            {
                // Ignored, if already cancelled.
            }
        }


        /// <summary>
        /// Do connection.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> DoConnection(CancellationTokenSource cancellation)
        {
            _peerSocketClient = new ClassCustomSocket(new Socket(ClassUtility.GetAddressFamily(PeerIpTarget), SocketType.Stream, ProtocolType.Tcp), false);

            if (await _peerSocketClient.Connect(PeerIpTarget, PeerPortTarget, _peerNetworkSetting.PeerMaxDelayToConnectToTarget, cancellation))
                return true;
            else _peerSocketClient?.Kill(SocketShutdown.Both);

            await Task.Delay(10);

            return false;
        }


        /// <summary>
        /// Wait the packet expected.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> WaitPacketExpected()
        {
            CancelHandlePacket();


            PeerTaskStatus = true;

            await TaskWaitPeerPacketResponse();

            if (PeerPacketReceived == null)
            {
#if DEBUG
                Debug.WriteLine("Peer " + PeerIpTarget + "|" + PeerUniqueIdTarget + " don't send a response to the packet sent: " + System.Enum.GetName(typeof(ClassPeerEnumPacketResponse), PacketResponseExpected)+" | " + System.Enum.GetName(typeof(ClassPeerEnumPacketResponse), PeerPacketTypeReceived));
#endif
                ClassLog.WriteLine("Peer " + PeerIpTarget + "|" + PeerUniqueIdTarget + " don't send a response to the packet sent: " + System.Enum.GetName(typeof(ClassPeerEnumPacketResponse), PacketResponseExpected) + " | " + System.Enum.GetName(typeof(ClassPeerEnumPacketResponse), PeerPacketTypeReceived), ClassEnumLogLevelType.LOG_LEVEL_PEER_TASK_SYNC, ClassEnumLogWriteLevel.LOG_WRITE_LEVEL_LOWEST_PRIORITY, true);
                return PeerPacketReceivedStatus;
            }
            else
            {

                if (!_keepAlive)
                    DisconnectFromTarget();
                else // Enable keep alive.
                    TaskEnablePeerPacketKeepAlive();

            }

            return true;
        }

        #endregion

        #region Wait packet to receive functions.

        /// <summary>
        /// Task in waiting a packet of response sent by the peer target.
        /// </summary>
        private async Task TaskWaitPeerPacketResponse()
        {

            listPacketReceived?.Clear();

            using (listPacketReceived = new DisposableList<ClassReadPacketSplitted>(false, 0, new List<ClassReadPacketSplitted>()
                {
                    new ClassReadPacketSplitted()
                }))
            {
                while (PeerConnectStatus)
                {
                    using (ReadPacketData readPacketData = await _peerSocketClient.TryReadPacketData(_peerNetworkSetting.PeerMaxPacketBufferSize, _peerNetworkSetting.PeerApiMaxConnectionDelay * 1000, false, _peerCancellationTokenMain))
                    {

                        ClassPeerCheckManager.UpdatePeerClientLastPacketReceived(_peerDatabase, PeerIpTarget, PeerUniqueIdTarget, TaskManager.TaskManager.CurrentTimestampSecond, _peerCancellationTokenMain);

                        if (!readPacketData.Status)
                            break;


                        #region Compile the packet.

                        try
                        {
                            listPacketReceived = ClassUtility.GetEachPacketSplitted(readPacketData.Data, listPacketReceived, _peerCancellationTokenMain);
                        }
                        catch
                        {
#if DEBUG
                            Debug.WriteLine("Failed to compile packet data received from peer " + PeerIpTarget);
#endif
                            break;
                        }

                        if (listPacketReceived.GetList.Sum(x => x.Packet.Length) >= ClassPeerPacketSetting.PacketMaxLengthReceive)
                        {
#if DEBUG
                            Debug.WriteLine("Too huge packet data length from peer " + PeerIpTarget +" | "+ listPacketReceived.GetList.Sum(x => x.Packet.Length)+"/"+ ClassPeerPacketSetting.PacketMaxLengthReceive);
#endif
                            break;
                        }

                        #endregion

                        int countCompleted = listPacketReceived.GetList.Count(x => x.Complete);

                        if (countCompleted == 0)
                            continue;

                        if (listPacketReceived[listPacketReceived.Count - 1].Used ||
                        !listPacketReceived[listPacketReceived.Count - 1].Complete ||
                         listPacketReceived[listPacketReceived.Count - 1].Packet == null ||
                         listPacketReceived[listPacketReceived.Count - 1].Packet.Length == 0)
                            continue;

                        byte[] base64Packet = null;
                        bool failed = false;

                        listPacketReceived[listPacketReceived.Count - 1].Used = true;

                        try
                        {
                            base64Packet = Convert.FromBase64String(listPacketReceived[listPacketReceived.Count - 1].Packet);
                        }
                        catch
                        {
                            failed = true;
                        }

                        listPacketReceived[listPacketReceived.Count - 1].Packet.Clear();

                        if (failed)
                            break;


                        ClassPeerPacketRecvObject peerPacketReceived = new ClassPeerPacketRecvObject(base64Packet, out bool status);

                        if (!status)
                        {
#if DEBUG
                            Debug.WriteLine("Can't build packet data from: " + _peerSocketClient.GetIp);
#endif
                            break;
                        };

                        if (peerPacketReceived == null)
                            break;

                        PeerPacketTypeReceived = peerPacketReceived.PacketOrder;

                        if (peerPacketReceived.PacketOrder == PacketResponseExpected)
                        {
                            PeerPacketReceivedStatus = true;

                            
                            if (_peerDatabase.ContainsPeerUniqueId(PeerIpTarget, PeerUniqueIdTarget, _peerCancellationTokenMain))
                                _peerDatabase[PeerIpTarget, PeerUniqueIdTarget, _peerCancellationTokenMain].PeerTimestampSignatureWhitelist = peerPacketReceived.PeerLastTimestampSignatureWhitelist;
                            
                            PeerPacketReceived = peerPacketReceived;
                        }

                        else
                        {


                            if (peerPacketReceived.PacketOrder != ClassPeerEnumPacketResponse.NOT_YET_SYNCED &&
                                peerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS)
                                PeerPacketReceivedStatus = peerPacketReceived.PacketOrder == ClassPeerEnumPacketResponse.SEND_MISSING_AUTH_KEYS;
                            else
                            {
                                if (peerPacketReceived.PacketOrder != ClassPeerEnumPacketResponse.NOT_YET_SYNCED &&
                                    PacketResponseExpected == ClassPeerEnumPacketResponse.SEND_BLOCK_DATA)
                                {
#if DEBUG
                                    Debug.WriteLine("Invalid packet data received: " + JsonConvert.SerializeObject(peerPacketReceived));
#endif
                                    ClassPeerCheckManager.InputPeerClientInvalidPacket(_peerDatabase, PeerIpTarget, PeerUniqueIdTarget, _peerNetworkSetting, _peerFirewallSettingObject, _peerCancellationTokenMain);
                                }
                            }
                        }
                        break;
                    }
                }
            }

            PeerTaskStatus = false;

        }

        #endregion

        #region Enable Keep alive functions.

        /// <summary>
        /// Enable a task who send a packet of keep alive to the peer target.
        /// </summary>
        private void TaskEnablePeerPacketKeepAlive()
        {

            CancelKeepAlive();

            var peerObject = _peerDatabase[PeerIpTarget, PeerUniqueIdTarget, _peerCancellationTokenMain];

            if (peerObject != null)
            {
                _peerTaskKeepAliveStatus = true;

                _peerCancellationTokenKeepAlive = new CancellationTokenSource();

                TaskManager.TaskManager.InsertTask(new Action(async () =>
                {

                    var sendObject = new ClassPeerPacketSendObject(_peerNetworkSetting.PeerUniqueId, peerObject.PeerInternPublicKey, peerObject.PeerClientLastTimestampPeerPacketSignatureWhitelist)
                    {
                        PacketOrder = ClassPeerEnumPacketSend.ASK_KEEP_ALIVE,
                        PacketContent = ClassUtility.SerializeData(new ClassPeerPacketAskKeepAlive()
                        {
                            PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                        }),
                    };


                    while (PeerConnectStatus && _peerTaskKeepAliveStatus)
                    {
                        try
                        {

                            sendObject.PacketContent = ClassUtility.SerializeData(new ClassPeerPacketAskKeepAlive()
                            {
                                PacketTimestamp = TaskManager.TaskManager.CurrentTimestampSecond
                            });

                            if (!await SendPeerPacket(sendObject.GetPacketData(), _peerCancellationTokenKeepAlive))
                            {
                                _peerTaskKeepAliveStatus = false;
                                break;
                            }

                            await Task.Delay(5 * 1000);
                        }
                        catch (Exception e)
                        {
                            if (e is SocketException || e is TaskCanceledException)
                            {
                                _peerTaskKeepAliveStatus = false;
                                break;
                            }
                        }

                    }

                }), 0, _peerCancellationTokenKeepAlive, null).Wait();
            }
        }



        #endregion

        #region Manage TCP Connection.

        /// <summary>
        /// Send a packet to the peer target.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        private async Task<bool> SendPeerPacket(byte[] packet, CancellationTokenSource cancellation)
        {
            string packetData = Convert.ToBase64String(packet) + ClassPeerPacketSetting.PacketPeerSplitSeperator.ToString();
            return await _peerSocketClient.TrySendSplittedPacket(packetData.Replace('*', ClassPeerPacketSetting.PacketPeerSplitSeperator).GetByteArray(), cancellation, _peerNetworkSetting.PeerMaxPacketSplitedSendSize, false);
        }

        private void CancelHandlePacket()
        {
            PeerTaskStatus = false;
            PeerPacketReceivedStatus = false;
            PeerPacketReceived = null;
            PeerPacketTypeReceived = ClassPeerEnumPacketResponse.NONE;
        }

        /// <summary>
        /// Cancel keep alive.
        /// </summary>
        private void CancelKeepAlive()
        {

            try
            {
                if (_peerCancellationTokenKeepAlive != null)
                {
                    if (!_peerCancellationTokenKeepAlive.IsCancellationRequested)
                        _peerCancellationTokenKeepAlive.Cancel();
                }
            }
            catch
            {

            }

            _peerTaskKeepAliveStatus = false;

        }

        /// <summary>
        /// Disconnect from target.
        /// </summary>
        public void DisconnectFromTarget()
        {
            listPacketReceived?.Clear();
            _peerSocketClient?.Kill(SocketShutdown.Both);
        }



        #endregion
    }
}
