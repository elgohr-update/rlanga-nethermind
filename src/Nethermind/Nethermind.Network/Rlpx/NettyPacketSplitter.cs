﻿/*
 * Copyright (c) 2018 Demerzel Solutions Limited
 * This file is part of the Nethermind library.
 *
 * The Nethermind library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The Nethermind library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Nethermind.Core.Encoding;

namespace Nethermind.Network.Rlpx
{   
    public class NettyPacketSplitter : MessageToMessageEncoder<Packet>
    {
        public const int FrameBoundary = 16;

        public int MaxFrameSize = FrameBoundary * 64;
        private int _contextId;

        public void DisableFraming()
        {
            MaxFrameSize = int.MaxValue;
        }
        
        protected override void Encode(IChannelHandlerContext context, Packet message, List<object> output)
        {
            Interlocked.Increment(ref _contextId);

            byte[] packetTypeData = Rlp.Encode(message.PacketType).Bytes;
            int packetTypeSize = packetTypeData.Length;
            int totalPayloadSize = packetTypeSize + message.Data.Length;
            
            int framesCount = (totalPayloadSize - 1) / MaxFrameSize + 1;
            for (int i = 0; i < framesCount; i++)
            {
                int totalPayloadOffset = MaxFrameSize * i;
                int framePayloadSize = Math.Min(MaxFrameSize, totalPayloadSize - totalPayloadOffset);
                int paddingSize = 0;
                if (i == framesCount - 1)
                {
                    paddingSize = totalPayloadSize % 16 == 0 ? 0 : 16 - totalPayloadSize % 16;
                }

                byte[] frame = new byte[16 + 16 + framePayloadSize + paddingSize + 16]; // header + header MAC + packet type + payload + padding + frame MAC

                frame[0] = (byte)(framePayloadSize >> 16);
                frame[1] = (byte)(framePayloadSize >> 8);
                frame[2] = (byte)framePayloadSize;
                
                Rlp[] headerDataItems;
                if (framesCount > 1)
                {
                    if (i == 0)
                    {
                        headerDataItems = new Rlp[3];
                        headerDataItems[2] = Rlp.Encode(totalPayloadSize);
                    }
                    else
                    {
                        headerDataItems = new Rlp[2];
                    }

                    headerDataItems[1] = Rlp.Encode(_contextId);
                }
                else
                {
                    headerDataItems = new Rlp[1];
                }

                // adaptive message IDs we always send protocol ID as 0
                // headerDataItems[0] = message.Protocol;
                headerDataItems[0] = Rlp.Encode(0);

                // TODO: rlp into existing array
                int framePacketTypeSize = i == 0 ? packetTypeData.Length : 0;
                byte[] headerDataBytes = Rlp.Encode(headerDataItems).Bytes;
                Buffer.BlockCopy(headerDataBytes, 0, frame, 3, headerDataBytes.Length);
                Buffer.BlockCopy(packetTypeData, 0, frame, 32, framePacketTypeSize);
                Buffer.BlockCopy(message.Data, totalPayloadOffset - packetTypeSize + framePacketTypeSize, frame, 32 + framePacketTypeSize, framePayloadSize - framePacketTypeSize);

                output.Add(frame);
            }
        }
    }
}