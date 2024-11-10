﻿using Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Network
{
    public class ServerSession : Session
    {
        object _lock = new object();
        public bool _connecting { get; private set; } = false;
        Queue<string> _recvQueue = new Queue<string>();

        public Queue<string> PopQueue()
        {
            lock(_lock)
            {
                Queue<string> result = _recvQueue;
                _recvQueue = new Queue<string>();
                return result;
            }
        }

        public void Send(Packet packet)
        {
            string packetJson = JsonConvert.SerializeObject(packet);
            Send(packetJson);
        }

        protected override void OnConnect()
        {
            Console.WriteLine($"Connect");

            _connecting = true;
        }

        protected override void OnDiscconect()
        {
            Console.WriteLine($"Disconnect");

            _connecting = false;
        }

        protected override void OnRecvPacket(ArraySegment<byte> data)
        {
            string json = System.Text.Encoding.UTF8.GetString(data.Array, 0, data.Count);
            Packet packet = JsonConvert.DeserializeObject<Packet>(json);

            switch (packet.Type)
            {
                case PacketType.S_LogIn:
                    {
                        S_LogInPacket s_LoginPacket = JsonConvert.DeserializeObject<S_LogInPacket>(json);
                        if(s_LoginPacket.Success)
                        {
                            Managers.Network.Name = s_LoginPacket.Name;

                            UnityEvent sceneMove = new UnityEvent();
                            sceneMove.AddListener(() => SceneManager.LoadScene("Matching Scene", LoadSceneMode.Single));
                            Managers.Timer.SetTimerNextUpdate(sceneMove);                           
                        }

                        break;
                    }
                case PacketType.S_SignUp:
                    {
                        S_SignUpPacket s_SignUpPacket = JsonConvert.DeserializeObject<S_SignUpPacket>(json);
                        if (s_SignUpPacket.Success)
                        {
                            Managers.Network.Name = s_SignUpPacket.Name;

                            UnityEvent sceneMove = new UnityEvent();
                            sceneMove.AddListener(() => SceneManager.LoadScene("Matching Scene", LoadSceneMode.Single));
                            Managers.Timer.SetTimerNextUpdate(sceneMove);
                        }

                        break;
                    }
                case PacketType.S_EnterRoom:
                    {
                        S_EnterRoomPacket s_SignUpPacket = JsonConvert.DeserializeObject<S_EnterRoomPacket>(json);

                        UnityEvent sceneMove = new UnityEvent();
                        sceneMove.AddListener(() => SceneManager.LoadScene("Baduk Scene", LoadSceneMode.Single));
                        Managers.Timer.SetTimerNextUpdate(sceneMove);

                        break;
                    }
                case PacketType.S_Chat:
                    {
                        S_Chat s_Chat = JsonConvert.DeserializeObject<S_Chat>(json);

                        UnityEvent sceneMove = new UnityEvent();
                        sceneMove.AddListener(() =>
                        {
                            GameObject chatBoxGo = GameObject.Find("UI_ChatBox");
                            if (chatBoxGo)
                            {
                                UI_ChatBox chatBox = chatBoxGo.GetComponent<UI_ChatBox>();
                                chatBox.PushMessage(s_Chat.Sender, s_Chat.Message);
                            }
                        });
                        Managers.Timer.SetTimerNextUpdate(sceneMove);

                        break;
                    }
                default:
                    break;
            }
        }

        protected override void OnSendPacket(int numOfBytes)
        {
            Console.WriteLine($"Send : {numOfBytes}");
        }
    }
}