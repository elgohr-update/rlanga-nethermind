//  Copyright (c) 2018 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Buffers.Binary;
using Nethermind.Core2.Containers;
using Nethermind.Core2.Crypto;
using Nethermind.Core2.Types;

namespace Nethermind.Ssz
{
    public static partial class Ssz
    {
        public static void Encode(Span<byte> span, IndexedAttestation? container)
        {
            if (container is null)
            {
                return;
            }
            
            if (span.Length != IndexedAttestation.SszLength(container))
            {
                ThrowTargetLength<IndexedAttestation>(span.Length, IndexedAttestation.SszLength(container));
            }

            int offset = 0;
            int dynamicOffset = IndexedAttestation.SszDynamicOffset;
            Encode(span, container.AttestingIndices, ref offset, ref dynamicOffset);
            Encode(span, container.Data, ref offset);
            Encode(span, container.Signature, ref offset);
        }

        public static IndexedAttestation DecodeIndexedAttestation(Span<byte> span)
        {
            IndexedAttestation container = new IndexedAttestation();
            int offset = 0;
            DecodeDynamicOffset(span, ref offset, out int dynamicOffset1);
            container.AttestingIndices = DecodeValidatorIndexes(span.Slice(dynamicOffset1));
            container.Data = DecodeAttestationData(span, ref offset);
            container.Signature = DecodeBlsSignature(span, ref offset);
            return container;
        }
    }
}