﻿using Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Server
{
    public class OmokRoom
    {
        public int Id;

        private object _lock = new object();
        ClientSession _blackPlayer;
        ClientSession _whitePlayer;
        bool _isFinish = false;
        StoneType _curTurn = StoneType.Black;

        List<List<StoneType>> _stones;

        public OmokRoom(int inId)
        {
            Id = inId;

            _stones = new List<List<StoneType>>(15);
            for (int i = 0; i < 15; i++)
            {
                _stones.Add(new List<StoneType>(15));
                for (int j = 0; j < 15; j++)
                {
                    _stones[i].Add(StoneType.None);
                }
            }
        }

        public void Start(ClientSession white, ClientSession black)
        {
            lock (_lock)
            {
                _whitePlayer = white;
                _whitePlayer.OnEnterRoom(this);

                _blackPlayer = black;
                _blackPlayer.OnEnterRoom(this);

                // 흑돌 유저
                {
                    S_EnterRoomPacket s_EnterRoomPacket = new S_EnterRoomPacket();
                    s_EnterRoomPacket.BlackPlayerName = _blackPlayer.Name;
                    s_EnterRoomPacket.WhitePlayerName = _whitePlayer.Name;
                    s_EnterRoomPacket.YourStone = StoneType.Black;
                    s_EnterRoomPacket.CurStone = StoneType.Black;
                    _blackPlayer.Send(s_EnterRoomPacket);
                }

                // 백돌 유저
                {
                    S_EnterRoomPacket s_EnterRoomPacket = new S_EnterRoomPacket();
                    s_EnterRoomPacket.BlackPlayerName = _blackPlayer.Name;
                    s_EnterRoomPacket.WhitePlayerName = _whitePlayer.Name;
                    s_EnterRoomPacket.YourStone = StoneType.White;
                    s_EnterRoomPacket.CurStone = StoneType.Black;
                    _whitePlayer.Send(s_EnterRoomPacket);
                }

                LogManager.Instance.PushMessage($"Room Start");
            }
        }

        public void ComeBack(ClientSession session)
        {
            session.OnEnterRoom(this);

            List<PositionInfo> positions = new List<PositionInfo>();
            for (int x = 0; x < 15; x++)
            {
                for (int y = 0; y < 15; y++)
                {
                    if (_stones[x][y] != StoneType.None)
                        positions.Add(new PositionInfo() 
                        { 
                            PosX = x, 
                            PosY = y, 
                            Stone = _stones[x][y] 
                        });
                }
            }

            if (_whitePlayer.Name == session.Name)
            {
                _whitePlayer = session;

                S_EnterRoomPacket s_EnterRoomPacket = new S_EnterRoomPacket();
                s_EnterRoomPacket.BlackPlayerName = _blackPlayer.Name;
                s_EnterRoomPacket.WhitePlayerName = _whitePlayer.Name;
                s_EnterRoomPacket.YourStone = StoneType.White;
                s_EnterRoomPacket.CurStone = _curTurn;
                s_EnterRoomPacket.Positions = positions;
                _whitePlayer.Send(s_EnterRoomPacket);
            }

            if (_blackPlayer.Name == session.Name)
            {
                _blackPlayer = session;

                S_EnterRoomPacket s_EnterRoomPacket = new S_EnterRoomPacket();
                s_EnterRoomPacket.BlackPlayerName = _blackPlayer.Name;
                s_EnterRoomPacket.WhitePlayerName = _whitePlayer.Name;
                s_EnterRoomPacket.YourStone = StoneType.Black;
                s_EnterRoomPacket.CurStone = _curTurn;
                s_EnterRoomPacket.Positions = positions;
                _blackPlayer.Send(s_EnterRoomPacket);
            }

            StoneType leaveStone = GetSessionStoneType(session);
            S_ChatPacket s_ChatPacket = new S_ChatPacket();
            s_ChatPacket.Sender = "Server";
            s_ChatPacket.Message = $"User {leaveStone} ComeBack";
            SendAll(s_ChatPacket);

            LogManager.Instance.PushMessage($"User {leaveStone} ComeBack");
        }

        public void Leave(ClientSession session)
        {
            StoneType leaveStone = GetSessionStoneType(session);
            S_ChatPacket s_ChatPacket = new S_ChatPacket();
            s_ChatPacket.Sender = "Server";
            s_ChatPacket.Message = $"User {leaveStone} Leave";
            SendAll(s_ChatPacket);
        }

        public void SendAll(Packet packet)
        {
            SendAll(null, packet);
        }

        /// <summary>
        /// Sender을 제외한 모두에게 패킷 전송
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="packet"></param>
        public void SendAll(Session sender, Packet packet)
        {
            lock (_lock)
            {
                if (_whitePlayer != sender)
                    _whitePlayer.Send(packet);

                if (_blackPlayer != sender)
                    _blackPlayer.Send(packet);
            }
        }

        StoneType GetSessionStoneType(ClientSession session)
        {
            if (_whitePlayer == session)
                return StoneType.White;

            if (_blackPlayer == session)
                return StoneType.Black;

            return StoneType.None;
        }

        #region Place
        public void PlaceStone(ClientSession mover, C_PlaceStonePacket c_MovePacket)
        {
            lock (_lock)
            {
                // 게임이 이미 끝났다면
                if (_isFinish)
                    return;

                // 현재 흑돌의 턴이지만 흑돌이 아닌 플레이어가 수를 둔다면
                if (_curTurn == StoneType.Black && mover != _blackPlayer)
                    return;

                // 현재 백돌의 턴이지만 백돌이 아닌 플레이어가 수를 둔다면
                if (_curTurn == StoneType.White && mover != _whitePlayer)
                    return;

                // 이미 수가 두어진 곳에 수를 두려고 한다면
                if (_stones[c_MovePacket.PosX][c_MovePacket.PosY] != StoneType.None)
                    return;

                // 수 두고 알리기
                _stones[c_MovePacket.PosX][c_MovePacket.PosY] = _curTurn;

                S_PlaceStonePacket s_MovePacket = new S_PlaceStonePacket();
                s_MovePacket.Mover = _curTurn;
                s_MovePacket.PosX = c_MovePacket.PosX;
                s_MovePacket.PosY = c_MovePacket.PosY;
                SendAll(s_MovePacket);

                // 게임이 끝났는지
                if (IsFiveInARow(c_MovePacket.PosX, c_MovePacket.PosY))
                {
                    LogManager.Instance.PushMessage($"WIN {_curTurn}");

                    _isFinish = true;

                    // 게임 종료 알리기
                    S_GameFinishPacket s_GameFinishPacket = new S_GameFinishPacket();
                    s_GameFinishPacket.Winner = _curTurn;
                    SendAll(s_GameFinishPacket);

                    // White
                    {
                        _whitePlayer.Account.Score += _curTurn == StoneType.White ? 100 : -100;
                        int s = _whitePlayer.Account.Update();
                    }

                    // Black
                    {
                        _blackPlayer.Account.Score += _curTurn == StoneType.Black ? 100 : -100;
                        int s = _blackPlayer.Account.Update();
                    }
                }
                else
                {
                    // 턴 전환
                    _curTurn = _curTurn == StoneType.Black ? StoneType.White : StoneType.Black;
                }
            }
        }

        // 특정 좌표(x, y)에서 5줄 완성 여부를 확인
        public bool IsFiveInARow(int x, int y)
        {
            StoneType stone = _stones[x][y];
            if (stone == StoneType.None) return false; // 돌이 없는 경우 확인할 필요 없음

            // 4개의 방향을 각각 체크
            return CheckDirection(x, y, 1, 0, stone)  // 수평 확인
                || CheckDirection(x, y, 0, 1, stone)  // 수직 확인
                || CheckDirection(x, y, 1, 1, stone)  // 대각선 ↘ 확인
                || CheckDirection(x, y, 1, -1, stone); // 대각선 ↙ 확인
        }

        // 주어진 방향(dX, dY)으로 연속된 5개의 돌이 있는지 확인하는 함수
        private bool CheckDirection(int x, int y, int dX, int dY, StoneType stone)
        {
            int count = 1; // 현재 위치에 돌이 놓여있으므로 1부터 시작

            // 왼쪽 방향 확인
            for (int i = 1; i < 5; i++)
            {
                int nx = x - dX * i;
                int ny = y - dY * i;
                if (IsWithinBounds(nx, ny) && _stones[nx][ny] == stone)
                    count++;
                else
                    break;
            }

            // 오른쪽 방향 확인
            for (int i = 1; i < 5; i++)
            {
                int nx = x + dX * i;
                int ny = y + dY * i;
                if (IsWithinBounds(nx, ny) && _stones[nx][ny] == stone)
                    count++;
                else
                    break;
            }

            // 연속된 돌의 개수가 5개면 성공
            return count >= 5;
        }

        // 오목판의 범위 내에 있는지 확인하는 함수
        private bool IsWithinBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < _stones.Count && y < _stones[0].Count;
        }
        #endregion
    }
}