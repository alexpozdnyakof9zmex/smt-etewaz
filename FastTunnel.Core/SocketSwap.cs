﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FastTunnel.Core
{
    public class SocketSwap
    {
        private Socket _sockt1;
        private Socket _sockt2;
        bool Swaped = false;

        private class Channel
        {
            public Socket Send { get; set; }

            public Socket Receive { get; set; }
        }

        public SocketSwap(Socket sockt1, Socket sockt2)
        {
            _sockt1 = sockt1;
            _sockt2 = sockt2;
        }

        public void StartSwap()
        {
            Swaped = true;
            ThreadPool.QueueUserWorkItem(swapCallback, new Channel
            {
                Send = _sockt1,
                Receive = _sockt2
            });

            ThreadPool.QueueUserWorkItem(swapCallback, new Channel
            {
                Send = _sockt2,
                Receive = _sockt1
            });
        }

        private void swapCallback(object state)
        {
            var chanel = state as Channel;
            while (true)
            {
                try
                {
                    byte[] result = new byte[1024];
                    int num = chanel.Receive.Receive(result, result.Length, SocketFlags.None);
                    if (num == 0)
                    {
                        if (chanel.Send.Connected)
                        {
                            chanel.Send.Close();
                        }
                        if (chanel.Receive.Connected)
                        {
                            chanel.Receive.Close();
                        }
                        break;
                    }

                    chanel.Send.Send(result, num, SocketFlags.None);
                }
                catch (SocketException)
                {
                    if (chanel.Send.Connected)
                        chanel.Send.Close();
                    if (chanel.Receive.Connected)
                        chanel.Receive.Close();
                    break;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        internal SocketSwap BeforeSwap(Action fun)
        {
            if (Swaped)
            {
                throw new Exception("BeforeSwap must be invoked before StartSwap!");
            }

            fun?.Invoke();
            return this;
        }
    }
}
